using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using UserManagementAPI.Models;
using static BCrypt.Net.BCrypt;

namespace UserManagementAPI.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            // Clear all existing users to ensure fresh seed data
            var existingUsers = await dbContext.Users.ToListAsync();
            if (existingUsers.Any())
            {
                dbContext.Users.RemoveRange(existingUsers);
                await dbContext.SaveChangesAsync();
            }

            // Create fresh seed users with proper bcrypt hashing
            var newUsers = new List<User>
            {
                new User { Username = "user1", PasswordHash = HashPassword("admin") },
                new User { Username = "user2", PasswordHash = HashPassword("admin") },
            };

            dbContext.Users.AddRange(newUsers);
            await dbContext.SaveChangesAsync();
        }
    }
}
