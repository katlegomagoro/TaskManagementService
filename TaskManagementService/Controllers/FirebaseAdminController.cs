using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManagementService.DAL.Models;
using TaskManagementService.Services;

namespace TaskManagementService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class FirebaseUsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public FirebaseUsersController(
            IUserService userService,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _userService = userService;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string term = "")
        {
            try
            {
                // Get users from local database
                var localUsers = await _userService.SearchUsersAsync(term);

                // Try to search Firebase if we have an API key
                var firebaseUsers = await SearchFirebaseUsersAsync(term);

                // Combine results, removing duplicates
                var allUsers = new Dictionary<string, AppUser>();

                foreach (var user in localUsers)
                {
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        allUsers[user.Email.ToLower()] = user;
                    }
                }

                foreach (var user in firebaseUsers)
                {
                    if (!string.IsNullOrEmpty(user.Email) &&
                        !allUsers.ContainsKey(user.Email.ToLower()))
                    {
                        allUsers[user.Email.ToLower()] = user;
                    }
                }

                return Ok(allUsers.Values.Select(u => new
                {
                    u.Id,
                    u.FirebaseUid,
                    u.Email,
                    u.DisplayName,
                    Source = u.Id > 0 ? "Database" : "Firebase"
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var allUsers = await _userService.GetAllUsersAsync();

                return Ok(allUsers.Select(u => new
                {
                    u.Id,
                    u.FirebaseUid,
                    u.Email,
                    u.DisplayName
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<List<AppUser>> SearchFirebaseUsersAsync(string searchTerm)
        {
            var users = new List<AppUser>();

            try
            {
                var apiKey = _configuration["Firebase:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("Firebase API key not configured");
                    return users;
                }

                // Firebase REST API endpoint
                var baseUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={apiKey}";

                // Can only look up specific emails if no search term, return empty
                if (!string.IsNullOrEmpty(searchTerm) && searchTerm.Contains("@"))
                {
                    var request = new
                    {
                        email = new[] { searchTerm }
                    };

                    var response = await _httpClient.PostAsJsonAsync(baseUrl, request);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<FirebaseLookupResponse>(content);

                        if (result?.Users?.Any() == true)
                        {
                            foreach (var firebaseUser in result.Users)
                            {
                                users.Add(new AppUser
                                {
                                    FirebaseUid = firebaseUser.LocalId,
                                    Email = firebaseUser.Email,
                                    DisplayName = firebaseUser.DisplayName ??
                                                firebaseUser.Email?.Split('@')[0] ??
                                                "User",
                                    CreatedAtUtc = DateTime.UtcNow,
                                    ModifiedAtUtc = DateTime.UtcNow
                                });
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Firebase API error: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching Firebase users: {ex.Message}");
            }

            return users;
        }

        private class FirebaseLookupResponse
        {
            [JsonPropertyName("users")]
            public List<FirebaseUser>? Users { get; set; }
        }

        private class FirebaseUser
        {
            [JsonPropertyName("localId")]
            public string LocalId { get; set; } = string.Empty;

            [JsonPropertyName("email")]
            public string? Email { get; set; }

            [JsonPropertyName("displayName")]
            public string? DisplayName { get; set; }
        }
    }
}