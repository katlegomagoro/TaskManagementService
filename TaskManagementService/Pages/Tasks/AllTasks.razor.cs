using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using System.Security.Claims;
using TaskManagementService.Components.Dialogs;
using TaskManagementService.Components.Tasks;
using TaskManagementService.DAL.Enums;
using TaskManagementService.Interfaces;
using TaskManagementService.Models.ViewModels;
using TaskManagementService.Services;
using TaskStatus = TaskManagementService.DAL.Enums.TaskStatus;

namespace TaskManagementService.Pages.Tasks
{
    public partial class AllTasks
    {
        // State
        private bool _isLoading = true;
        private bool _hasPermission = false;
        private HashSet<TaskViewModel> _selectedTasks = new();
        private TaskStats? _stats;
        private MudDataGrid<TaskViewModel>? _grid;

        // Permissions
        private bool _canEditAllTasks = false;
        private bool _canDeleteAllTasks = false;
        private int _currentUserId;
        private PermissionType _currentUserPermission;

        // Services
        [Inject] private ITaskService TaskService { get; set; } = default!;
        [Inject] private IPermissionService PermissionService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private ILogger<AllTasks> Logger { get; set; } = default!;

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
                _currentUserPermission = await PermissionService.GetUserPermissionTypeAsync(_currentUserId);

                // Check if user has permission to view all tasks
                _hasPermission = _currentUserPermission == PermissionType.SuperAdmin ||
                                _currentUserPermission == PermissionType.Admin;

                if (!_hasPermission)
                {
                    _isLoading = false;
                    return;
                }

                // Set permissions
                _canEditAllTasks = _currentUserPermission == PermissionType.SuperAdmin ||
                                  _currentUserPermission == PermissionType.Admin;
                _canDeleteAllTasks = _currentUserPermission == PermissionType.SuperAdmin;

                // Load stats
                await LoadStats();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initializing AllTasks page");
                Snackbar.Add("Failed to load tasks", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task<GridData<TaskViewModel>> LoadServerData(GridState<TaskViewModel> state)
        {
            try
            {
                return await TaskService.LoadTasksGridDataAsync(state, _currentUserId, viewAll: true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading grid data");
                Snackbar.Add("Failed to load tasks", Severity.Error);
                return new GridData<TaskViewModel>
                {
                    Items = new List<TaskViewModel>(),
                    TotalItems = 0
                };
            }
        }

        private async Task LoadStats()
        {
            try
            {
                _stats = await TaskService.GetAllTaskStatsAsync(_currentUserId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading task stats");
                // Don't show error for stats
            }
        }

        private async Task OpenCreateDialog()
        {
            if (!_canEditAllTasks)
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

            if (!result.Canceled && result.Data is CreateTaskModel model)
            {
                await CreateTask(model);
            }
        }

        private async Task CreateTask(CreateTaskModel model)
        {
            try
            {
                var task = await TaskService.CreateTaskAsync(model, _currentUserId);
                if (task != null)
                {
                    Snackbar.Add("Task created successfully!", Severity.Success);
                    if (_grid != null)
                    {
                        await _grid.ReloadServerData();
                    }
                    await LoadStats();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating task");
                Snackbar.Add("Failed to create task", Severity.Error);
            }
        }

        private async Task OpenEditDialog(TaskViewModel task)
        {
            if (!_canEditAllTasks)
            {
                Snackbar.Add("You don't have permission to edit tasks", Severity.Warning);
                return;
            }

            var editModel = new UpdateTaskModel
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

            if (!result.Canceled && result.Data is UpdateTaskModel model)
            {
                await UpdateTask(task.Id, model);
            }
        }

        private async Task UpdateTask(int taskId, UpdateTaskModel model)
        {
            try
            {
                var task = await TaskService.UpdateTaskAsync(taskId, model, _currentUserId);
                if (task != null)
                {
                    Snackbar.Add("Task updated successfully!", Severity.Success);
                    if (_grid != null)
                    {
                        await _grid.ReloadServerData();
                    }
                    await LoadStats();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating task");
                Snackbar.Add("Failed to update task", Severity.Error);
            }
        }

        private async Task DeleteTask(TaskViewModel task)
        {
            if (!_canDeleteAllTasks)
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
                        if (_grid != null)
                        {
                            await _grid.ReloadServerData();
                        }
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

        private async Task DeleteSelectedTasks()
        {
            if (!_selectedTasks.Any())
                return;

            if (!_canDeleteAllTasks)
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
                        if (_grid != null)
                        {
                            await _grid.ReloadServerData();
                        }
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

        private async Task OnSelectedItemsChanged(HashSet<TaskViewModel> selectedItems)
        {
            _selectedTasks = selectedItems;
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnCellTitleChanged(MudBlazor.CellContext<TaskViewModel> edit, string value)
        {
            edit.Item.Title = value;
            StateHasChanged();

            // Auto-save the change
            try
            {
                var updateModel = new UpdateTaskModel
                {
                    Title = edit.Item.Title,
                    Description = edit.Item.Description,
                    Status = edit.Item.Status
                };

                var task = await TaskService.UpdateTaskAsync(edit.Item.Id, updateModel, _currentUserId);
                if (task != null)
                {
                    Snackbar.Add("Task title updated", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating task title");
                Snackbar.Add("Failed to update task", Severity.Error);
            }
        }

        private async Task OnCellDescriptionChanged(MudBlazor.CellContext<TaskViewModel> edit, string value)
        {
            edit.Item.Description = value;
            StateHasChanged();

            // Auto-save the change
            try
            {
                var updateModel = new UpdateTaskModel
                {
                    Title = edit.Item.Title,
                    Description = edit.Item.Description,
                    Status = edit.Item.Status
                };

                var task = await TaskService.UpdateTaskAsync(edit.Item.Id, updateModel, _currentUserId);
                if (task != null)
                {
                    Snackbar.Add("Task description updated", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating task description");
                Snackbar.Add("Failed to update task", Severity.Error);
            }
        }

        private async Task OnCellStatusChanged(MudBlazor.CellContext<TaskViewModel> edit, TaskStatus value)
        {
            edit.Item.Status = value;
            StateHasChanged();

            // Auto-save the change
            try
            {
                var updateModel = new UpdateTaskModel
                {
                    Title = edit.Item.Title,
                    Description = edit.Item.Description,
                    Status = edit.Item.Status
                };

                var task = await TaskService.UpdateTaskAsync(edit.Item.Id, updateModel, _currentUserId);
                if (task != null)
                {
                    Snackbar.Add("Task status updated", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating task status");
                Snackbar.Add("Failed to update task", Severity.Error);
            }
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