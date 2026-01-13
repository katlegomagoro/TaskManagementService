using Microsoft.JSInterop;

namespace TaskManagementService.Services
{
    public class FirebaseAuthClient
    {
        private readonly IJSRuntime _js;

        public FirebaseAuthClient(IJSRuntime js)
        {
            _js = js;
        }

        public Task<string?> LoginAsync(string email, string password) =>
            _js.InvokeAsync<string?>("firebaseAuth.login", email, password).AsTask();

        public Task<string?> RegisterAsync(string email, string password) =>
            _js.InvokeAsync<string?>("firebaseAuth.register", email, password).AsTask();

        public Task LogoutAsync() =>
            _js.InvokeVoidAsync("firebaseAuth.logout").AsTask();

        public Task<string?> GetIdTokenAsync() =>
            _js.InvokeAsync<string?>("firebaseAuth.getIdToken").AsTask();
    }
}
