using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using System.Security.Claims;
using TaskManagementService.DAL;
using TaskManagementService.DAL.Enums;
using TaskManagementService.DAL.Models;
using TaskManagementService.Interfaces;
using TaskManagementService.Services;

namespace TaskManagementService.Pages
{
    public partial class Profile
    {
        private bool _isLoading = true;
        private bool _isSaving = false;
        private bool _hasChanges = false;
        private int _taskCount = 0;
        private int _completedTaskCount = 0;

        private ProfileModel _profileModel = new();
        private AppUser? _originalUser;
        private ClaimsPrincipal? _currentUser;

        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private IDbContextFactory<TaskManagementServiceDbContext> DbContextFactory { get; set; } = default!;
        [Inject] private IAuthenticationService AuthenticationService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                _currentUser = authState.User;

                if (_currentUser?.Identity?.IsAuthenticated != true)
                {
                    NavigationManager.NavigateTo("/auth");
                    return;
                }

                // Get user ID from claims
                var userIdClaim = _currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    await LoadUserProfile(userId);
                    await LoadUserStats(userId);
                }
                else
                {
                    Snackbar.Add("Invalid user ID", Severity.Error);
                    NavigationManager.NavigateTo("/");
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error loading profile: {ex.Message}", Severity.Error);
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadUserProfile(int userId)
        {
            try
            {
                await using var dbContext = await DbContextFactory.CreateDbContextAsync();

                var user = await dbContext.AppUsers
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    Snackbar.Add("User profile not found", Severity.Error);
                    NavigationManager.NavigateTo("/");
                    return;
                }

                _originalUser = user;

                _profileModel = new ProfileModel
                {
                    Id = user.Id,
                    FirebaseUid = user.FirebaseUid,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    PermissionType = user.PermissionType,
                    CreatedAtUtc = user.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm"),
                    ModifiedAtUtc = user.ModifiedAtUtc?.ToString("yyyy-MM-dd HH:mm") ?? "Never"
                };

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error loading user profile: {ex.Message}", Severity.Error);
            }
        }

        private async Task LoadUserStats(int userId)
        {
            try
            {
                await using var dbContext = await DbContextFactory.CreateDbContextAsync();

                // Count total tasks
                _taskCount = await dbContext.TaskItems
                    .CountAsync(t => t.OwnerUserId == userId);

                // Count completed tasks
                _completedTaskCount = await dbContext.TaskItems
                    .CountAsync(t => t.OwnerUserId == userId && t.Status == DAL.Enums.TaskStatus.Completed);

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading stats: {ex.Message}");
            }
        }

        private void OnDisplayNameChanged(ChangeEventArgs e)
        {
            var value = e.Value?.ToString() ?? "";
            if (_originalUser != null)
            {
                _hasChanges = value != _originalUser.DisplayName;
            }
            _profileModel.DisplayName = value;
            StateHasChanged();
        }

        private async Task HandleSaveProfile()
        {
            if (!_hasChanges || _originalUser == null) return;

            _isSaving = true;
            StateHasChanged();

            try
            {
                // Update local database
                bool dbUpdated = await AuthenticationService.UpdateUserProfileAsync(_originalUser.Id, _profileModel.DisplayName);

                if (dbUpdated)
                {
                    // Update the original user reference
                    _originalUser.DisplayName = _profileModel.DisplayName;

                    // Reload user profile to get updated data
                    await LoadUserProfile(_originalUser.Id);
                    _hasChanges = false;

                    Snackbar.Add("Profile updated successfully", Severity.Success);

                    // Update the authentication state
                    var customAuthProvider = (CustomAuthenticationStateProvider)AuthenticationStateProvider;
                    await customAuthProvider.UpdateUserProfileAsync(_originalUser.Id, _profileModel.DisplayName);

                    // Also refresh the authentication state
                    await AuthenticationStateProvider.GetAuthenticationStateAsync();
                }
                else
                {
                    Snackbar.Add("Failed to update profile", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error saving profile: {ex.Message}", Severity.Error);
            }
            finally
            {
                _isSaving = false;
                StateHasChanged();
            }
        }

        private void ResetForm()
        {
            if (_originalUser != null)
            {
                _profileModel.DisplayName = _originalUser.DisplayName;
                _hasChanges = false;
                StateHasChanged();
            }
        }

        private async Task RefreshProfile()
        {
            _isLoading = true;
            StateHasChanged();

            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                    {
                        await LoadUserProfile(userId);
                        await LoadUserStats(userId);
                    }
                }
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }
    }

    public class ProfileModel
    {
        public int Id { get; set; }
        public string FirebaseUid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public PermissionType PermissionType { get; set; }
        public string CreatedAtUtc { get; set; } = string.Empty;
        public string ModifiedAtUtc { get; set; } = string.Empty;
    }
}