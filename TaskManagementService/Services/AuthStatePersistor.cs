using System.Security.Claims;

namespace TaskManagementService.Services
{
    public class AuthStatePersistor
    {
        private ClaimsPrincipal? _currentUser;

        public ClaimsPrincipal? CurrentUser
        {
            get => _currentUser;
            private set => _currentUser = value;
        }

        public void SetUser(ClaimsPrincipal user)
        {
            CurrentUser = user;
        }

        public void ClearUser()
        {
            CurrentUser = null;
        }
    }
}