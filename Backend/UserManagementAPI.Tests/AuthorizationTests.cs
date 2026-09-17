using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using UserManagementAPI;

namespace UserManagementAPI.Tests
{
    public class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private string _validToken;

        public AuthorizationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _validToken = GenerateValidJwtToken();
        }

        /// <summary>
        /// E1. Unauthorized Access - Users Endpoint
        /// Verify that /api/users requires JWT authentication.
        /// Expected: Unauthorized (401) without token
        /// </summary>
        [Fact]
        public async Task E1_GetUsersWithoutToken_ShouldReturn401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/users");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// E2. Unauthorized Access - Chat Endpoint
        /// Verify that /api/chat/* requires JWT authentication.
        /// Expected: Unauthorized (401) without token
        /// </summary>
        [Fact]
        public async Task E2_SendChatMessageWithoutToken_ShouldReturn401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new { message = "Hello" };

            // Act
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            );
            var response = await client.PostAsync("/api/chat/send", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// E3. Unauthorized Access - Login Endpoint
        /// Verify that /api/auth/login does NOT require JWT (public endpoint).
        /// Expected: Success (200) or BadRequest (400), not Unauthorized
        /// </summary>
        [Fact]
        public async Task E3_LoginEndpointShouldBePublic_ShouldNotReturn401()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new { username = "testuser", password = "testpass" };

            // Act
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            );
            var response = await client.PostAsync("/api/auth/login", content);

            // Assert - Should NOT be Unauthorized (401)
            // It could be BadRequest (400) for invalid credentials, or other error, but NOT 401
            Assert.NotEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// E4. User Can Access Own Data
        /// Verify that authenticated user can access their own data.
        /// Expected: Success (200) with user list
        /// </summary>
        [Fact]
        public async Task E4_GetUsersWithValidToken_ShouldReturn200Ok()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_validToken}");

            // Act
            var response = await client.GetAsync("/api/users");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// E1b. Chat Endpoint - Get History Without Token
        /// Additional test: Verify /api/chat/history also requires JWT
        /// Expected: Unauthorized (401) without token
        /// </summary>
        [Fact]
        public async Task E1b_GetChatHistoryWithoutToken_ShouldReturn401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/chat/history");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// E4b. Chat Access With Valid Token
        /// Additional test: Verify authenticated user can access chat endpoints
        /// Expected: Success (200) or appropriate response
        /// </summary>
        [Fact]
        public async Task E4b_GetChatHistoryWithValidToken_ShouldNotReturn401()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_validToken}");

            // Act
            var response = await client.GetAsync("/api/chat/history");

            // Assert - Should NOT be Unauthorized
            Assert.NotEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// E5. Invalid Token Format
        /// Additional test: Verify that malformed tokens are rejected
        /// Expected: Unauthorized (401) with malformed token
        /// </summary>
        [Fact]
        public async Task E5_UsersEndpointWithMalformedToken_ShouldReturn401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.token.format");

            // Act
            var response = await client.GetAsync("/api/users");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // Helper method to generate a valid JWT token
        private string GenerateValidJwtToken()
        {
            var key = Encoding.ASCII.GetBytes(
                "dev-secret-key-this-is-not-secure-replace-in-production-12345678901234"
            );
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Name, "user1"),
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                ),
                Issuer = "UserManagementApp",
                Audience = "UserManagementAppUsers",
            };
            var token = handler.CreateToken(descriptor);
            return handler.WriteToken(token);
        }
    }
}
