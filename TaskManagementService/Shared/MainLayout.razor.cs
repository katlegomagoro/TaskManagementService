using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TaskManagementService.Shared
{
    public partial class MainLayout : LayoutComponentBase
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private bool _drawerOpen = true;

        private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

        private bool IsOnAuthPage()
        {
            var currentUri = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

            // Check if we're at the root or auth pages
            return string.IsNullOrEmpty(currentUri) ||
                   currentUri == "/" ||
                   currentUri.StartsWith("auth", StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldShowHeader()
        {
            // Don't show header on auth pages
            return !IsOnAuthPage();
        }
    }
}