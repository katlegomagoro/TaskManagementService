using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using TaskManagementService.DAL;
using TaskManagementService.DAL.Enums;
using TaskManagementService.DAL.Models;
using TaskManagementService.Interfaces;

namespace TaskManagementService.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IDbContextFactory<TaskManagementServiceDbContext> _dbContextFactory;
        private readonly ILogger<AuthenticationService> _logger;
        private static bool _firstUserCreatedAsSuperAdmin = false;
        private static readonly object _lock = new object();

        public AuthenticationService(
            IDbContextFactory<TaskManagementServiceDbContext> dbContextFactory,
            ILogger<AuthenticationService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public string? GetFirebaseUidFromToken(string idToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(idToken) as JwtSecurityToken;

                var firebaseUid = jsonToken?.Claims
                    .FirstOrDefault(c => c.Type == "user_id")?.Value;

                if (string.IsNullOrEmpty(firebaseUid))
                {
                    firebaseUid = jsonToken?.Claims
                        .FirstOrDefault(c => c.Type == "sub")?.Value;
                }

                return firebaseUid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode Firebase ID token");
                return null;
            }
        }

        public async Task<AppUser?> GetOrCreateUserFromFirebaseAsync(string firebaseIdToken)
        {
            var firebaseUid = GetFirebaseUidFromToken(firebaseIdToken);
            if (string.IsNullOrEmpty(firebaseUid))
            {
                _logger.LogWarning("Could not extract Firebase UID from token");
                return null;
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(firebaseIdToken);

            var email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                     ?? email?.Split('@')[0]
                     ?? "User";

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("No email found in Firebase token");
                return null;
            }

            return await GetOrCreateUserAsync(firebaseUid, email, name);
        }

        public async Task<AppUser?> GetOrCreateUserAsync(string firebaseUid, string email, string displayName)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                // Try to find existing user by Firebase UID
                var existingUser = await db.AppUsers
                    .Include(u => u.UserPermissions)
                    .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);

                if (existingUser != null)
                {
                    // Update email/display name if they have changed
                    bool needsUpdate = false;

                    if (existingUser.Email != email)
                    {
                        existingUser.Email = email;
                        needsUpdate = true;
                    }

                    if (existingUser.DisplayName != displayName)
                    {
                        existingUser.DisplayName = displayName;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        existingUser.ModifiedAtUtc = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                    }

                    _logger.LogInformation("User found by Firebase UID: {Email}", email);
                    return existingUser;
                }

                // Try to find by email (in case Firebase UID wasn't stored previously)
                var userByEmail = await db.AppUsers
                    .Include(u => u.UserPermissions)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (userByEmail != null)
                {
                    // Update Firebase UID
                    userByEmail.FirebaseUid = firebaseUid;
                    userByEmail.ModifiedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync();

                    _logger.LogInformation("User found by email and updated with Firebase UID: {Email}", email);
                    return userByEmail;
                }

                // Create new user - CHECK IF THIS IS THE FIRST USER
                var isFirstUser = !await db.AppUsers.AnyAsync();
                var newUser = new AppUser
                {
                    FirebaseUid = firebaseUid,
                    Email = email,
                    DisplayName = displayName,
                    CreatedAtUtc = DateTime.UtcNow,
                    ModifiedAtUtc = DateTime.UtcNow,
                    PermissionType = isFirstUser ? PermissionType.SuperAdmin : PermissionType.User
                };

                // Add default permission based on whether this is the first user
                var defaultPermissionType = isFirstUser ? PermissionType.SuperAdmin : PermissionType.User;
                var userPermission = new UserPermission
                {
                    AppUser = newUser,
                    PermissionType = defaultPermissionType,
                    CreatedAtUtc = DateTime.UtcNow
                };

                newUser.UserPermissions.Add(userPermission);

                db.AppUsers.Add(newUser);
                await db.SaveChangesAsync();

                if (isFirstUser)
                {
                    _logger.LogWarning("FIRST USER CREATED AS SUPERADMIN: {Email}", email);
                }
                else
                {
                    _logger.LogInformation("New user created: {Email} with Firebase UID: {FirebaseUid}", email, firebaseUid);
                }

                return await db.AppUsers
                    .Include(u => u.UserPermissions)
                    .FirstOrDefaultAsync(u => u.Id == newUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrCreateUserAsync for email: {Email}", email);
                throw;
            }
        }

        // the UpdateUserProfileAsync method to handle Firebase 
        public async Task<bool> UpdateUserProfileAsync(int userId, string newDisplayName)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                var user = await db.AppUsers.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for profile update", userId);
                    return false;
                }

                // Update local database
                user.DisplayName = newDisplayName;
                user.ModifiedAtUtc = DateTime.UtcNow;

                await db.SaveChangesAsync();

                _logger.LogInformation("User profile updated for ID {UserId}: {DisplayName}", userId, newDisplayName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for ID {UserId}", userId);
                return false;
            }
        }


        public async Task<bool> IsUserAdminAsync(int userId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var user = await db.AppUsers
                .Include(u => u.UserPermissions)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user?.UserPermissions.Any(p =>
                p.PermissionType == PermissionType.Admin ||
                p.PermissionType == PermissionType.SuperAdmin) ?? false;
        }
    }
}