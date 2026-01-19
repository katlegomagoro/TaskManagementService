using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagementService.DAL;
using TaskManagementService.DAL.Models;
using TaskManagementService.Interfaces;

namespace TaskManagementService.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IDbContextFactory<TaskManagementServiceDbContext> _dbContextFactory;
        private readonly ILogger<UserProfileService> _logger;
        private readonly IFirebaseAuthClient _firebaseAuthClient;

        public UserProfileService(
            IDbContextFactory<TaskManagementServiceDbContext> dbContextFactory,
            ILogger<UserProfileService> logger,
            IFirebaseAuthClient firebaseAuthClient)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _firebaseAuthClient = firebaseAuthClient;
        }

        public async Task<AppUser?> GetUserProfileAsync(int userId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                var user = await dbContext.AppUsers
                    .Include(u => u.OwnedTasks)
                    .Include(u => u.UserPermissions)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", userId);
                    return null;
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<AppUser?> UpdateUserProfileAsync(int userId, string displayName)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                var user = await dbContext.AppUsers.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for profile update", userId);
                    return null;
                }

                // Update display name
                user.DisplayName = displayName?.Trim() ?? user.DisplayName;
                user.ModifiedAtUtc = DateTime.UtcNow;

                dbContext.AppUsers.Update(user);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("User profile updated for user ID: {UserId}, new display name: {DisplayName}",
                    userId, displayName);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ChangeEmailAsync(int userId, string newEmail)
        {
            try
            {
                // Get current user to validate
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                var user = await dbContext.AppUsers.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for email change", userId);
                    return false;
                }

                // Note: Email changes should be handled through Firebase
                // This would require Firebase Admin SDK to update email
                // For now, we'll just update it in our database if the Firebase update succeeds

                // In a real implementation, you would:
                // 1. Call Firebase API to update email
                // 2. If successful, update in local database

                _logger.LogInformation("Email change requested for user ID: {UserId}, new email: {NewEmail}",
                    userId, newEmail);

                return false; // Not implemented yet
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing email for user ID: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                // Password changes are handled by Firebase
                // You would need to implement Firebase password reset/change

                _logger.LogInformation("Password change requested for user ID: {UserId}", userId);
                return false; // Not implemented yet
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user ID: {UserId}", userId);
                return false;
            }
        }

        public async Task<int> GetUserTaskCountAsync(int userId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                return await dbContext.TaskItems
                    .Where(t => t.OwnerUserId == userId)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task count for user ID: {UserId}", userId);
                return 0;
            }
        }

        public async Task<int> GetCompletedTaskCountAsync(int userId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                return await dbContext.TaskItems
                    .Where(t => t.OwnerUserId == userId && t.Status == DAL.Enums.TaskStatus.Completed)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completed task count for user ID: {UserId}", userId);
                return 0;
            }
        }
    }
}