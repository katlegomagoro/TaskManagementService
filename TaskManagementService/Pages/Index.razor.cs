using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using TaskManagementService.DAL.Enums;
using TaskManagementService.Interfaces;
using TaskManagementService.Services;

namespace TaskManagementService.Pages
{
    public partial class Index : ComponentBase
    {
        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [CascadingParameter]
        private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

        [Inject]
        private IPermissionService PermissionService { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            if (AuthenticationStateTask == null) return;

            try
            {
                var authState = await AuthenticationStateTask;
                var user = authState?.User;

                // If not authenticated stay on landing page
                if (user?.Identity?.IsAuthenticated != true)
                    return;

                // Get user ID for permission check
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    // Fallback redirect
                    NavigationManager.NavigateTo("/tasks", replace: true);
                    return;
                }

                // Check permission type for redirect
                var permission = await PermissionService.GetUserPermissionTypeAsync(userId);

                // Redirect based on permission
                var target = permission switch
                {
                    PermissionType.SuperAdmin or PermissionType.Admin => "/all-tasks",
                    _ => "/tasks"  
                };

                NavigationManager.NavigateTo(target, replace: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Index redirect: {ex.Message}");
                // Don't redirect on error
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (AuthenticationStateTask != null)
                {
                    var authState = await AuthenticationStateTask;
                    Console.WriteLine($"Index auth state: IsAuthenticated={authState?.User?.Identity?.IsAuthenticated}");
                    Console.WriteLine($"Index user name: {authState?.User?.Identity?.Name}");
                    Console.WriteLine($"Index claims count: {authState?.User?.Claims?.Count()}");
                }
            }
        }
    }
}