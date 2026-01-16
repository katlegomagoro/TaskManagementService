namespace TaskManagementService.Interfaces
{
    public interface IFirebaseAuthClient
    {
        Task<string?> LoginAsync(string email, string password);
        Task<string?> RegisterAsync(string email, string password);
        Task LogoutAsync();
        Task<string?> GetIdTokenAsync();
    }
}