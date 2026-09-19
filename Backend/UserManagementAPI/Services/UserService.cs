using UserManagementAPI.Models;
using UserManagementAPI.Data;
using Microsoft.EntityFrameworkCore;
using static BCrypt.Net.BCrypt;

namespace UserManagementAPI.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User> CreateUserAsync(User user);
        Task<User?> UpdateUserAsync(int id, User user);
        Task DeleteUserAsync(int id);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext dbContext, ILogger<UserService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                return await _dbContext.Users.Where(u => !u.IsDeleted).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching users: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            try
            {
                return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching user by ID: {ex.Message}");
                return null;
            }
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching user by username: {ex.Message}");
                return null;
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                user.PasswordHash = HashPassword(user.PasswordHash);
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating user: {ex.Message}");
                throw;
            }
        }

        public async Task<User> UpdateUserAsync(int id, User user)
        {
            try
            {
                var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
                if (existingUser == null)
                    return null;

                existingUser.Username = user.Username;
                existingUser.PasswordHash = HashPassword(user.PasswordHash);
                _dbContext.Users.Update(existingUser);
                await _dbContext.SaveChangesAsync();
                return existingUser;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteUserAsync(int id)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
                if (user != null)
                {
                    user.IsDeleted = true;
                    _dbContext.Users.Update(user);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting user: {ex.Message}");
                throw;
            }
        }
    }
}
