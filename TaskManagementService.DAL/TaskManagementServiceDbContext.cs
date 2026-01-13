using Microsoft.EntityFrameworkCore;
using TaskManagementService.DAL.Models;

namespace TaskManagementService.DAL
{
    public class TaskManagementServiceDbContext : DbContext
    {
        public TaskManagementServiceDbContext(DbContextOptions<TaskManagementServiceDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<AppUser> AppUsers => Set<AppUser>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
        public DbSet<TaskItem> TaskItems => Set<TaskItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Applies all IEntityTypeConfiguration<T> from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskManagementServiceDbContext).Assembly);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Store all enums as strings by default (safe for reporting + avoids magic ints)
            configurationBuilder.Properties<Enum>().HaveConversion<string>();
        }
    }
}
