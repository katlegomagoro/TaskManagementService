using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using System.Security.Claims;
using TaskManagementService.Interfaces;
using TaskManagementService.Models;
using TaskManagementService.Services;

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

        [Inject]
        private CustomAuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        [Inject]
        private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

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
                var user = await AuthStateProvider.LoginAsync(_loginModel.Email, _loginModel.Password);

                if (user.Identity?.IsAuthenticated != true)
                {
                    Snackbar.Add("Invalid email or password", Severity.Error);
                    return;
                }

                var displayName = user.FindFirst(ClaimTypes.Name)?.Value ?? "User";
                var isAdmin = user.IsInRole("Admin") || user.IsInRole("SuperAdmin");

                Snackbar.Add($"Welcome back, {displayName}!", Severity.Success);

                // CRITICAL: Wait for the authentication state to propagate
                await Task.Delay(100); // Small delay to ensure state is updated

                // Force a re-evaluation of the authentication state
                await AuthenticationStateProvider.GetAuthenticationStateAsync();

                // Navigate after authentication state is properly set
                NavigationManager.NavigateTo(isAdmin ? "/dashboard" : "/tasks", forceLoad: false);
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