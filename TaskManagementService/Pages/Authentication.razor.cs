using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace TaskManagementService.Pages
{
    public partial class Authentication
    {
        [Parameter]
        public string? Mode { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        private ILogger<Authentication> Logger { get; set; } = default!;

        private bool _isLoginMode = true;
        private List<BreadcrumbItem> _breadcrumbItems = new();
        private bool _isInitialized = false;

        private string PageTitleText => _isLoginMode ? "Sign In to Your Account" : "Create Your Account";
        private string PageSubtitleText => _isLoginMode
            ? "Welcome back! Please sign in to continue."
            : "Get started with your free account today.";

        protected override void OnInitialized()
        {
            try
            {
                // Determine mode from URL parameter or from being at root
                var isAtRoot = IsRootPage();

                if (!string.IsNullOrEmpty(Mode) && Mode.Equals("register", StringComparison.OrdinalIgnoreCase))
                {
                    _isLoginMode = false;
                }
                else if (isAtRoot)
                {
                    // At root, default to login mode
                    _isLoginMode = true;
                }

                SetupBreadcrumbs();
                _isInitialized = true;

                Logger.LogInformation("Authentication page initialized in {Mode} mode at {Path}",
                    _isLoginMode ? "login" : "register",
                    isAtRoot ? "root" : "auth");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initializing authentication page");
                Snackbar.Add("Failed to initialize authentication page", Severity.Error);
            }
        }

        protected override void OnParametersSet()
        {
            if (!_isInitialized)
                return;

            try
            {
                // Update mode if URL parameter changes
                var wasLoginMode = _isLoginMode;

                if (!string.IsNullOrEmpty(Mode))
                {
                    _isLoginMode = !Mode.Equals("register", StringComparison.OrdinalIgnoreCase);
                }

                if (wasLoginMode != _isLoginMode)
                {
                    SetupBreadcrumbs();
                    Logger.LogInformation("Authentication mode changed to: {Mode}", _isLoginMode ? "login" : "register");
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in OnParametersSet for authentication page");
            }
        }

        private void SetupBreadcrumbs()
        {
            try
            {
                _breadcrumbItems.Clear();

                // Only add breadcrumbs if not on root page
                if (!IsRootPage())
                {
                    _breadcrumbItems.Add(new BreadcrumbItem("Home", "/"));

                    var currentMode = _isLoginMode ? "login" : "register";
                    var currentLabel = _isLoginMode ? "Sign In" : "Register";

                    _breadcrumbItems.Add(new BreadcrumbItem(
                        currentLabel,
                        $"/auth/{currentMode}",
                        disabled: true
                    ));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error setting up breadcrumbs");
            }
        }

        private bool IsRootPage()
        {
            var currentUri = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

            // Check if we're at the root URL (empty or just "/")
            return string.IsNullOrEmpty(currentUri) ||
                   currentUri == "/" ||
                   currentUri.Trim() == "/" ||
                   !currentUri.StartsWith("auth");
        }

        private string GetAuthIcon()
        {
            return _isLoginMode
                ? Icons.Material.Filled.Login
                : Icons.Material.Filled.PersonAdd;
        }

        private void SwitchToRegister()
        {
            try
            {
                _isLoginMode = false;
                if (IsRootPage())
                {
                    NavigationManager.NavigateTo("/auth/register");
                }
                else
                {
                    NavigationManager.NavigateTo("/auth/register");
                }
                SetupBreadcrumbs();
                StateHasChanged();

                Logger.LogInformation("Switched to register mode");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error switching to register mode");
                Snackbar.Add("Failed to switch to registration", Severity.Error);
            }
        }

        private void SwitchToLogin()
        {
            try
            {
                _isLoginMode = true;
                if (IsRootPage())
                {
                    // Stay at root for login
                    NavigationManager.NavigateTo("/");
                }
                else
                {
                    NavigationManager.NavigateTo("/auth");
                }
                SetupBreadcrumbs();
                StateHasChanged();

                Logger.LogInformation("Switched to login mode");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error switching to login mode");
                Snackbar.Add("Failed to switch to login", Severity.Error);
            }
        }

        private async Task HandleSocialLogin(string provider)
        {
            try
            {
                Snackbar.Add($"{provider} login coming soon!", Severity.Info);
                Logger.LogInformation("{Provider} login requested", provider);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling {Provider} login", provider);
                Snackbar.Add($"Failed to initiate {provider} login", Severity.Error);
            }
        }
    }
}