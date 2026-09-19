using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(IDocumentService documentService, ILogger<DocumentController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, error = "No file uploaded" });

                if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { success = false, error = "Only PDF files are supported" });

                if (file.Length > 10 * 1024 * 1024) // 10MB limit
                    return BadRequest(new { success = false, error = "File size exceeds 10MB limit" });

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(new { success = false, error = "User not authenticated" });

                using (var stream = file.OpenReadStream())
                {
                    var document = await _documentService.UploadDocumentAsync(stream, file.FileName, userId);
                    _logger.LogInformation($"Document uploaded successfully: {file.FileName} by user {userId}");
                    return Ok(new { success = true, message = $"Document '{file.FileName}' uploaded successfully", documentId = document.Id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading document: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new {
                    success = false,
                    error = $"Error uploading document: {ex.Message}",
                    details = ex.InnerException?.Message ?? "No additional details"
                });
            }
        }

        [HttpGet("my-documents")]
        public async Task<IActionResult> GetMyDocuments()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized();

                var documents = await _documentService.GetUserDocumentsAsync(userId);
                return Ok(new { success = true, documents });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving documents: {ex.Message}");
                return StatusCode(500, new { success = false, error = "Error retrieving documents" });
            }
        }

        [HttpDelete("{documentId}")]
        public async Task<IActionResult> DeleteDocument(int documentId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(new { success = false, error = "User not authenticated" });

                _logger.LogInformation($"Deleting document {documentId} by user {userId}");

                await _documentService.DeleteDocumentAsync(documentId);

                _logger.LogInformation($"Document {documentId} deleted successfully");
                return Ok(new { success = true, message = "Document deleted successfully", documentId });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting document {documentId}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new {
                    success = false,
                    error = "Error deleting document: " + ex.Message,
                    details = ex.InnerException?.Message ?? "No additional details"
                });
            }
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchDocuments([FromBody] SearchRequest request)
        {
            try
            {
                var context = await _documentService.GenerateEmbeddingContextAsync(request.Query);
                return Ok(new { success = true, context });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching documents: {ex.Message}");
                return StatusCode(500, new { success = false, error = "Error searching documents" });
            }
        }
    }

    public class SearchRequest
    {
        public string? Query { get; set; }
    }
}
