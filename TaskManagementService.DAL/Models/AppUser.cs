using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagementService.DAL.Enums;

namespace TaskManagementService.DAL.Models
{
    public class AppUser
    {
        public int Id { get; set; }

        public string FirebaseUid { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        public PermissionType PermissionType { get; set; } = PermissionType.User;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAtUtc { get; set; }

        public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
        public ICollection<TaskItem> OwnedTasks { get; set; } = new List<TaskItem>();
    }
}

