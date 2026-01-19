using MudBlazor;
using System.ComponentModel.DataAnnotations;
using TaskManagementService.DAL.Enums;
using TaskManagementService.DAL.Models;
using TaskStatus = TaskManagementService.DAL.Enums.TaskStatus;

namespace TaskManagementService.Models.ViewModels
{
    public class TaskViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000, ErrorMessage = "Description cannot exceed 4000 characters")]
        public string? Description { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.Open;

        public int OwnerUserId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        // For UI display
        public bool IsSelected { get; set; }
        public bool IsBeingEdited { get; set; }
        public string StatusColor { get; set; } = string.Empty;
        public string StatusIcon { get; set; } = string.Empty;

        // Conversion methods
        public static TaskViewModel FromEntity(TaskItem task, AppUser owner)
        {
            return new TaskViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                OwnerUserId = task.OwnerUserId,
                OwnerName = owner.DisplayName,
                OwnerEmail = owner.Email,
                CreatedAtUtc = task.CreatedAtUtc,
                ModifiedAtUtc = task.ModifiedAtUtc,
                CompletedAtUtc = task.CompletedAtUtc,
                StatusColor = GetStatusColor(task.Status),
                StatusIcon = GetStatusIcon(task.Status)
            };
        }

        private static string GetStatusColor(TaskStatus status)
        {
            return status switch
            {
                TaskStatus.Open => "#2196F3", // Blue
                TaskStatus.InProgress => "#FF9800", // Orange
                TaskStatus.Completed => "#4CAF50", // Green
                TaskStatus.OnHold => "#9E9E9E", // Gray
                TaskStatus.Cancelled => "#F44336", // Red
                _ => "#757575"
            };
        }

        private static string GetStatusIcon(TaskStatus status)
        {
            return status switch
            {
                TaskStatus.Open => Icons.Material.Filled.RadioButtonUnchecked,
                TaskStatus.InProgress => Icons.Material.Filled.PlayArrow,
                TaskStatus.Completed => Icons.Material.Filled.CheckCircle,
                TaskStatus.OnHold => Icons.Material.Filled.Pause,
                TaskStatus.Cancelled => Icons.Material.Filled.Cancel,
                _ => Icons.Material.Filled.Help
            };
        }
    }

    public class CreateTaskModel
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000, ErrorMessage = "Description cannot exceed 4000 characters")]
        public string? Description { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.Open;
    }

    public class UpdateTaskModel
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000, ErrorMessage = "Description cannot exceed 4000 characters")]
        public string? Description { get; set; }

        public TaskStatus Status { get; set; }
    }

    public class TaskFilters
    {
        public string? SearchTerm { get; set; }
        public TaskStatus? Status { get; set; }
        public int? OwnerUserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IncludeCompleted { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedAtUtc";
        public bool SortDescending { get; set; } = true;
    }

    public class TaskStats
    {
        public int TotalTasks { get; set; }
        public int OpenTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OnHoldTasks { get; set; }
        public int CancelledTasks { get; set; }
        public decimal CompletionRate { get; set; }
        public Dictionary<string, int> TasksByOwner { get; set; } = new();
        public Dictionary<string, int> TasksByStatus { get; set; } = new();
    }

    public class TaskGridData
    {
        public List<TaskViewModel> Tasks { get; set; } = new();
        public int TotalItems { get; set; }
        public TaskStats? Stats { get; set; }
    }
}