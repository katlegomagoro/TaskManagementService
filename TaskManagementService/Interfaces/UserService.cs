using Microsoft.EntityFrameworkCore;
using TaskManagementService.DAL;
using TaskManagementService.DAL.Models;
using TaskManagementService.Interfaces;

namespace TaskManagementService.Services
{
    public interface IUserService
    {
        Task<List<AppUser>> SearchUsersAsync(string searchTerm);
        Task<List<AppUser>> GetAllUsersAsync();
        Task<AppUser?> GetUserByIdAsync(int userId);
        Task<AppUser?> GetUserByEmailAsync(string email);
    }

    public class UserService : IUserService
    {
        private readonly IDbContextFactory<TaskManagementServiceDbContext> _dbContextFactory;
        private readonly IFirebaseUserSearchService _firebaseUserSearchService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IDbContextFactory<TaskManagementServiceDbContext> dbContextFactory,
            IFirebaseUserSearchService firebaseUserSearchService,
            ILogger<UserService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _firebaseUserSearchService = firebaseUserSearchService;
            _logger = logger;
        }

        public async Task<List<AppUser>> SearchUsersAsync(string searchTerm)
        {
            var dbUsers = await SearchDatabaseUsersAsync(searchTerm);

            List<AppUser> firebaseUsers = new();
            try
            {
                firebaseUsers = await _firebaseUserSearchService.SearchFirebaseUsersAsync(searchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firebase search failed. Continuing with database users only.");
            }

            // Combine and deduplicate by email
            var allUsers = new Dictionary<string, AppUser>();

            // Add database users first (they already exist in our system)
            foreach (var user in dbUsers)
            {
                if (!string.IsNullOrEmpty(user.Email) && !allUsers.ContainsKey(user.Email.ToLower()))
                {
                    allUsers[user.Email.ToLower()] = user;
                }
            }

            // Add Firebase users that aren't already in our database
            foreach (var user in firebaseUsers)
            {
                if (!string.IsNullOrEmpty(user.Email) &&
                    !allUsers.ContainsKey(user.Email.ToLower()) &&
                    !dbUsers.Any(du => du.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    allUsers[user.Email.ToLower()] = user;
                }
            }

            return allUsers.Values.ToList();
        }

        public async Task<List<AppUser>> GetAllUsersAsync()
        {
            var dbUsers = await GetAllDatabaseUsersAsync();
            var firebaseUsers = await _firebaseUserSearchService.GetAllFirebaseUsersAsync();

            // Combine and deduplicate by email
            var allUsers = new Dictionary<string, AppUser>();

            foreach (var user in dbUsers)
            {
                if (!string.IsNullOrEmpty(user.Email) && !allUsers.ContainsKey(user.Email.ToLower()))
                {
                    allUsers[user.Email.ToLower()] = user;
                }
            }

            foreach (var user in firebaseUsers)
            {
                if (!string.IsNullOrEmpty(user.Email) &&
                    !allUsers.ContainsKey(user.Email.ToLower()) &&
                    !dbUsers.Any(du => du.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    allUsers[user.Email.ToLower()] = user;
                }
            }

            return allUsers.Values.ToList();
        }

        private async Task<List<AppUser>> SearchDatabaseUsersAsync(string searchTerm)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await dbContext.AppUsers
                    .OrderBy(u => u.DisplayName)
                    .Take(50)
                    .ToListAsync();
            }

            return await dbContext.AppUsers
                .Where(u =>
                    u.Email.Contains(searchTerm) ||
                    u.DisplayName.Contains(searchTerm))
                .OrderBy(u => u.DisplayName)
                .Take(50)
                .ToListAsync();
        }

        private async Task<List<AppUser>> GetAllDatabaseUsersAsync()
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            return await dbContext.AppUsers
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }

        public async Task<AppUser?> GetUserByIdAsync(int userId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            return await dbContext.AppUsers.FindAsync(userId);
        }

        public async Task<AppUser?> GetUserByEmailAsync(string email)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            return await dbContext.AppUsers
                .FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}