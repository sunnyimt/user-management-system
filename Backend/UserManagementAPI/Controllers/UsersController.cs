using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using UserManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] UserRequest request)
        {
            if (string.IsNullOrEmpty(request?.Username) || string.IsNullOrEmpty(request?.Password))
            {
                return BadRequest(new { message = "Username and password are required" });
            }

            var existingUser = await _userService.GetUserByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Username already exists" });
            }

            var createdUser = await _userService.CreateUserAsync(new User { Username = request.Username, PasswordHash = request.Password });
            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] UserRequest request)
        {
            if (string.IsNullOrEmpty(request?.Username) || string.IsNullOrEmpty(request?.Password))
            {
                return BadRequest(new { message = "Username and password are required" });
            }

            var existingUser = await _userService.GetUserByIdAsync(id);
            if (existingUser == null)
                return NotFound(new { message = "User not found" });

            var userWithSameUsername = await _userService.GetUserByUsernameAsync(request.Username);
            if (userWithSameUsername != null && userWithSameUsername.Id != id)
            {
                return BadRequest(new { message = "Username already exists" });
            }

            var actingUsername = User.FindFirst(ClaimTypes.Name)?.Value;
            var updatedUser = await _userService.UpdateUserAsync(id, new User { Username = request.Username, PasswordHash = request.Password }, actingUsername);
            return Ok(updatedUser);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            await _userService.DeleteUserAsync(id);
            return NoContent();
        }
    }
}
