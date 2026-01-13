using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Serilog;
using TaskManagementService.DAL;
using TaskManagementService.Services;

namespace TaskManagementService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();
            builder.Host.UseSerilog();

            // Core services
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();

            builder.Services.AddMudServices();
            builder.Services.AddHttpContextAccessor();

            // Firebase JS interop client
            builder.Services.AddScoped<FirebaseAuthClient>();

            // Database (EF Core + SQL Server) 
            var connectionString = builder.Configuration.GetConnectionString("TaskManagementServiceDb");

            builder.Services.AddDbContext<TaskManagementServiceDbContext>((provider, options) =>
            {
                options.UseSqlServer(connectionString);
            }, ServiceLifetime.Scoped);

            builder.Services.AddDbContextFactory<TaskManagementServiceDbContext>((provider, options) =>
            {
                options.UseSqlServer(connectionString);
            }, ServiceLifetime.Scoped);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                app.UseHsts();

            }

            app.UseSerilogRequestLogging();
            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}
