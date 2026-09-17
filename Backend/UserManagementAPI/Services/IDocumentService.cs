using UserManagementAPI.Models;

namespace UserManagementAPI.Services
{
    public interface IDocumentService
    {
        Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, int userId);
        Task<List<DocumentChunk>> GetRelevantChunksAsync(string query, int limit = 5);
        Task<List<Document>> GetUserDocumentsAsync(int userId);
        Task DeleteDocumentAsync(int documentId);
        Task<string> GenerateEmbeddingContextAsync(string query);
    }
}
