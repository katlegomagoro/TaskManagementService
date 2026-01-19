using TaskManagementService.DAL.Models;

namespace TaskManagementService.Interfaces
{
    public interface IUserProfileService
    {
        Task<AppUser?> GetUserProfileAsync(int userId);
        Task<AppUser?> UpdateUserProfileAsync(int userId, string displayName);
        Task<bool> ChangeEmailAsync(int userId, string newEmail);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<int> GetUserTaskCountAsync(int userId);
        Task<int> GetCompletedTaskCountAsync(int userId);
    }
}