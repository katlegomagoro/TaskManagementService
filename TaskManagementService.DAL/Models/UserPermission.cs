using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagementService.DAL.Enums;

namespace TaskManagementService.DAL.Models
{
    public class UserPermission
    {
        public int Id { get; set; }

        public int AppUserId { get; set; }

        public PermissionType PermissionType { get; set; }

        public int? TaskItemId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public AppUser AppUser { get; set; } = default!;
        public TaskItem? TaskItem { get; set; }
    }
}
