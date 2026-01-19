using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;

namespace TaskManagementService.Services
{
    public class AuthLocalStorageService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string AuthKey = "taskmanagement_auth";

        public AuthLocalStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SaveAuthStateAsync(ClaimsPrincipal user)
        {
            try
            {
                var authData = new AuthStorageData
                {
                    UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    Email = user.FindFirst(ClaimTypes.Email)?.Value,
                    DisplayName = user.FindFirst(ClaimTypes.Name)?.Value,
                    Roles = user.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToList(),
                    FirebaseUid = user.FindFirst("FirebaseUid")?.Value
                };

                var json = JsonSerializer.Serialize(authData);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AuthKey, json);
            }
            catch (Exception)
            {
                // Silently fail if localStorage is not available
            }
        }

        public async Task<ClaimsPrincipal?> LoadAuthStateAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", AuthKey);
                if (string.IsNullOrEmpty(json))
                    return null;

                var authData = JsonSerializer.Deserialize<AuthStorageData>(json);
                if (authData == null)
                    return null;

                var claims = new List<Claim>();

                if (!string.IsNullOrEmpty(authData.UserId))
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, authData.UserId));

                if (!string.IsNullOrEmpty(authData.Email))
                    claims.Add(new Claim(ClaimTypes.Email, authData.Email));

                if (!string.IsNullOrEmpty(authData.DisplayName))
                    claims.Add(new Claim(ClaimTypes.Name, authData.DisplayName));

                if (!string.IsNullOrEmpty(authData.FirebaseUid))
                    claims.Add(new Claim("FirebaseUid", authData.FirebaseUid));

                foreach (var role in authData.Roles ?? new List<string>())
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, "localStorage");
                return new ClaimsPrincipal(identity);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task ClearAuthStateAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AuthKey);
            }
            catch (Exception)
            {
                // Silently fail
            }
        }

        private class AuthStorageData
        {
            public string? UserId { get; set; }
            public string? Email { get; set; }
            public string? DisplayName { get; set; }
            public List<string>? Roles { get; set; }
            public string? FirebaseUid { get; set; }
        }
    }
}