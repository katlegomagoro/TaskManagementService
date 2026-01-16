using TaskManagementService.DAL.Models;

namespace TaskManagementService.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AppUser?> GetOrCreateUserAsync(string firebaseIdToken, string email, string displayName);
        Task<AppUser?> GetOrCreateUserFromFirebaseAsync(string firebaseIdToken);
        Task<bool> IsUserAdminAsync(int userId);
        string? GetFirebaseUidFromToken(string idToken);
    }
}