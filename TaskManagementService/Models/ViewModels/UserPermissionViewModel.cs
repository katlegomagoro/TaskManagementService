using TaskManagementService.DAL.Models;
using System.Diagnostics.CodeAnalysis;
using TaskManagementService.DAL.Enums;

namespace TaskManagementService.Models.ViewModels
{
    public class UserPermissionViewModelComparer : IEqualityComparer<UserPermissionViewModel>
    {
        public bool Equals(UserPermissionViewModel? x, UserPermissionViewModel? y)
            => x?.UserPermission?.Id == y?.UserPermission?.Id;

        public int GetHashCode([DisallowNull] UserPermissionViewModel obj)
            => obj.UserPermission.Id.GetHashCode();
    }

    public class UserPermissionViewModel
    {
        public UserPermission UserPermission { get; set; } = null!;
        public UserPermissionState State { get; set; } = new();
    }

    public class UserPermissionState
    {
        public PermissionType? PermissionTypeChoice { get; set; }
        public bool HasPermissionSelected { get; set; }
    }
}