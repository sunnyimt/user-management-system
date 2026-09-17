using UserManagementAPI.Data;
using UserManagementAPI.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace UserManagementAPI.Services
{
    public interface IChatService
    {
        Task<string> SendMessageAsync(int userId, string userMessage);
        Task<string> SearchLocalDatabaseAsync(string query);
        Task<List<ChatMessage>> GetChatHistoryAsync(int userId);
    }

    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ChatService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IDocumentService _documentService;

        public ChatService(ApplicationDbContext dbContext, ILogger<ChatService> logger, IConfiguration configuration, HttpClient httpClient, IDocumentService documentService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _documentService = documentService;
        }

        public async Task<string> SendMessageAsync(int userId, string userMessage)
        {
            try
            {
                var useLocalLLM = _configuration.GetValue<bool>("UseLocalLLM", true);
                _logger.LogInformation($"Chat request - UseLocalLLM: {useLocalLLM}");

                if (useLocalLLM)
                {
                    try
                    {
                        return await SendMessageToLocalLLMAsync(userId, userMessage);
                    }
                    catch (Exception localEx)
                    {
                        _logger.LogWarning($"Local LLM (Ollama) failed: {localEx.Message}. Using local database context only.");
                        return await GetLocalDatabaseResponseAsync(userId, userMessage);
                    }
                }
                else
                {
                    return await SendMessageToAnthropicAsync(userId, userMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in chat service: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private async Task<string> GetLocalDatabaseResponseAsync(int userId, string userMessage)
        {
            _logger.LogInformation("Using local database response - Ollama not available");

            // Save user message
            var userMsg = new ChatMessage
            {
                UserId = userId,
                Role = "user",
                Content = userMessage,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.ChatMessages.Add(userMsg);
            await _dbContext.SaveChangesAsync();

            // Try to get document context, but don't wait too long
            string documentContext = "";
            try
            {
                var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3));
                var contextTask = _documentService.GenerateEmbeddingContextAsync(userMessage);
                documentContext = await contextTask.ConfigureAwait(false);
            }
            catch
            {
                _logger.LogWarning("Could not retrieve document context in time");
            }

            // Create a response
            string response;
            if (!string.IsNullOrEmpty(documentContext))
            {
                response = $"Based on the documents in your system:\n\n{documentContext}\n\n" +
                    $"(Note: Ollama is not running. To get AI-powered answers, please start Ollama: ollama run mistral)";
            }
            else
            {
                response = "I'm currently running in local mode without an AI engine. " +
                    "To enable AI-powered chat responses, please:\n\n" +
                    "1. Start Ollama locally:\n" +
                    "   - Download from https://ollama.ai\n" +
                    "   - Run: ollama run mistral\n\n" +
                    "2. Or upload documents to the Documents tab to search local knowledge base\n\n" +
                    "Your message has been saved to the conversation history.";
            }

            // Save assistant response
            var assistantMsg = new ChatMessage
            {
                UserId = userId,
                Role = "assistant",
                Content = response,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.ChatMessages.Add(assistantMsg);
            await _dbContext.SaveChangesAsync();

            return response;
        }

        private async Task<string> GetFallbackResponseAsync(int userId, string userMessage)
        {
            _logger.LogInformation("Using fallback response - no LLM available");

            // Save user message
            var userMsg = new ChatMessage
            {
                UserId = userId,
                Role = "user",
                Content = userMessage,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.ChatMessages.Add(userMsg);
            await _dbContext.SaveChangesAsync();

            // Generate fallback response
            var fallbackResponse = $"I'm currently unable to connect to an LLM service. " +
                $"To enable chat functionality, please:\n\n" +
                $"Option 1: Start Ollama locally\n" +
                $"  1. Download from https://ollama.ai\n" +
                $"  2. Run: ollama run mistral\n" +
                $"  3. Ensure LocalLLM.Url is set to http://localhost:11434/api/generate\n\n" +
                $"Option 2: Use Anthropic API\n" +
                $"  1. Get API key from https://console.anthropic.com\n" +
                $"  2. Set Anthropic:ApiKey in appsettings.json\n" +
                $"  3. Set UseLocalLLM to false\n\n" +
                $"Your message was saved: \"{userMessage}\"";

            // Save assistant fallback response
            var assistantMsg = new ChatMessage
            {
                UserId = userId,
                Role = "assistant",
                Content = fallbackResponse,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.ChatMessages.Add(assistantMsg);
            await _dbContext.SaveChangesAsync();

            return fallbackResponse;
        }

        private async Task<string> SendMessageToAnthropicAsync(int userId, string userMessage)
        {
            try
            {
                var apiKey = _configuration["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic API key not configured");

                // Save user message to database
                var userMsg = new ChatMessage
                {
                    UserId = userId,
                    Role = "user",
                    Content = userMessage,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.ChatMessages.Add(userMsg);
                await _dbContext.SaveChangesAsync();

                // Get recent chat history for context
                var history = await _dbContext.ChatMessages
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(10)
                    .ToListAsync();
                history.Reverse();

                // Get relevant document context using RAG (with error handling)
                string documentContext = "";
                try
                {
                    documentContext = await _documentService.GenerateEmbeddingContextAsync(userMessage);
                }
                catch (Exception docEx)
                {
                    _logger.LogWarning($"Failed to get document context: {docEx.Message}. Continuing without documents.");
                }

                // Build system prompt with document context
                var systemPrompt = "You are a helpful assistant for the User Management System. ";
                if (!string.IsNullOrEmpty(documentContext))
                {
                    systemPrompt += "\n\nUse the following document context to answer questions:\n\n" + documentContext;
                }
                else
                {
                    systemPrompt += "You have access to the system's documentation through uploaded documents. If the user asks about system functionality and you don't have document context, provide answers based on your knowledge.";
                }

                // Build messages for Claude API
                var messages = new List<object>();
                foreach (var msg in history)
                {
                    if (msg.Id != userMsg.Id) // Exclude the message we just saved
                    {
                        messages.Add(new { role = msg.Role, content = msg.Content });
                    }
                }
                messages.Add(new { role = "user", content = userMessage });

                // Call Claude API with system prompt
                var request = new
                {
                    model = "claude-3-5-sonnet-20241022",
                    max_tokens = 1024,
                    system = systemPrompt,
                    messages = messages
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {response.StatusCode} - {errorContent}");
                    throw new InvalidOperationException($"API returned {response.StatusCode}: {errorContent}");
                }

                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(responseJson))
                {
                    var root = doc.RootElement;
                    var assistantMessage = root
                        .GetProperty("content")[0]
                        .GetProperty("text")
                        .GetString();

                    // Save assistant message to database
                    var assistantMsg = new ChatMessage
                    {
                        UserId = userId,
                        Role = "assistant",
                        Content = assistantMessage,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.ChatMessages.Add(assistantMsg);
                    await _dbContext.SaveChangesAsync();

                    return assistantMessage;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in chat service: {ex.Message}");
                throw;
            }
        }

        private async Task<string> SendMessageToLocalLLMAsync(int userId, string userMessage)
        {
            try
            {
                // Save user message to database
                var userMsg = new ChatMessage
                {
                    UserId = userId,
                    Role = "user",
                    Content = userMessage,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.ChatMessages.Add(userMsg);
                await _dbContext.SaveChangesAsync();

                // Get recent chat history for context
                var history = await _dbContext.ChatMessages
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(10)
                    .ToListAsync();
                history.Reverse();

                // Get relevant document context using RAG (with error handling)
                string documentContext = "";
                try
                {
                    documentContext = await _documentService.GenerateEmbeddingContextAsync(userMessage);
                }
                catch (Exception docEx)
                {
                    _logger.LogWarning($"Failed to get document context: {docEx.Message}. Continuing without documents.");
                }

                // Build prompt from chat history with document context
                var prompt = "You are a helpful assistant for the User Management System.\n";

                if (!string.IsNullOrEmpty(documentContext))
                {
                    prompt += "\nRELEVANT DOCUMENTATION:\n" + documentContext + "\n";
                }

                prompt += "---\n\nCONVERSATION:\n";
                foreach (var msg in history)
                {
                    if (msg.Id != userMsg.Id)
                    {
                        var role = msg.Role == "user" ? "User" : "Assistant";
                        prompt += $"{role}: {msg.Content}\n";
                    }
                }
                prompt += $"User: {userMessage}\nAssistant:";

                // Call local Ollama API
                var ollamaUrl = _configuration["LocalLLM:Url"] ?? "http://localhost:11434/api/generate";
                var model = _configuration["LocalLLM:Model"] ?? "mistral";

                _logger.LogInformation($"Connecting to Ollama at: {ollamaUrl} with model: {model}");

                var request = new
                {
                    model = model,
                    prompt = prompt,
                    stream = true,
                    temperature = 0.7
                };

                var requestJson = JsonSerializer.Serialize(request);
                _logger.LogInformation($"Sending streaming request to Ollama (prompt length: {prompt.Length})");

                var content = new StringContent(
                    requestJson,
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.Timeout = TimeSpan.FromSeconds(180);

                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, ollamaUrl) { Content = content };
                    using (var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead))
                    {
                        _logger.LogInformation($"Ollama response status: {response.StatusCode}");

                        if (!response.IsSuccessStatusCode)
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            _logger.LogError($"Ollama Error: {response.StatusCode} - {errorContent}");
                            throw new InvalidOperationException($"Ollama returned {response.StatusCode}: {errorContent}");
                        }

                        // Read streaming response line by line
                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        using (var reader = new System.IO.StreamReader(responseStream, System.Text.Encoding.UTF8, true, 1024))
                        {
                            var fullResponse = new System.Text.StringBuilder();
                            string line;
                            bool foundDone = false;

                            while ((line = await reader.ReadLineAsync()) != null && !foundDone)
                            {
                                if (string.IsNullOrWhiteSpace(line))
                                    continue;

                                try
                                {
                                    using (JsonDocument doc = JsonDocument.Parse(line))
                                    {
                                        var root = doc.RootElement;
                                        if (root.TryGetProperty("response", out var responseProperty))
                                        {
                                            fullResponse.Append(responseProperty.GetString());
                                        }
                                        if (root.TryGetProperty("done", out var doneProperty) && doneProperty.GetBoolean())
                                        {
                                            foundDone = true;
                                            _logger.LogInformation("Received done signal from Ollama");
                                        }
                                    }
                                }
                                catch (JsonException ex)
                                {
                                    _logger.LogWarning($"Failed to parse Ollama stream line: {ex.Message}");
                                }
                            }

                            var assistantMessage = fullResponse.ToString().Trim();
                            if (string.IsNullOrEmpty(assistantMessage))
                            {
                                throw new InvalidOperationException("Ollama returned empty response");
                            }

                            // Save assistant message to database
                            var assistantMsg = new ChatMessage
                            {
                                UserId = userId,
                                Role = "assistant",
                                Content = assistantMessage,
                                CreatedAt = DateTime.UtcNow
                            };
                            _dbContext.ChatMessages.Add(assistantMsg);
                            await _dbContext.SaveChangesAsync();

                            _logger.LogInformation($"Ollama response saved to database (length: {assistantMessage.Length})");
                            return assistantMessage;
                        }
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError($"HTTP error connecting to Ollama: {httpEx.Message}");
                    throw new InvalidOperationException($"Cannot connect to Ollama at {ollamaUrl}. Make sure Ollama is running: ollama run mistral", httpEx);
                }
                catch (TaskCanceledException timeoutEx)
                {
                    _logger.LogError($"Ollama request timeout after {_httpClient.Timeout.TotalSeconds} seconds: {timeoutEx.Message}");
                    throw new InvalidOperationException("Ollama request timed out. The model might be processing a large request.", timeoutEx);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in local LLM service: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        public async Task<string> SearchLocalDatabaseAsync(string query)
        {
            try
            {
                _logger.LogInformation($"Searching local database for: {query}");

                // Get relevant document context from local database
                try
                {
                    var documentContext = await _documentService.GenerateEmbeddingContextAsync(query);
                    return documentContext;
                }
                catch (Exception docEx)
                {
                    _logger.LogWarning($"Failed to get document context: {docEx.Message}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching local database: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(int userId)
        {
            return await _dbContext.ChatMessages
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}
