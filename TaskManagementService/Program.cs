using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
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

            // Initialize Firebase FIRST
            InitializeFirebase(builder);

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

            // Blazor auth 
            builder.Services.AddCascadingAuthenticationState();

            // AuthorizationCore for Blazor components (AuthorizeView / AuthorizeRouteView)
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
            builder.Services.AddScoped<ITaskService, TaskService>();

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

        private static void InitializeFirebase(WebApplicationBuilder builder)
        {
            try
            {
                // Check if Firebase app is already created
                if (FirebaseApp.DefaultInstance == null)
                {
                    // Get Firebase configuration from appsettings.json
                    var firebaseProjectId = builder.Configuration["Firebase:ProjectId"];

                    if (string.IsNullOrEmpty(firebaseProjectId))
                    {
                        Log.Logger.Warning("Firebase ProjectId not found in configuration. Firebase will not be initialized.");
                        return;
                    }

                    // Try to initialize with GOOGLE_APPLICATION_CREDENTIALS environment variable
                    var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

                    if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
                    {
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.FromFile(credentialsPath),
                            ProjectId = firebaseProjectId
                        });
                        Log.Logger.Information($"Firebase initialized with credentials from: {credentialsPath}");
                    }
                    else
                    {
                        // Try to get credentials from appsettings
                        var googleCredentialsPath = builder.Configuration["Firebase:CredentialsPath"];

                        if (!string.IsNullOrEmpty(googleCredentialsPath) && File.Exists(googleCredentialsPath))
                        {
                            FirebaseApp.Create(new AppOptions()
                            {
                                Credential = GoogleCredential.FromFile(googleCredentialsPath),
                                ProjectId = firebaseProjectId
                            });
                            Log.Logger.Information($"Firebase initialized with service account credentials from: {googleCredentialsPath}");
                        }
                        else
                        {
                            // Try to initialize with default application credentials
                            FirebaseApp.Create(new AppOptions()
                            {
                                Credential = GoogleCredential.GetApplicationDefault(),
                                ProjectId = firebaseProjectId
                            });
                            Log.Logger.Information("Firebase initialized with default application credentials");
                        }
                    }
                }
                else
                {
                    Log.Logger.Information("Firebase already initialized");
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to initialize Firebase. Some Firebase features may not work.");
                // Don't throw, continue without Firebase
            }
        }
    }
}