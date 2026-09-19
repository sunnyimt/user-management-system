using UserManagementAPI.Data;
using UserManagementAPI.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using System.Text.Json;

namespace UserManagementAPI.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IConfigurationService _configService;

        public DocumentService(ApplicationDbContext context, HttpClient httpClient, IConfiguration configuration, IConfigurationService configService)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
            _configService = configService;
        }

        public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, int userId)
        {
            try
            {
                Console.WriteLine($"Starting document upload: {fileName} for user {userId}");

                string extractedText;
                try
                {
                    extractedText = ExtractTextFromPdf(fileStream);
                    if (string.IsNullOrWhiteSpace(extractedText))
                    {
                        extractedText = $"[Document: {fileName}]\n\nCould not extract text from PDF. Please ensure the PDF contains readable text.";
                    }
                }
                catch (Exception pdfEx)
                {
                    Console.WriteLine($"Warning: PDF extraction failed: {pdfEx.Message}. Using placeholder text.");
                    extractedText = $"[Document: {fileName}]\n\nPDF parsing error. File uploaded successfully but text extraction failed. Error: {pdfEx.Message}";
                }

                var document = new Document
                {
                    FileName = fileName,
                    FileType = "pdf",
                    OriginalText = extractedText,
                    UploadedAt = DateTime.UtcNow,
                    UploadedByUserId = userId,
                    IsDeleted = false
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Document saved to database with ID: {document.Id}");

                // Process and chunk the document (with error handling)
                try
                {
                    await ProcessDocumentChunksAsync(document);
                    Console.WriteLine($"Document chunks processed successfully");
                }
                catch (Exception chunkEx)
                {
                    Console.WriteLine($"Warning: Chunk processing failed: {chunkEx.Message}. Document still uploaded.");
                    // Don't throw - document is already saved
                }

                return document;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading document: {ex.Message}\n{ex.StackTrace}");
                throw new InvalidOperationException($"Failed to upload document: {ex.Message}", ex);
            }
        }

        private string ExtractTextFromPdf(Stream fileStream)
        {
            try
            {
                // Reset stream position to beginning
                if (fileStream.CanSeek)
                {
                    fileStream.Seek(0, SeekOrigin.Begin);
                }

                var text = new System.Text.StringBuilder();

                using (var reader = new PdfReader(fileStream))
                using (var pdfDoc = new PdfDocument(reader))
                {
                    int pageCount = pdfDoc.GetNumberOfPages();
                    Console.WriteLine($"PDF has {pageCount} pages");

                    for (int page = 1; page <= pageCount; page++)
                    {
                        try
                        {
                            var strategy = new SimpleTextExtractionStrategy();
                            var pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
                            if (!string.IsNullOrWhiteSpace(pageText))
                            {
                                text.Append(pageText);
                                text.Append("\n");
                            }
                        }
                        catch (Exception pageEx)
                        {
                            Console.WriteLine($"Warning: Could not extract text from page {page}: {pageEx.Message}");
                        }
                    }
                }

                var result = text.ToString().Trim();
                if (string.IsNullOrWhiteSpace(result))
                {
                    throw new InvalidOperationException("No text could be extracted from PDF");
                }

                Console.WriteLine($"Extracted {result.Length} characters from PDF");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PDF extraction error: {ex.Message}");
                throw;
            }
        }

        private async Task ProcessDocumentChunksAsync(Document document)
        {
            var chunks = SplitTextIntoChunks(document.OriginalText ?? "");

            var documentChunks = new List<DocumentChunk>();

            foreach (var (content, index) in chunks.Select((c, i) => (c, i)))
            {
                var embedding = await GenerateEmbeddingAsync(content);

                var chunk = new DocumentChunk
                {
                    DocumentId = document.Id,
                    Content = content,
                    ChunkIndex = index,
                    Embedding = embedding,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                documentChunks.Add(chunk);
            }

            _context.DocumentChunks.AddRange(documentChunks);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Processed {documentChunks.Count} chunks for document: {document.FileName}");
        }

        private List<string> SplitTextIntoChunks(string text)
        {
            var config = _configService.GetChunkingConfiguration();
            return _configService.ChunkDocumentText(text, config);
        }

        private async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var apiKey = _configuration["Anthropic:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return GenerateMockEmbedding(text);
                }

                var request = new
                {
                    model = "claude-3-5-sonnet-20241022",
                    input = text
                };

                _httpClient.Timeout = TimeSpan.FromSeconds(5);
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                try
                {
                    var response = await _httpClient.PostAsync(
                        "https://api.anthropic.com/v1/messages/embeddings",
                        content
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var jsonDoc = JsonDocument.Parse(responseBody);

                        if (jsonDoc.RootElement.TryGetProperty("embedding", out var embeddingElement))
                        {
                            return embeddingElement.EnumerateArray()
                                .Select(e => (float)e.GetDouble())
                                .ToArray();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Embedding API error: {response.StatusCode}");
                        return GenerateMockEmbedding(text);
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Embedding API timeout - using mock embeddings");
                    return GenerateMockEmbedding(text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating embedding: {ex.Message}");
                return GenerateMockEmbedding(text);
            }

            return GenerateMockEmbedding(text);
        }

        private float[] GenerateMockEmbedding(string text)
        {
            // Generate deterministic mock embeddings based on text for testing
            var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(text));
            var random = new Random(BitConverter.ToInt32(hash, 0));

            var embedding = new float[1536]; // Claude embedding dimension
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)random.NextGaussian();
            }

            // Normalize
            var norm = (float)Math.Sqrt(embedding.Sum(x => x * x));
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= norm;
            }

            return embedding;
        }

        public async Task<List<DocumentChunk>> GetRelevantChunksAsync(string query, int limit = 5)
        {
            try
            {
                var queryEmbedding = await GenerateEmbeddingAsync(query);

                // Fetch all chunks and calculate similarity on client side
                var chunks = await _context.DocumentChunks
                    .Where(c => !c.IsDeleted && c.Document != null && !c.Document.IsDeleted)
                    .Include(c => c.Document)
                    .AsNoTracking()
                    .ToListAsync();

                // Calculate cosine similarity for each chunk
                var rankedChunks = chunks
                    .Select(c => new
                    {
                        Chunk = c,
                        Similarity = c.Embedding != null ? CosineSimilarity(queryEmbedding, c.Embedding) : 0f
                    })
                    .OrderByDescending(x => x.Similarity)
                    .Take(limit)
                    .Select(x => x.Chunk)
                    .ToList();

                return rankedChunks;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving relevant chunks: {ex.Message}");
                return new List<DocumentChunk>();
            }
        }


        private float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                return 0f;

            float dotProduct = 0f;
            float magnitudeA = 0f;
            float magnitudeB = 0f;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                magnitudeA += a[i] * a[i];
                magnitudeB += b[i] * b[i];
            }

            magnitudeA = (float)Math.Sqrt(magnitudeA);
            magnitudeB = (float)Math.Sqrt(magnitudeB);

            if (magnitudeA == 0 || magnitudeB == 0)
                return 0f;

            return dotProduct / (magnitudeA * magnitudeB);
        }

        public async Task<List<Document>> GetUserDocumentsAsync(int userId)
        {
            return await _context.Documents
                .Where(d => d.UploadedByUserId == userId && !d.IsDeleted)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task DeleteDocumentAsync(int documentId)
        {
            try
            {
                Console.WriteLine($"Starting delete for document ID: {documentId}");

                // Use tracking query to modify the entity
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    Console.WriteLine($"Document {documentId} not found");
                    throw new InvalidOperationException($"Document with ID {documentId} not found");
                }

                Console.WriteLine($"Found document: {document.FileName}");

                // Mark document as deleted
                document.IsDeleted = true;
                document.DeletedAt = DateTime.UtcNow;

                // Mark all chunks as deleted
                var chunks = await _context.DocumentChunks
                    .Where(c => c.DocumentId == documentId)
                    .ToListAsync();

                Console.WriteLine($"Found {chunks.Count} chunks to delete");

                foreach (var chunk in chunks)
                {
                    chunk.IsDeleted = true;
                }

                // Save changes
                var changeCount = await _context.SaveChangesAsync();
                Console.WriteLine($"Deleted document and {chunks.Count} chunks. Changes saved: {changeCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting document {documentId}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<string> GenerateEmbeddingContextAsync(string query)
        {
            var relevantChunks = await GetRelevantChunksAsync(query, limit: 5);

            if (relevantChunks.Count == 0)
                return string.Empty;

            var context = new System.Text.StringBuilder();
            context.AppendLine("# Relevant Document Context:");
            context.AppendLine();

            foreach (var chunk in relevantChunks)
            {
                context.AppendLine($"## From: {chunk.Document?.FileName}");
                context.AppendLine(chunk.Content);
                context.AppendLine();
            }

            return context.ToString();
        }
    }

    public static class RandomExtensions
    {
        public static double NextGaussian(this Random random)
        {
            double u1 = random.NextDouble();
            double u2 = random.NextDouble();
            double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return z0;
        }
    }
}
