using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using TaskManagementService.Interfaces;
using MudBlazor;
using TaskManagementService.Components.Dialogs;
using TaskManagementService.DAL.Enums;
using TaskManagementService.Services;

namespace TaskManagementService.Shared
{
    public partial class NavMenu : ComponentBase
    {
        private bool _homeExpanded = true;
        private bool _tasksExpanded = false;
        private bool _adminExpanded = false;
        private bool _profileExpanded = false;
        private bool _authExpanded = true;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private IFirebaseAuthClient FirebaseAuthClient { get; set; } = default!;

        [Inject]
        private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject]
        private CustomAuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        [Inject]
        private IDialogService DialogService { get; set; } = default!;

        [Inject]
        private IPermissionService PermissionService { get; set; } = default!;

        private int _currentUserId = 0;
        private PermissionType _currentUserPermission = PermissionType.User;
        private bool _isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadUserPermissions();
            await base.OnInitializedAsync();
        }

        private async Task LoadUserPermissions()
        {
            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                    {
                        _currentUserId = userId;

                        // Get permission type from PermissionService instead of claims
                        _currentUserPermission = await PermissionService.GetUserPermissionTypeAsync(userId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user permissions: {ex.Message}");
                _currentUserPermission = PermissionType.User; // Default to User
            }
            finally
            {
                _isLoading = false;
            }
        }

        private bool ShouldShowAdministration(ClaimsPrincipal user)
        {
            if (_isLoading || user?.Identity?.IsAuthenticated != true)
                return false;

            // Show Administration for ALL authenticated users
            // The ManageUserPermissions page will handle what each user can see/do
            return true;
        }

        private bool IsSuperAdmin(ClaimsPrincipal user)
        {
            return _currentUserPermission == PermissionType.SuperAdmin;
        }

        private string GetUserName(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return "Guest";

            return user.Identity.Name
                ?? user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value
                ?? "User";
        }

        private string GetAdministrationText()
        {
            if (_isLoading)
                return "Administration";

            return _currentUserPermission == PermissionType.SuperAdmin
                ? "Administration"
                : "My Settings";
        }

        private string GetPermissionsLinkText()
        {
            if (_isLoading)
                return "Permissions";

            return _currentUserPermission == PermissionType.SuperAdmin
                ? "Manage Permissions"
                : "My Permissions";
        }

        private void OnHomeClick()
        {
            _homeExpanded = true;
            NavigationManager.NavigateTo("/");
        }

        private async Task Logout()
        {
            try
            {
                var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.ContentText, "Are you sure you want to logout?" },
            { x => x.ButtonText, "Yes, Logout" },
            { x => x.Color, Color.Error }
        };

                var options = new DialogOptions
                {
                    CloseButton = true,
                    MaxWidth = MaxWidth.ExtraSmall,
                    CloseOnEscapeKey = true
                };

                var dialog = await DialogService.ShowAsync<ConfirmDialog>("Logout", parameters, options);
                var result = await dialog.Result;

                if (!result.Canceled && result.Data is bool confirm && confirm)
                {
                    await AuthStateProvider.LogoutAsync();

                    // Force a small delay to ensure state is cleared
                    await Task.Delay(100);

                    // Force a refresh of the authentication state
                    await AuthenticationStateProvider.GetAuthenticationStateAsync();

                    // Navigate to home page with replace to clear history
                    NavigationManager.NavigateTo("/", replace: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logout: {ex.Message}");
                // Force navigation anyway
                NavigationManager.NavigateTo("/", true);
            }
        }
    }
}