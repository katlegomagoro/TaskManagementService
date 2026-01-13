using System.Reflection;
using System.Runtime.Serialization;

namespace TaskManagementService.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var member = value.GetType()
                              .GetMember(value.ToString())
                              .FirstOrDefault();

            var attribute = member?.GetCustomAttribute<EnumMemberAttribute>();

            return attribute?.Value ?? value.ToString();
        }
    }
}
