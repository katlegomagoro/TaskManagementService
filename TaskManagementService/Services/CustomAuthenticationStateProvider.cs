using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using TaskManagementService.DAL.Models;
using TaskManagementService.Interfaces;

namespace TaskManagementService.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IFirebaseAuthClient _firebaseAuthClient;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<CustomAuthenticationStateProvider> _logger;
        private readonly IPermissionService _permissionService;
        private readonly AuthStatePersistor _authStatePersistor;
        private readonly AuthLocalStorageService _authLocalStorageService;

        public CustomAuthenticationStateProvider(
            IFirebaseAuthClient firebaseAuthClient,
            IAuthenticationService authenticationService,
            ILogger<CustomAuthenticationStateProvider> logger,
            IPermissionService permissionService,
            AuthStatePersistor authStatePersistor,
            AuthLocalStorageService authLocalStorageService)
        {
            _firebaseAuthClient = firebaseAuthClient;
            _authenticationService = authenticationService;
            _logger = logger;
            _permissionService = permissionService;
            _authStatePersistor = authStatePersistor;
            _authLocalStorageService = authLocalStorageService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // First, try to use in-memory persisted state (fastest)
                if (_authStatePersistor.CurrentUser != null)
                {
                    _logger.LogDebug("Using in-memory authentication state");
                    return new AuthenticationState(_authStatePersistor.CurrentUser);
                }

                // If no in-memory state, try localStorage
                var savedUser = await _authLocalStorageService.LoadAuthStateAsync();
                if (savedUser != null)
                {
                    _logger.LogDebug("Using saved authentication state from localStorage");
                    _authStatePersistor.SetUser(savedUser); // Cache in memory
                    return new AuthenticationState(savedUser);
                }

                // Try to get token from Firebase client
                var idToken = await _firebaseAuthClient.GetIdTokenAsync();

                if (string.IsNullOrEmpty(idToken))
                {
                    _logger.LogDebug("No Firebase token found - user is not authenticated");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Get or create user from Firebase token
                var appUser = await _authenticationService.GetOrCreateUserFromFirebaseAsync(idToken);

                if (appUser == null)
                {
                    _logger.LogWarning("Failed to get/create user from Firebase token");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Create claims principal
                var claims = await CreateClaimsAsync(appUser);
                var identity = new ClaimsIdentity(claims, "Firebase");
                var user = new ClaimsPrincipal(identity);

                // Persist both in-memory and localStorage
                _authStatePersistor.SetUser(user);
                await _authLocalStorageService.SaveAuthStateAsync(user);

                _logger.LogInformation("User authenticated: {Email} with ID: {UserId}", appUser.Email, appUser.Id);

                return new AuthenticationState(user);
            }
            catch (Exception ex) when (IsPrerenderingError(ex))
            {
                // Handle prerendering gracefully
                _logger.LogDebug("Prerendering - returning empty authentication state");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authentication state");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public async Task<ClaimsPrincipal> LoginAsync(string email, string password)
        {
            try
            {
                // Authenticate with Firebase
                var idToken = await _firebaseAuthClient.LoginAsync(email, password);

                if (string.IsNullOrEmpty(idToken))
                {
                    _logger.LogWarning("Firebase login failed for email: {Email}", email);
                    return new ClaimsPrincipal(new ClaimsIdentity());
                }

                // Get or create user
                var appUser = await _authenticationService.GetOrCreateUserFromFirebaseAsync(idToken);

                if (appUser == null)
                {
                    _logger.LogError("Failed to get/create user after Firebase login");
                    return new ClaimsPrincipal(new ClaimsIdentity());
                }

                // Create claims principal
                var claims = await CreateClaimsAsync(appUser);
                var identity = new ClaimsIdentity(claims, "Firebase");
                var user = new ClaimsPrincipal(identity);

                // Persist both in-memory and localStorage
                _authStatePersistor.SetUser(user);
                await _authLocalStorageService.SaveAuthStateAsync(user);

                // CRITICAL: Create a new task to notify
                var authState = new AuthenticationState(user);
                NotifyAuthenticationStateChanged(Task.FromResult(authState));

                _logger.LogInformation("User logged in successfully: {Email}", email);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for email: {Email}", email);
                throw;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _firebaseAuthClient.LogoutAsync();

                // Clear both storage mechanisms
                _authStatePersistor.ClearUser();
                await _authLocalStorageService.ClearAuthStateAsync();

                // Create empty claims principal
                var user = new ClaimsPrincipal(new ClaimsIdentity());

                // Notify authentication state change
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));

                _logger.LogInformation("User logged out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                throw;
            }
        }

        // Helper method to update user profile
        // Helper method to update user profile
        public async Task UpdateUserProfileAsync(int userId, string newDisplayName)
        {
            try
            {
                var currentUser = _authStatePersistor.CurrentUser;
                if (currentUser != null)
                {
                    // First, check if this is the same user
                    var currentUserIdClaim = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim, out int currentUserId) || currentUserId != userId)
                    {
                        _logger.LogWarning("Attempted to update profile for different user");
                        return;
                    }

                    // Update display name in claims
                    var identity = currentUser.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        var nameClaim = identity.FindFirst(ClaimTypes.Name);
                        if (nameClaim != null)
                        {
                            identity.RemoveClaim(nameClaim);
                        }
                        identity.AddClaim(new Claim(ClaimTypes.Name, newDisplayName));

                        // Update in-memory storage
                        _authStatePersistor.SetUser(currentUser);

                        // Update localStorage
                        await _authLocalStorageService.SaveAuthStateAsync(currentUser);

                        // Notify state change
                        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(currentUser)));

                        _logger.LogInformation("Updated user profile for ID {UserId}: {DisplayName}", userId, newDisplayName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile in auth state");
                throw;
            }
        }

        private async Task<List<Claim>> CreateClaimsAsync(AppUser appUser)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, appUser.Id.ToString()),
                new Claim(ClaimTypes.Email, appUser.Email),
                new Claim(ClaimTypes.Name, appUser.DisplayName),
                new Claim("FirebaseUid", appUser.FirebaseUid)
            };

            // Get user's permission type from database
            var permissionType = await _permissionService.GetUserPermissionTypeAsync(appUser.Id);

            // Add role claim based on permission type
            claims.Add(new Claim(ClaimTypes.Role, permissionType.ToString()));

            return claims;
        }

        private bool IsPrerenderingError(Exception ex)
        {
            return ex is InvalidOperationException &&
                   ex.Message.Contains("JavaScript interop calls cannot be issued") &&
                   ex.Message.Contains("prerendering");
        }
    }
}