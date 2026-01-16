using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using TaskManagementService.DAL;
using TaskManagementService.Interfaces;
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

            builder.Services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.NewestOnTop = false;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.VisibleStateDuration = 5000;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
                config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            });

            // Register Interfaces and Services
            builder.Services.AddScoped<IFirebaseAuthClient, FirebaseAuthClient>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

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

            // Add authentication and authorization

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseSerilogRequestLogging();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            // Apply migrations on startup
            using (var scope = app.Services.CreateScope())
            {
                var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TaskManagementServiceDbContext>>();
                using var dbContext = dbContextFactory.CreateDbContext();
                dbContext.Database.Migrate();
            }

            app.Run();
        }
    }
}