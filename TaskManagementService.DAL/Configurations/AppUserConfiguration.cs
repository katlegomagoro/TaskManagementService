using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagementService.DAL.Models;

namespace TaskManagementService.DAL.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.ToTable("AppUsers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FirebaseUid)
                .HasMaxLength(128)
                .IsRequired();

            builder.HasIndex(x => x.FirebaseUid).IsUnique();

            builder.Property(x => x.Email)
                .HasMaxLength(255)
                .IsRequired();

            builder.HasIndex(x => x.Email).IsUnique();

            builder.Property(x => x.DisplayName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.PermissionType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.HasMany(x => x.UserPermissions)
                .WithOne(x => x.AppUser)
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.OwnedTasks)
                .WithOne(x => x.OwnerUser)
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
