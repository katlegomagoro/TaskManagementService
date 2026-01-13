using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagementService.DAL.Enums
{
    public enum PermissionType
    {
        [EnumMember(Value = "Super Admin")]
        SuperAdmin = 1,

        [EnumMember(Value = "Admin")]
        Admin = 2,

        [EnumMember(Value = "Standard User")]
        User = 3,

        [EnumMember(Value = "Read Only")]
        ReadOnly = 4
    }
}
