using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagementService.DAL.Models;

namespace TaskManagementService.DAL.Configurations
{
    public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
    {
        public void Configure(EntityTypeBuilder<UserPermission> builder)
        {
            builder.ToTable("UserPermissions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PermissionType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.HasOne(x => x.TaskItem)
                .WithMany(x => x.UserPermissions)
                .HasForeignKey(x => x.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
