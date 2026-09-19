using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<ActionResult<string>> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request?.Message))
                return BadRequest(new { message = "Message cannot be empty" });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            try
            {
                var response = await _chatService.SendMessageAsync(userId, request.Message);
                return Ok(new { response });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chat error: {ex.Message}");
                return StatusCode(500, new { message = "Error processing chat message" });
            }
        }

        [HttpPost("search-local")]
        public async Task<ActionResult> SearchLocalDatabase([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request?.Message))
                return BadRequest(new { message = "Message cannot be empty" });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            try
            {
                var results = await _chatService.SearchLocalDatabaseAsync(request.Message);
                return Ok(new {
                    hasResults = !string.IsNullOrEmpty(results),
                    context = results,
                    message = string.IsNullOrEmpty(results)
                        ? "No relevant documents found in local database"
                        : "Found relevant documents in local database"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Local search error: {ex.Message}");
                return StatusCode(500, new { message = "Error searching local database" });
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult> GetHistory()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var history = await _chatService.GetChatHistoryAsync(userId);
            return Ok(history);
        }
    }

    public class ChatRequest
    {
        public string? Message { get; set; }
    }
}
