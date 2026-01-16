using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using TaskManagementService.Interfaces;
using TaskManagementService.Models;

namespace TaskManagementService.Components.Authentication
{
    public partial class Login : ComponentBase
    {
        [Parameter]
        public EventCallback OnSwitchToRegister { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        private IFirebaseAuthClient FirebaseAuthClient { get; set; } = default!;

        [Inject]
        private IAuthenticationService AuthenticationService { get; set; } = default!;

        [Inject]
        private ILogger<Login> Logger { get; set; } = default!;

        private LoginModel _loginModel = new();
        private bool _isLoading = false;

        protected override void OnInitialized()
        {
            _isLoading = false;
        }

        // Handler for email field - non-nullable string
        private void HandleEmailChanged(string val)
        {
            _loginModel.Email = val;
        }

        // Handler for password field - non-nullable string  
        private void HandlePasswordChanged(string val)
        {
            _loginModel.Password = val;
        }

        // Handler for checkbox - non-nullable bool
        private void HandleRememberMeChanged(bool val)
        {
            _loginModel.RememberMe = val;
        }

        private async Task HandleLogin()
        {
            if (_isLoading)
                return;

            _isLoading = true;
            StateHasChanged();

            try
            {
                var firebaseIdToken = await FirebaseAuthClient.LoginAsync(_loginModel.Email, _loginModel.Password);

                if (string.IsNullOrEmpty(firebaseIdToken))
                {
                    Snackbar.Add("Invalid email or password", Severity.Error);
                    _isLoading = false;
                    StateHasChanged();
                    return;
                }

                var appUser = await AuthenticationService.GetOrCreateUserFromFirebaseAsync(firebaseIdToken);

                if (appUser == null)
                {
                    Snackbar.Add("Failed to sync user account. Please try again.", Severity.Error);
                    _isLoading = false;
                    StateHasChanged();
                    return;
                }

                var isAdmin = await AuthenticationService.IsUserAdminAsync(appUser.Id);

                Snackbar.Add($"Welcome back, {appUser.DisplayName}!", Severity.Success);

                if (isAdmin)
                {
                    NavigationManager.NavigateTo("/dashboard", true);
                }
                else
                {
                    NavigationManager.NavigateTo("/tasks", true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Login failed for email: {Email}", _loginModel.Email);
                Snackbar.Add("Login failed. Please try again.", Severity.Error);
                _isLoading = false;
                StateHasChanged();
            }
            finally
            {
                if (_isLoading)
                {
                    _isLoading = false;
                    StateHasChanged();
                }
            }
        }

        private void SwitchToRegister()
        {
            OnSwitchToRegister.InvokeAsync();
        }
    }
}