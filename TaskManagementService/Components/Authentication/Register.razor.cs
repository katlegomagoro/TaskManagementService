using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using TaskManagementService.Interfaces;
using TaskManagementService.Models;

namespace TaskManagementService.Components.Authentication
{
    public partial class Register : ComponentBase
    {
        [Parameter]
        public EventCallback OnSwitchToLogin { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        private IFirebaseAuthClient FirebaseAuthClient { get; set; } = default!;

        [Inject]
        private IAuthenticationService AuthenticationService { get; set; } = default!;

        [Inject]
        private ILogger<Register> Logger { get; set; } = default!;

        private RegisterModel _registerModel = new();
        private bool _isLoading = false;

        protected override void OnInitialized()
        {
            _isLoading = false;
        }

        // Handler for email field
        private void HandleEmailChanged(string val)
        {
            _registerModel.Email = val;
        }

        // Handler for display name field
        private void HandleDisplayNameChanged(string val)
        {
            _registerModel.DisplayName = val;
        }

        // Handler for password field
        private void HandlePasswordChanged(string val)
        {
            _registerModel.Password = val;
        }

        // Handler for confirm password field
        private void HandleConfirmPasswordChanged(string val)
        {
            _registerModel.ConfirmPassword = val;
        }

        // Handler for terms agreement
        private void HandleAgreeToTermsChanged(bool val)
        {
            _registerModel.AgreeToTerms = val;
        }

        private async Task HandleRegister()
        {
            if (_isLoading)
                return;

            _isLoading = true;
            StateHasChanged();

            try
            {
                var firebaseIdToken = await FirebaseAuthClient.RegisterAsync(_registerModel.Email, _registerModel.Password);

                if (string.IsNullOrEmpty(firebaseIdToken))
                {
                    Snackbar.Add("Registration failed. Please try again.", Severity.Error);
                    _isLoading = false;
                    StateHasChanged();
                    return;
                }

                var appUser = await AuthenticationService.GetOrCreateUserFromFirebaseAsync(firebaseIdToken);

                if (appUser == null)
                {
                    Snackbar.Add("Failed to create user profile", Severity.Error);
                    _isLoading = false;
                    StateHasChanged();
                    return;
                }

                Snackbar.Add("Account created successfully! Please sign in.", Severity.Success);

                await OnSwitchToLogin.InvokeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Registration failed for email: {Email}", _registerModel.Email);

                var errorMessage = ex.Message.ToLowerInvariant();
                if (errorMessage.Contains("email-already-in-use") || errorMessage.Contains("already exists"))
                {
                    Snackbar.Add("Email is already registered. Please sign in instead.", Severity.Warning);
                    await OnSwitchToLogin.InvokeAsync();
                }
                else
                {
                    Snackbar.Add("Registration failed. Please try again.", Severity.Error);
                }

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

        private void SwitchToLogin()
        {
            OnSwitchToLogin.InvokeAsync();
        }
    }
}