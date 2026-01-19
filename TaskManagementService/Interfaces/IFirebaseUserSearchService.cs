using TaskManagementService.DAL.Models;

namespace TaskManagementService.Interfaces
{
    public interface IFirebaseUserSearchService
    {
        Task<List<AppUser>> SearchFirebaseUsersAsync(string searchTerm);
        Task<List<AppUser>> GetAllFirebaseUsersAsync();
        Task<AppUser?> GetUserByUidAsync(string firebaseUid);
        Task<AppUser?> GetUserByEmailAsync(string email);
        Task<int> GetUserCountAsync();
    }
}