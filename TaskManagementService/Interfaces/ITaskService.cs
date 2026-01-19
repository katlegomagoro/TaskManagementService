using MudBlazor;
using TaskManagementService.DAL.Enums;
using TaskManagementService.DAL.Models;
using TaskManagementService.Models.ViewModels;
using TaskStatus = TaskManagementService.DAL.Enums.TaskStatus;

namespace TaskManagementService.Interfaces
{
    public interface ITaskService
    {
        // CRUD Operations
        Task<TaskItem?> GetTaskByIdAsync(int taskId, int currentUserId);
        Task<TaskItem> CreateTaskAsync(CreateTaskModel model, int ownerUserId);
        Task<TaskItem?> UpdateTaskAsync(int taskId, UpdateTaskModel model, int currentUserId);
        Task<bool> DeleteTaskAsync(int taskId, int currentUserId);

        // Bulk Operations
        Task<bool> UpdateTaskStatusAsync(int taskId, TaskStatus status, int currentUserId);
        Task<bool> DeleteMultipleTasksAsync(List<int> taskIds, int currentUserId);

        // Data Retrieval with Filtering
        Task<TaskGridData> GetTasksForUserAsync(TaskFilters filters, int currentUserId);
        Task<TaskGridData> GetAllTasksAsync(TaskFilters filters, int currentUserId);

        // Statistics
        Task<TaskStats> GetTaskStatsForUserAsync(int userId);
        Task<TaskStats> GetAllTaskStatsAsync(int currentUserId);

        // Permission Checks
        Task<bool> CanViewTaskAsync(int taskId, int currentUserId);
        Task<bool> CanEditTaskAsync(int taskId, int currentUserId);
        Task<bool> CanDeleteTaskAsync(int taskId, int currentUserId);

        // DataGrid Support
        Task<GridData<TaskViewModel>> LoadTasksGridDataAsync(
            GridState<TaskViewModel> state,
            int currentUserId,
            bool viewAll = false);
    }
}