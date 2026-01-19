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

        // Handler for email field  
        private void HandleEmailChanged(string val)
        {
            _loginModel.Email = val;
        }

        // Handler for password field
        private void HandlePasswordChanged(string val)
        {
            _loginModel.Password = val;
        }

        // Handler for checkbox
        private void HandleRememberMeChanged(bool val)
        {
            _loginModel.RememberMe = val;
        }

        private async Task HandleLogin()
        {
            if (_isLoading) return;

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

                Snackbar.Add($"Welcome back, {displayName}!", Severity.Success);

                // Force authentication state refresh and wait for propagation
                await AuthenticationStateProvider.GetAuthenticationStateAsync();
                // delay for state propagation
                await Task.Delay(200);

                var isAdmin = user.IsInRole("Admin") || user.IsInRole("SuperAdmin");
                NavigationManager.NavigateTo(isAdmin ? "/all-tasks" : "/tasks", replace: true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Login failed for email: {Email}", _loginModel.Email);
                Snackbar.Add("Login failed. Please try again.", Severity.Error);
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }

        private void SwitchToRegister()
        {
            OnSwitchToRegister.InvokeAsync();
        }
    }
}