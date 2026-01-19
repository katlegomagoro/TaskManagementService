using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;
using TaskManagementService.DAL.Models;
using TaskManagementService.Interfaces;

namespace TaskManagementService.Services
{
    public class FirebaseUserSearchService : IFirebaseUserSearchService
    {
        private readonly FirebaseAuth _firebaseAuth;
        private readonly ILogger<FirebaseUserSearchService> _logger;

        public FirebaseUserSearchService(ILogger<FirebaseUserSearchService> logger)
        {
            _logger = logger;

            try
            {
                // Get the default FirebaseApp instance
                var firebaseApp = FirebaseApp.DefaultInstance;

                if (firebaseApp == null)
                {
                    _logger.LogError("FirebaseApp is not initialized. Check Program.cs setup.");
                    throw new InvalidOperationException("Firebase is not initialized. Call FirebaseApp.Create() in Program.cs first.");
                }

                _firebaseAuth = FirebaseAuth.GetAuth(firebaseApp);
                _logger.LogInformation("FirebaseUserSearchService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize FirebaseUserSearchService");
                throw;
            }
        }

        public async Task<List<AppUser>> SearchFirebaseUsersAsync(string searchTerm)
        {
            var users = new List<AppUser>();

            try
            {
                if (_firebaseAuth == null)
                {
                    _logger.LogWarning("FirebaseAuth is not available");
                    return users;
                }

                var allFirebaseUsers = await GetAllFirebaseUserRecordsAsync();

                // Filter based on search term
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    users = allFirebaseUsers
                        .Take(100) 
                        .Select(ConvertToAppUser)
                        .ToList();
                }
                else
                {
                    users = allFirebaseUsers
                        .Where(u =>
                            (u.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (u.DisplayName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (u.PhoneNumber?.Contains(searchTerm) ?? false))
                        .Take(50) 
                        .Select(ConvertToAppUser)
                        .ToList();
                }

                _logger.LogDebug("Found {Count} Firebase users for search term '{SearchTerm}'", users.Count, searchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Firebase users for term '{SearchTerm}'", searchTerm);
            }

            return users;
        }

        public async Task<List<AppUser>> GetAllFirebaseUsersAsync()
        {
            try
            {
                var allFirebaseUsers = await GetAllFirebaseUserRecordsAsync();
                return allFirebaseUsers
                    .Select(ConvertToAppUser)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all Firebase users");
                return new List<AppUser>();
            }
        }

        public async Task<AppUser?> GetUserByUidAsync(string firebaseUid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(firebaseUid))
                {
                    _logger.LogWarning("Empty Firebase UID provided");
                    return null;
                }

                var userRecord = await _firebaseAuth.GetUserAsync(firebaseUid);
                return ConvertToAppUser(userRecord);
            }
            catch (FirebaseAuthException ex) when (ex.ErrorCode == ErrorCode.NotFound)
            {
                _logger.LogDebug("Firebase user with UID '{FirebaseUid}' not found", firebaseUid);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Firebase user by UID '{FirebaseUid}'", firebaseUid);
                return null;
            }
        }

        public async Task<AppUser?> GetUserByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Empty email provided");
                    return null;
                }

                var userRecord = await _firebaseAuth.GetUserByEmailAsync(email);
                return ConvertToAppUser(userRecord);
            }
            catch (FirebaseAuthException ex) when (ex.ErrorCode == ErrorCode.NotFound)
            {
                _logger.LogDebug("Firebase user with email '{Email}' not found", email);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Firebase user by email '{Email}'", email);
                return null;
            }
        }

        public async Task<int> GetUserCountAsync()
        {
            try
            {
                var allUsers = await GetAllFirebaseUserRecordsAsync();
                return allUsers.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Firebase user count");
                return 0;
            }
        }

        private async Task<List<UserRecord>> GetAllFirebaseUserRecordsAsync()
        {
            var allUsers = new List<UserRecord>();

            try
            {
                // List users in batches (Firebase returns 1000 users per page by default)
                var pagedEnumerable = _firebaseAuth.ListUsersAsync(null);
                var responses = pagedEnumerable.AsRawResponses().GetAsyncEnumerator();

                while (await responses.MoveNextAsync())
                {
                    ExportedUserRecords response = responses.Current;
                    allUsers.AddRange(response.Users);

                    // For very large user bases, consider limiting
                    if (allUsers.Count > 5000)
                    {
                        _logger.LogWarning("Reached safety limit of {Limit} Firebase users", 5000);
                        break;
                    }
                }

                _logger.LogInformation("Retrieved {Count} Firebase users", allUsers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Firebase user records");
            }

            return allUsers;
        }

        private AppUser ConvertToAppUser(UserRecord userRecord)
        {
            return new AppUser
            {
                FirebaseUid = userRecord.Uid,
                Email = userRecord.Email,
                DisplayName = userRecord.DisplayName ?? userRecord.Email?.Split('@')[0] ?? "User",
                CreatedAtUtc = userRecord.UserMetaData?.CreationTimestamp ?? DateTime.UtcNow,
                ModifiedAtUtc = userRecord.UserMetaData?.LastSignInTimestamp ?? DateTime.UtcNow
            };
        }
    }
}