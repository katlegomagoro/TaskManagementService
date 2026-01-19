using Microsoft.AspNetCore.Components.Authorization;
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

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();
            builder.Host.UseSerilog();

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

            builder.Services.AddScoped<IPermissionService, PermissionService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IFirebaseUserSearchService, FirebaseUserSearchService>();
            builder.Services.AddScoped<IFirebaseAuthClient, FirebaseAuthClient>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

            var connectionString = builder.Configuration.GetConnectionString("TaskManagementServiceDb");

            builder.Services.AddDbContext<TaskManagementServiceDbContext>((provider, options) =>
            {
                options.UseSqlServer(connectionString);
            }, ServiceLifetime.Scoped);

            builder.Services.AddDbContextFactory<TaskManagementServiceDbContext>((provider, options) =>
            {
                options.UseSqlServer(connectionString);
            }, ServiceLifetime.Scoped);

            // Blazor auth (component-level)
            builder.Services.AddCascadingAuthenticationState();

            // IMPORTANT: Use AuthorizationCore for Blazor components (AuthorizeView / AuthorizeRouteView)
            builder.Services.AddAuthorizationCore(options =>
            {
                options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
                options.AddPolicy("AdminOrAbove", policy => policy.RequireRole("Admin", "SuperAdmin"));
                options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
            });

            builder.Services.AddScoped<AuthStatePersistor>();
            builder.Services.AddScoped<AuthLocalStorageService>();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            builder.Services.AddScoped<CustomAuthenticationStateProvider>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseSerilogRequestLogging();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // REMOVE these: you are not setting any ASP.NET auth scheme (cookie/JWT),
            // and protecting endpoints will break /_blazor connections.
            // app.UseAuthentication();
            // app.UseAuthorization();

            app.MapBlazorHub();           
            app.MapFallbackToPage("/_Host");

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