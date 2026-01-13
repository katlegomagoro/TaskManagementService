using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaskState = TaskManagementService.DAL.Enums.TaskStatus;



namespace TaskManagementService.DAL.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public TaskState Status { get; set; } = TaskState.Open;


        // Normal user can only see their tasks -> OwnerUserId is key
        public int OwnerUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        // Navigation
        public AppUser OwnerUser { get; set; } = default!;
        public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
}
