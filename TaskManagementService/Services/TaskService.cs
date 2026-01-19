using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MudBlazor;
using TaskManagementService.DAL;
using TaskManagementService.DAL.Enums;
using TaskManagementService.DAL.Models;
using TaskManagementService.Interfaces;
using TaskManagementService.Models.ViewModels;
using TaskStatus = TaskManagementService.DAL.Enums.TaskStatus;

namespace TaskManagementService.Services
{
    public class TaskService : ITaskService
    {
        private readonly IDbContextFactory<TaskManagementServiceDbContext> _dbContextFactory;
        private readonly ILogger<TaskService> _logger;
        private readonly IPermissionService _permissionService;

        public TaskService(
            IDbContextFactory<TaskManagementServiceDbContext> dbContextFactory,
            ILogger<TaskService> logger,
            IPermissionService permissionService)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _permissionService = permissionService;
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int taskId, int currentUserId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                var task = await db.TaskItems
                    .Include(t => t.OwnerUser)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                    return null;

                // Check if user can view this task
                if (!await CanViewTaskAsync(taskId, currentUserId))
                {
                    _logger.LogWarning("User {UserId} attempted to access task {TaskId} without permission",
                        currentUserId, taskId);
                    return null;
                }

                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task {TaskId} for user {UserId}", taskId, currentUserId);
                throw;
            }
        }

        public async Task<TaskItem> CreateTaskAsync(CreateTaskModel model, int ownerUserId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                var task = new TaskItem
                {
                    Title = model.Title.Trim(),
                    Description = model.Description?.Trim(),
                    Status = model.Status,
                    OwnerUserId = ownerUserId,
                    CreatedAtUtc = DateTime.UtcNow,
                    ModifiedAtUtc = DateTime.UtcNow
                };

                db.TaskItems.Add(task);
                await db.SaveChangesAsync();

                _logger.LogInformation("Task created: {TaskId} by user {UserId}", task.Id, ownerUserId);
                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task for user {UserId}", ownerUserId);
                throw;
            }
        }

        public async Task<TaskItem?> UpdateTaskAsync(int taskId, UpdateTaskModel model, int currentUserId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                var task = await db.TaskItems.FindAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for update", taskId);
                    return null;
                }

                // Check if user can edit this task
                if (!await CanEditTaskAsync(taskId, currentUserId))
                {
                    _logger.LogWarning("User {UserId} attempted to edit task {TaskId} without permission",
                        currentUserId, taskId);
                    return null;
                }

                // Update task properties
                task.Title = model.Title.Trim();
                task.Description = model.Description?.Trim();
                task.Status = model.Status;
                task.ModifiedAtUtc = DateTime.UtcNow;

                // If task is being marked as completed, set CompletedAtUtc
                if (model.Status == TaskStatus.Completed && task.CompletedAtUtc == null)
                {
                    task.CompletedAtUtc = DateTime.UtcNow;
                }
                // If task was completed but status changed to something else, clear CompletedAtUtc
                else if (model.Status != TaskStatus.Completed && task.CompletedAtUtc != null)
                {
                    task.CompletedAtUtc = null;
                }

                await db.SaveChangesAsync();

                _logger.LogInformation("Task updated: {TaskId} by user {UserId}", taskId, currentUserId);
                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId} for user {UserId}", taskId, currentUserId);
                throw;
            }
        }

        public async Task<bool> DeleteTaskAsync(int taskId, int currentUserId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                var task = await db.TaskItems.FindAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for deletion", taskId);
                    return false;
                }

                // Check if user can delete this task
                if (!await CanDeleteTaskAsync(taskId, currentUserId))
                {
                    _logger.LogWarning("User {UserId} attempted to delete task {TaskId} without permission",
                        currentUserId, taskId);
                    return false;
                }

                db.TaskItems.Remove(task);
                await db.SaveChangesAsync();

                _logger.LogInformation("Task deleted: {TaskId} by user {UserId}", taskId, currentUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task {TaskId} for user {UserId}", taskId, currentUserId);
                throw;
            }
        }

        public async Task<bool> UpdateTaskStatusAsync(int taskId, TaskStatus status, int currentUserId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                var task = await db.TaskItems.FindAsync(taskId);
                if (task == null)
                    return false;

                // Check if user can edit this task
                if (!await CanEditTaskAsync(taskId, currentUserId))
                    return false;

                task.Status = status;
                task.ModifiedAtUtc = DateTime.UtcNow;

                // Update CompletedAtUtc if status is Completed
                if (status == TaskStatus.Completed && task.CompletedAtUtc == null)
                {
                    task.CompletedAtUtc = DateTime.UtcNow;
                }
                else if (status != TaskStatus.Completed && task.CompletedAtUtc != null)
                {
                    task.CompletedAtUtc = null;
                }

                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task status {TaskId} to {Status}", taskId, status);
                throw;
            }
        }

        public async Task<bool> DeleteMultipleTasksAsync(List<int> taskIds, int currentUserId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                var tasks = await db.TaskItems
                    .Where(t => taskIds.Contains(t.Id))
                    .ToListAsync();

                // Filter tasks that user can delete
                var deletableTasks = new List<TaskItem>();
                foreach (var task in tasks)
                {
                    if (await CanDeleteTaskAsync(task.Id, currentUserId))
                    {
                        deletableTasks.Add(task);
                    }
                }

                if (!deletableTasks.Any())
                    return false;

                db.TaskItems.RemoveRange(deletableTasks);
                await db.SaveChangesAsync();

                _logger.LogInformation("Deleted {Count} tasks by user {UserId}", deletableTasks.Count, currentUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting multiple tasks for user {UserId}", currentUserId);
                throw;
            }
        }

        public async Task<TaskGridData> GetTasksForUserAsync(TaskFilters filters, int currentUserId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                var query = db.TaskItems
                    .Include(t => t.OwnerUser)
                    .Where(t => t.OwnerUserId == currentUserId);

                query = ApplyFilters(query, filters);

                // Get total count before pagination
                var totalItems = await query.CountAsync();

                // Apply sorting
                query = ApplySorting(query, filters);

                // Apply pagination
                var tasks = await query
                    .Skip((filters.Page - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .ToListAsync();

                // Convert to ViewModels
                var taskViewModels = tasks.Select(t => TaskViewModel.FromEntity(t, t.OwnerUser)).ToList();

                // Get stats
                var stats = await GetTaskStatsForUserAsync(currentUserId);

                return new TaskGridData
                {
                    Tasks = taskViewModels,
                    TotalItems = totalItems,
                    Stats = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for user {UserId}", currentUserId);
                throw;
            }
        }

        public async Task<TaskGridData> GetAllTasksAsync(TaskFilters filters, int currentUserId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                // Check if user has permission to view all tasks
                var userPermission = await _permissionService.GetUserPermissionTypeAsync(currentUserId);
                if (userPermission != PermissionType.SuperAdmin && userPermission != PermissionType.Admin)
                {
                    _logger.LogWarning("User {UserId} with permission {Permission} attempted to view all tasks",
                        currentUserId, userPermission);
                    return new TaskGridData();
                }

                var query = db.TaskItems
                    .Include(t => t.OwnerUser)
                    .AsQueryable();

                query = ApplyFilters(query, filters);

                // Get total count before pagination
                var totalItems = await query.CountAsync();

                // Apply sorting
                query = ApplySorting(query, filters);

                // Apply pagination
                var tasks = await query
                    .Skip((filters.Page - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .ToListAsync();

                // Convert to ViewModels
                var taskViewModels = tasks.Select(t => TaskViewModel.FromEntity(t, t.OwnerUser)).ToList();

                // Get stats
                var stats = await GetAllTaskStatsAsync(currentUserId);

                return new TaskGridData
                {
                    Tasks = taskViewModels,
                    TotalItems = totalItems,
                    Stats = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tasks for user {UserId}", currentUserId);
                throw;
            }
        }

        public async Task<TaskStats> GetTaskStatsForUserAsync(int userId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                var tasks = await db.TaskItems
                    .Where(t => t.OwnerUserId == userId)
                    .ToListAsync();

                return CalculateStats(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task stats for user {UserId}", userId);
                return new TaskStats();
            }
        }

        public async Task<TaskStats> GetAllTaskStatsAsync(int currentUserId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                // Check if user has permission to view all tasks
                var userPermission = await _permissionService.GetUserPermissionTypeAsync(currentUserId);
                if (userPermission != PermissionType.SuperAdmin && userPermission != PermissionType.Admin)
                {
                    return new TaskStats();
                }

                var tasks = await db.TaskItems
                    .Include(t => t.OwnerUser)
                    .ToListAsync();

                var stats = CalculateStats(tasks);

                // Additional stats for all tasks
                var tasksByOwner = tasks
                    .GroupBy(t => t.OwnerUser.DisplayName)
                    .ToDictionary(g => g.Key, g => g.Count());

                stats.TasksByOwner = tasksByOwner;

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all task stats for user {UserId}", currentUserId);
                return new TaskStats();
            }
        }

        private TaskStats CalculateStats(List<TaskItem> tasks)
        {
            var stats = new TaskStats
            {
                TotalTasks = tasks.Count,
                OpenTasks = tasks.Count(t => t.Status == TaskStatus.Open),
                InProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress),
                CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed),
                OnHoldTasks = tasks.Count(t => t.Status == TaskStatus.OnHold),
                CancelledTasks = tasks.Count(t => t.Status == TaskStatus.Cancelled)
            };

            // Calculate completion rate
            if (stats.TotalTasks > 0)
            {
                stats.CompletionRate = Math.Round((decimal)stats.CompletedTasks / stats.TotalTasks * 100, 2);
            }

            // Tasks by status for charts
            stats.TasksByStatus = new Dictionary<string, int>
            {
                { "Open", stats.OpenTasks },
                { "In Progress", stats.InProgressTasks },
                { "Completed", stats.CompletedTasks },
                { "On Hold", stats.OnHoldTasks },
                { "Cancelled", stats.CancelledTasks }
            };

            return stats;
        }

        public async Task<bool> CanViewTaskAsync(int taskId, int currentUserId)
        {
            try
            {
                var userPermission = await _permissionService.GetUserPermissionTypeAsync(currentUserId);

                // SuperAdmin and Admin can view all tasks
                if (userPermission == PermissionType.SuperAdmin || userPermission == PermissionType.Admin)
                    return true;

                // Regular users can only view their own tasks
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var task = await db.TaskItems.FindAsync(taskId);

                return task?.OwnerUserId == currentUserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking view permission for task {TaskId} and user {UserId}",
                    taskId, currentUserId);
                return false;
            }
        }

        public async Task<bool> CanEditTaskAsync(int taskId, int currentUserId)
        {
            try
            {
                var userPermission = await _permissionService.GetUserPermissionTypeAsync(currentUserId);

                // ReadOnly users cannot edit any tasks
                if (userPermission == PermissionType.ReadOnly)
                    return false;

                // SuperAdmin can edit all tasks
                if (userPermission == PermissionType.SuperAdmin)
                    return true;

                // Admin can edit all tasks
                if (userPermission == PermissionType.Admin)
                    return true;

                // Regular users can only edit their own tasks
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var task = await db.TaskItems.FindAsync(taskId);

                return task?.OwnerUserId == currentUserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking edit permission for task {TaskId} and user {UserId}",
                    taskId, currentUserId);
                return false;
            }
        }

        public async Task<bool> CanDeleteTaskAsync(int taskId, int currentUserId)
        {
            try
            {
                var userPermission = await _permissionService.GetUserPermissionTypeAsync(currentUserId);

                // ReadOnly users cannot delete any tasks
                if (userPermission == PermissionType.ReadOnly)
                    return false;

                // SuperAdmin can delete all tasks
                if (userPermission == PermissionType.SuperAdmin)
                    return true;

                // Admin cannot delete tasks (only SuperAdmin can)
                if (userPermission == PermissionType.Admin)
                    return false;

                // Regular users can only delete their own tasks
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var task = await db.TaskItems.FindAsync(taskId);

                return task?.OwnerUserId == currentUserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking delete permission for task {TaskId} and user {UserId}",
                    taskId, currentUserId);
                return false;
            }
        }

        public async Task<GridData<TaskViewModel>> LoadTasksGridDataAsync(
            GridState<TaskViewModel> state,
            int currentUserId,
            bool viewAll = false)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                IQueryable<TaskItem> query;

                if (viewAll)
                {
                    // Check if user has permission to view all tasks
                    var userPermission = await _permissionService.GetUserPermissionTypeAsync(currentUserId);
                    if (userPermission != PermissionType.SuperAdmin && userPermission != PermissionType.Admin)
                    {
                        return new GridData<TaskViewModel>
                        {
                            Items = new List<TaskViewModel>(),
                            TotalItems = 0
                        };
                    }

                    query = db.TaskItems.Include(t => t.OwnerUser).AsQueryable();
                }
                else
                {
                    query = db.TaskItems
                        .Include(t => t.OwnerUser)
                        .Where(t => t.OwnerUserId == currentUserId);
                }

                // Get total count
                var totalItems = await query.CountAsync();

                // Apply sorting
                if (state.SortDefinitions != null && state.SortDefinitions.Any())
                {
                    var sortDefinition = state.SortDefinitions.First();
                    query = sortDefinition.SortBy switch
                    {
                        nameof(TaskViewModel.Title) => sortDefinition.Descending
                            ? query.OrderByDescending(t => t.Title)
                            : query.OrderBy(t => t.Title),
                        nameof(TaskViewModel.Status) => sortDefinition.Descending
                            ? query.OrderByDescending(t => t.Status)
                            : query.OrderBy(t => t.Status),
                        nameof(TaskViewModel.CreatedAtUtc) => sortDefinition.Descending
                            ? query.OrderByDescending(t => t.CreatedAtUtc)
                            : query.OrderBy(t => t.CreatedAtUtc),
                        _ => query.OrderByDescending(t => t.CreatedAtUtc)
                    };
                }
                else
                {
                    query = query.OrderByDescending(t => t.CreatedAtUtc);
                }

                // Apply pagination
                var tasks = await query
                    .Skip(state.Page * state.PageSize)
                    .Take(state.PageSize)
                    .ToListAsync();

                // Convert to ViewModels
                var taskViewModels = tasks.Select(t => TaskViewModel.FromEntity(t, t.OwnerUser)).ToList();

                return new GridData<TaskViewModel>
                {
                    Items = taskViewModels,
                    TotalItems = totalItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading grid data for user {UserId}, viewAll: {ViewAll}",
                    currentUserId, viewAll);
                throw;
            }
        }

        private IQueryable<TaskItem> ApplyFilters(IQueryable<TaskItem> query, TaskFilters filters)
        {
            // Search term filter
            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
            {
                var searchTerm = filters.SearchTerm.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(searchTerm) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchTerm)));
            }

            // Status filter
            if (filters.Status.HasValue)
            {
                query = query.Where(t => t.Status == filters.Status.Value);
            }

            // Owner filter
            if (filters.OwnerUserId.HasValue)
            {
                query = query.Where(t => t.OwnerUserId == filters.OwnerUserId.Value);
            }

            // Date range filter
            if (filters.StartDate.HasValue)
            {
                query = query.Where(t => t.CreatedAtUtc >= filters.StartDate.Value);
            }

            if (filters.EndDate.HasValue)
            {
                var endDate = filters.EndDate.Value.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.CreatedAtUtc <= endDate);
            }

            // Include completed filter
            if (!filters.IncludeCompleted)
            {
                query = query.Where(t => t.Status != TaskStatus.Completed);
            }

            return query;
        }

        private IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> query, TaskFilters filters)
        {
            return filters.SortBy.ToLower() switch
            {
                "title" => filters.SortDescending
                    ? query.OrderByDescending(t => t.Title)
                    : query.OrderBy(t => t.Title),
                "status" => filters.SortDescending
                    ? query.OrderByDescending(t => t.Status)
                    : query.OrderBy(t => t.Status),
                "createdatutc" => filters.SortDescending
                    ? query.OrderByDescending(t => t.CreatedAtUtc)
                    : query.OrderBy(t => t.CreatedAtUtc),
                "modifiedatutc" => filters.SortDescending
                    ? query.OrderByDescending(t => t.ModifiedAtUtc)
                    : query.OrderBy(t => t.ModifiedAtUtc),
                _ => query.OrderByDescending(t => t.CreatedAtUtc)
            };
        }
    }
}