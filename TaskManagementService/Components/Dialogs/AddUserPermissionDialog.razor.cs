using Microsoft.AspNetCore.Components;
using MudBlazor;
using TaskManagementService.DAL.Models;
using TaskManagementService.Services;

namespace TaskManagementService.Components.Dialogs
{
    public partial class AddUserPermissionDialog : ComponentBase
    {
        [CascadingParameter]
        private IMudDialogInstance DialogInstance { get; set; } = default!;

        [Inject]
        private IUserService UserService { get; set; } = default!;

        [Inject]
        private ISnackbar Snackbar { get; set; } = default!;

        private string SearchTerm { get; set; } = string.Empty;
        private List<AppUser> FilteredUsers { get; set; } = new();
        private bool IsLoading { get; set; } = false;
        private CancellationTokenSource? _searchCancellationTokenSource;

        protected override async Task OnInitializedAsync()
        {
            await SearchUsers();
        }

        private async Task OnSearch(string term)
        {
            SearchTerm = term;
            await SearchUsers();
        }

        private async Task SearchUsers()
        {
            // Cancel previous search if still running
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();

            IsLoading = true;
            StateHasChanged();

            try
            {
                await Task.Delay(100, _searchCancellationTokenSource.Token); 

                if (!_searchCancellationTokenSource.Token.IsCancellationRequested)
                {
                    FilteredUsers = await UserService.SearchUsersAsync(SearchTerm);
                }
            }
            catch (TaskCanceledException)
            {
                
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error searching users: {ex.Message}", Severity.Error);
                FilteredUsers = new List<AppUser>();
            }
            finally
            {
                if (!_searchCancellationTokenSource.Token.IsCancellationRequested)
                {
                    IsLoading = false;
                    StateHasChanged();
                }
            }
        }

        private void SelectUser(AppUser user)
        {
            DialogInstance.Close<AppUser>(user);
        }

        private void Cancel()
        {
            DialogInstance.Cancel();
        }

        private string GetInitials(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "??";

            var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();

            return displayName.Length >= 2
                ? displayName.Substring(0, 2).ToUpper()
                : displayName.ToUpper();
        }
    }
}