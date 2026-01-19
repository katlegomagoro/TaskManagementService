using TaskManagementService.DAL.Models;

namespace TaskManagementService.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AppUser?> GetOrCreateUserAsync(string firebaseIdToken, string email, string displayName);
        Task<AppUser?> GetOrCreateUserFromFirebaseAsync(string firebaseIdToken);
        Task<bool> IsUserAdminAsync(int userId);
        Task<bool> UpdateUserProfileAsync(int userId, string newDisplayName);
        string? GetFirebaseUidFromToken(string idToken);
    }
}