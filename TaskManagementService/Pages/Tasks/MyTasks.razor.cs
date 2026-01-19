using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using System.Security.Claims;
using TaskManagementService.Components.Dialogs;
using TaskManagementService.Components.Tasks;
using TaskManagementService.Interfaces;
using TaskManagementService.Services;
using TaskManagementService.DAL.Enums;
using ViewModels = TaskManagementService.Models.ViewModels;
using TaskStatus = TaskManagementService.DAL.Enums.TaskStatus;

namespace TaskManagementService.Pages.Tasks
{
    public partial class MyTasks
    {
        // State
        private bool _isLoading = true;
        private List<ViewModels.TaskViewModel> _tasks = new();
        private List<ViewModels.TaskViewModel> _selectedTasks = new();
        private ViewModels.TaskStats? _stats;
        private ViewModels.TaskFilters _filters = new();
        private int _totalItems = 0;

        // Permissions
        private bool _canCreateTasks = false;
        private bool _canEditTasks = false;
        private bool _canDeleteTasks = false;
        private int _currentUserId;

        // Services
        [Inject] private ITaskService TaskService { get; set; } = default!;
        [Inject] private IPermissionService PermissionService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private ILogger<MyTasks> Logger { get; set; } = default!;


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

                // Get user permission type
                var userPermission = await PermissionService.GetUserPermissionTypeAsync(_currentUserId);

                // Set permissions based on user role
                _canCreateTasks = userPermission != PermissionType.ReadOnly;
                _canEditTasks = userPermission != PermissionType.ReadOnly;
                _canDeleteTasks = userPermission == PermissionType.SuperAdmin || userPermission == PermissionType.User;

                // Load initial data
                await LoadTasks();
                await LoadStats();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initializing MyTasks page");
                Snackbar.Add("Failed to load tasks", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadTasks()
        {
            try
            {
                _isLoading = true;
                StateHasChanged();

                var result = await TaskService.GetTasksForUserAsync(_filters, _currentUserId);
                _tasks = result.Tasks;
                _totalItems = result.TotalItems;

                // Update selected tasks list
                _selectedTasks = _tasks.Where(t => t.IsSelected).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading tasks");
                Snackbar.Add("Failed to load tasks", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadStats()
        {
            try
            {
                _stats = await TaskService.GetTaskStatsForUserAsync(_currentUserId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading task stats");
                // Don't show error for stats - it's not critical
            }
        }

        private async Task OnFiltersChanged(ViewModels.TaskFilters filters)
        {
            _filters = filters;
            await LoadTasks();
        }

        private async Task ApplySearch(string value)
        {
            _filters.SearchTerm = value;
            _filters.Page = 1;
            await LoadTasks();
        }

        private async Task ApplyStatus(TaskStatus? value)
        {
            _filters.Status = value;
            _filters.Page = 1;
            await LoadTasks();
        }

        private async Task ApplyPageSize(int value)
        {
            _filters.PageSize = value;
            _filters.Page = 1;
            await LoadTasks();
        }

        private async Task OnPageChanged(int page)
        {
            _filters.Page = page;
            await LoadTasks();
        }

        private async Task OpenCreateDialog()
        {
            if (!_canCreateTasks)
            {
                Snackbar.Add("You don't have permission to create tasks", Severity.Warning);
                return;
            }

            var parameters = new DialogParameters<TaskForm>
            {
                { x => x.IsCreating, true }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<TaskForm>("Create New Task", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is ViewModels.CreateTaskModel model)
            {
                await CreateTask(model);
            }
        }

        private async Task CreateTask(ViewModels.CreateTaskModel model)
        {
            try
            {
                var task = await TaskService.CreateTaskAsync(model, _currentUserId);
                if (task != null)
                {
                    Snackbar.Add("Task created successfully!", Severity.Success);
                    await LoadTasks();
                    await LoadStats();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating task");
                Snackbar.Add("Failed to create task", Severity.Error);
            }
        }

        private async Task OpenEditDialog(ViewModels.TaskViewModel task)
        {
            if (!_canEditTasks)
            {
                Snackbar.Add("You don't have permission to edit tasks", Severity.Warning);
                return;
            }

            var editModel = new ViewModels.UpdateTaskModel
            {
                Title = task.Title,
                Description = task.Description,
                Status = task.Status
            };

            var parameters = new DialogParameters<TaskForm>
            {
                { x => x.IsCreating, false },
                { x => x.ExistingTask, editModel }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<TaskForm>("Edit Task", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is ViewModels.UpdateTaskModel model)
            {
                await UpdateTask(task.Id, model);
            }
        }

        private async Task UpdateTask(int taskId, ViewModels.UpdateTaskModel model)
        {
            try
            {
                var task = await TaskService.UpdateTaskAsync(taskId, model, _currentUserId);
                if (task != null)
                {
                    Snackbar.Add("Task updated successfully!", Severity.Success);
                    await LoadTasks();
                    await LoadStats();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating task");
                Snackbar.Add("Failed to update task", Severity.Error);
            }
        }

        private async Task DeleteTask(ViewModels.TaskViewModel task)
        {
            if (!_canDeleteTasks)
            {
                Snackbar.Add("You don't have permission to delete tasks", Severity.Warning);
                return;
            }

            var parameters = new DialogParameters<ConfirmDialog>
            {
                { x => x.ContentText, $"Are you sure you want to delete '{task.Title}'?" },
                { x => x.ButtonText, "Yes, Delete" },
                { x => x.Color, Color.Error }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.ExtraSmall,
                CloseOnEscapeKey = true
            };

            var dialog = await DialogService.ShowAsync<ConfirmDialog>("Delete Task", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is bool confirm && confirm)
            {
                try
                {
                    var success = await TaskService.DeleteTaskAsync(task.Id, _currentUserId);
                    if (success)
                    {
                        Snackbar.Add("Task deleted successfully!", Severity.Success);
                        await LoadTasks();
                        await LoadStats();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error deleting task");
                    Snackbar.Add("Failed to delete task", Severity.Error);
                }
            }
        }

        private async Task UpdateTaskStatus(ViewModels.TaskViewModel task, TaskStatus status)
        {
            if (!_canEditTasks)
            {
                Snackbar.Add("You don't have permission to update task status", Severity.Warning);
                return;
            }

            try
            {
                var success = await TaskService.UpdateTaskStatusAsync(task.Id, status, _currentUserId);
                if (success)
                {
                    Snackbar.Add($"Task marked as {status}", Severity.Success);
                    await LoadTasks();
                    await LoadStats();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating task status");
                Snackbar.Add("Failed to update task status", Severity.Error);
            }
        }

        private async Task DeleteSelectedTasks()
        {
            if (!_selectedTasks.Any())
                return;

            if (!_canDeleteTasks)
            {
                Snackbar.Add("You don't have permission to delete tasks", Severity.Warning);
                return;
            }

            var parameters = new DialogParameters<ConfirmDialog>
            {
                { x => x.ContentText, $"Are you sure you want to delete {_selectedTasks.Count} selected task(s)?" },
                { x => x.ButtonText, "Yes, Delete All" },
                { x => x.Color, Color.Error }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.ExtraSmall,
                CloseOnEscapeKey = true
            };

            var dialog = await DialogService.ShowAsync<ConfirmDialog>("Delete Selected Tasks", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is bool confirm && confirm)
            {
                try
                {
                    var taskIds = _selectedTasks.Select(t => t.Id).ToList();
                    var success = await TaskService.DeleteMultipleTasksAsync(taskIds, _currentUserId);
                    if (success)
                    {
                        Snackbar.Add($"Deleted {_selectedTasks.Count} task(s)", Severity.Success);
                        await LoadTasks();
                        await LoadStats();
                        _selectedTasks.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error deleting selected tasks");
                    Snackbar.Add("Failed to delete tasks", Severity.Error);
                }
            }
        }

        private void ClearSelection()
        {
            foreach (var task in _tasks)
            {
                task.IsSelected = false;
            }
            _selectedTasks.Clear();
            StateHasChanged();
        }

        private Color GetStatusColor(TaskStatus status)
        {
            return status switch
            {
                TaskStatus.Open => Color.Info,
                TaskStatus.InProgress => Color.Warning,
                TaskStatus.Completed => Color.Success,
                TaskStatus.OnHold => Color.Default,
                TaskStatus.Cancelled => Color.Error,
                _ => Color.Default
            };
        }
    }
}