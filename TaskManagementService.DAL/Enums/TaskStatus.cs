using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagementService.DAL.Enums
{
    public enum TaskStatus
    {
        [EnumMember(Value = "Open")]
        Open = 1,

        [EnumMember(Value = "In Progress")]
        InProgress = 2,

        [EnumMember(Value = "Completed")]
        Completed = 3
    }
}
