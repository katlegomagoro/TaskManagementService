using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using System.Security.Claims;
using TaskManagementService.Components.Dialogs;
using TaskManagementService.DAL;
using TaskManagementService.DAL.Enums;
using TaskManagementService.DAL.Models;
using TaskManagementService.Models.ViewModels;
using TaskManagementService.Services;

namespace TaskManagementService.Pages
{
    public partial class ManageUserPermissions
    {
        // Private fields
        private bool _isLoading = true;
        private bool _canEdit = false;
        private bool _hasChanges = false;
        private string _userSearchTerm = string.Empty;
        private int _currentUserId;
        private PermissionType _currentUserPermission;
        private List<BreadcrumbItem> _breadcrumbItems = new();

        private readonly List<UserPermissionViewModel> _localPermissions = new();
        private readonly List<UserPermissionViewModel> _removedPermissions = new();

        private MudDataGrid<UserPermissionViewModel> _grid = new();

        // Dependency Injection
        [Inject] private IPermissionService PermissionService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private IDbContextFactory<TaskManagementServiceDbContext> DbContextFactory { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated != true)
                {
                    NavigationManager.NavigateTo("/auth");
                    return;
                }

                // Get user ID from claims
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    _currentUserId = userId;
                }

                // Get current user's permission type
                _currentUserPermission = await PermissionService.GetUserPermissionTypeAsync(_currentUserId);

                // Check if user can edit permissions (only SuperAdmin can edit)
                _canEdit = _currentUserPermission == PermissionType.SuperAdmin;

                // Check if user can manage (view) permissions
                var canManage = await PermissionService.CanManagePermissionsAsync(_currentUserId);
                if (!canManage)
                {
                    // If user can't manage permissions (ReadOnly), redirect or show error
                    NavigationManager.NavigateTo("/");
                    return;
                }

                // Setup breadcrumbs based on user role
                SetupBreadcrumbs();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error initializing page: {ex.Message}", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void SetupBreadcrumbs()
        {
            _breadcrumbItems.Clear();

            if (_currentUserPermission == PermissionType.SuperAdmin)
            {
                _breadcrumbItems.Add(new BreadcrumbItem("Home", "/"));
                _breadcrumbItems.Add(new BreadcrumbItem("Administration", "/", disabled: true));
                _breadcrumbItems.Add(new BreadcrumbItem("Manage Permissions", "/ManageUserPermissions", disabled: true));
            }
            else if (_currentUserPermission == PermissionType.Admin)
            {
                _breadcrumbItems.Add(new BreadcrumbItem("Home", "/"));
                _breadcrumbItems.Add(new BreadcrumbItem("Administration", "/", disabled: true));
                _breadcrumbItems.Add(new BreadcrumbItem("View Permissions", "/ManageUserPermissions", disabled: true));
            }
            else
            {
                _breadcrumbItems.Add(new BreadcrumbItem("Home", "/"));
                _breadcrumbItems.Add(new BreadcrumbItem("My Settings", "/", disabled: true));
                _breadcrumbItems.Add(new BreadcrumbItem("My Permissions", "/ManageUserPermissions", disabled: true));
            }
        }

        private async Task<GridData<UserPermissionViewModel>> LoadServerData(GridState<UserPermissionViewModel> state)
        {
            try
            {
                return await PermissionService.LoadUserPermissionsAsync(
                    state,
                    _currentUserId,
                    _userSearchTerm,
                    _localPermissions,
                    _removedPermissions
                );
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error loading permissions: {ex.Message}", Severity.Error);
                return new GridData<UserPermissionViewModel>
                {
                    Items = new List<UserPermissionViewModel>(),
                    TotalItems = 0
                };
            }
        }

        private async Task OnAddUser()
        {
            // Only SuperAdmin can add users
            if (!_canEdit)
            {
                Snackbar.Add("Only SuperAdmin can add user permissions", Severity.Warning);
                return;
            }

            var dialog = await DialogService.ShowAsync<AddUserPermissionDialog>("Add User Permission");
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is AppUser selectedUser)
            {
                // Check if user already has a permission in the grid
                var existingPermission = _localPermissions.FirstOrDefault(p =>
                    p.UserPermission.AppUserId == selectedUser.Id);

                if (existingPermission == null)
                {
                    var newPermission = new UserPermissionViewModel
                    {
                        UserPermission = new UserPermission
                        {
                            AppUserId = selectedUser.Id,
                            AppUser = selectedUser,
                            PermissionType = PermissionType.User,
                            CreatedAtUtc = DateTime.UtcNow
                        },
                        State = new UserPermissionState()
                    };

                    _localPermissions.Add(newPermission);
                    _hasChanges = true;
                    await _grid.ReloadServerData();
                    Snackbar.Add($"Added permission for {selectedUser.DisplayName}", Severity.Success);
                }
                else
                {
                    Snackbar.Add($"{selectedUser.DisplayName} already has a permission", Severity.Warning);
                }
            }
        }

        private async Task OnDeletePermission(UserPermissionViewModel permission)
        {
            // Only SuperAdmin can delete permissions
            if (!_canEdit)
            {
                Snackbar.Add("Only SuperAdmin can delete permissions", Severity.Warning);
                return;
            }

            // Don't allow deleting own permission
            if (permission.UserPermission.AppUserId == _currentUserId)
            {
                Snackbar.Add("You cannot delete your own permission", Severity.Warning);
                return;
            }

            var result = await DialogService.ShowMessageBox(
                "Delete Permission",
                $"Are you sure you want to delete permission for {permission.UserPermission.AppUser.DisplayName}?",
                "Delete", "Cancel");

            if (result == true)
            {
                _removedPermissions.Add(permission);
                _localPermissions.Remove(permission);
                _hasChanges = true;
                await _grid.ReloadServerData();
                Snackbar.Add($"Removed permission for {permission.UserPermission.AppUser.DisplayName}", Severity.Info);
            }
        }

        private async Task OnSearchChange(string searchTerm)
        {
            // Only SuperAdmin and Admin can search all users
            if (_currentUserPermission != PermissionType.SuperAdmin &&
                _currentUserPermission != PermissionType.Admin)
            {
                _userSearchTerm = string.Empty; // Regular users can't search
                Snackbar.Add("Search is only available for SuperAdmin and Admin", Severity.Info);
                return;
            }

            _userSearchTerm = searchTerm;
            await _grid.ReloadServerData();
        }

        private bool CanSave()
        {
            return _hasChanges && _canEdit && _localPermissions.All(p =>
                p.UserPermission.PermissionType != default &&
                p.UserPermission.AppUserId > 0);
        }

        private async Task SaveChanges()
        {
            try
            {
                if (!_canEdit)
                {
                    Snackbar.Add("You don't have permission to save changes", Severity.Error);
                    return;
                }

                await PermissionService.SaveChangesAsync(_localPermissions, _removedPermissions, _currentUserId);

                _localPermissions.Clear();
                _removedPermissions.Clear();
                _hasChanges = false;

                Snackbar.Add("Permissions saved successfully!", Severity.Success);
                await _grid.ReloadServerData();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error saving permissions: {ex.Message}", Severity.Error);
            }
        }

        private Color GetPermissionColor(PermissionType permissionType)
        {
            return permissionType switch
            {
                PermissionType.SuperAdmin => Color.Error,
                PermissionType.Admin => Color.Primary,
                PermissionType.User => Color.Secondary,
                PermissionType.ReadOnly => Color.Info,
                _ => Color.Default
            };
        }

        // Add this method to handle permission type changes in the grid
        private async Task OnPermissionTypeChanged(UserPermissionViewModel item, PermissionType newType)
        {
            if (!_canEdit)
            {
                Snackbar.Add("Only SuperAdmin can change permissions", Severity.Warning);
                return;
            }

            // Don't allow users to change their own permission type
            if (item.UserPermission.AppUserId == _currentUserId)
            {
                Snackbar.Add("You cannot change your own permission type", Severity.Warning);
                return;
            }

            item.UserPermission.PermissionType = newType;
            _hasChanges = true;
            StateHasChanged();
        }

        // Helper methods for the Razor markup
        private string GetPageSubtitle()
        {
            return _currentUserPermission switch
            {
                PermissionType.SuperAdmin => "Manage permissions for all users",
                PermissionType.Admin => "View permissions for all users (read-only)",
                _ => "View your current permissions"
            };
        }

        private string GetRoleDescription()
        {
            return _currentUserPermission switch
            {
                PermissionType.SuperAdmin =>
                    "You are SuperAdmin. You can view and edit permissions for all users.",
                PermissionType.Admin =>
                    "You are Admin. You can view permissions for all users but cannot make changes.",
                PermissionType.User =>
                    "You are a Standard User. You can only view your own permissions.",
                PermissionType.ReadOnly =>
                    "You are Read-Only. You can only view your own permissions.",
                _ => "View your permissions information"
            };
        }
    }
}