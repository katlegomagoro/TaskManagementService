using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TaskManagementService.DAL
{
    public class TaskManagementServiceDbContextFactory
        : IDesignTimeDbContextFactory<TaskManagementServiceDbContext>
    {
        private static IConfiguration Configuration => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        public TaskManagementServiceDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TaskManagementServiceDbContext>();

            optionsBuilder.UseSqlServer(Configuration.GetConnectionString("TaskManagementServiceDb"));

            return new TaskManagementServiceDbContext(optionsBuilder.Options);
        }
    }
}