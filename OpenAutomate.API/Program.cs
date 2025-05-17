// OpenAutomate.API/Program.cs
using Microsoft.EntityFrameworkCore;
using OpenAutomate.Infrastructure.DbContext;
using OpenAutomate.Infrastructure.Services;
using OpenAutomate.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OpenAutomate.Core.Configurations;
using OpenAutomate.API.Extensions;
using OpenAutomate.Infrastructure.Repositories;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Domain.IRepository;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Reflection;
using System.IO;
using OpenAutomate.API.Hubs;
using Microsoft.AspNetCore.OData;

namespace OpenAutomate.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // Add application configuration
            ConfigureAppSettings(builder);
            
            // Add services to the container
            ConfigureServices(builder);
            
            // Configure authentication system
            ConfigureAuthentication(builder);
            
            // Configure API documentation
            ConfigureSwagger(builder);
            
            var app = builder.Build();
            
            // Configure middleware pipeline
            ConfigureMiddleware(app);
            
            // Apply database migrations
            ApplyDatabaseMigrations(app);
            
            app.Run();
        }
        
        private static void ConfigureAppSettings(WebApplicationBuilder builder)
        {
            builder.Configuration.AddEnvironmentVariables();
            
            // Register configuration sections with the DI container
            var appSettingsSection = builder.Configuration.GetSection("AppSettings");
            builder.Services.Configure<AppSettings>(options => {
                appSettingsSection.Bind(options);
                options.FrontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:3000";
            });
            builder.Services.Configure<JwtSettings>(appSettingsSection.GetSection("Jwt"));
            builder.Services.Configure<DatabaseSettings>(appSettingsSection.GetSection("Database"));
            builder.Services.Configure<CorsSettings>(appSettingsSection.GetSection("Cors"));
            builder.Services.Configure<EmailSettings>(appSettingsSection.GetSection("EmailSettings"));
        }
        
        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Get configuration for DbContext
            var dbSettings = builder.Configuration
                .GetSection("AppSettings")
                .GetSection("Database")
                .Get<DatabaseSettings>();
                
            // Register TenantContext before ApplicationDbContext
            builder.Services.AddSingleton<ITenantContext, TenantContext>();
            
            // Add DbContext
            builder.Services.AddDbContext<ApplicationDbContext>((provider, options) =>
            {
                options.UseSqlServer(dbSettings.DefaultConnection);
            });
            
            // Configure CORS
            ConfigureCors(builder);
            
            // Register application services
            RegisterApplicationServices(builder);
            
            // Add controllers with OData support
            builder.Services.AddControllers()
                .AddOData(options => 
                    options.Select()
                           .Filter()
                           .OrderBy()
                           .Expand()
                           .Count()
                           .SetMaxTop(100));
                           
            // Register OData model
            builder.Services.AddSingleton(provider => ODataExtensions.GetEdmModel());
            
            builder.Services.AddEndpointsApiExplorer();

            // Add SignalR services with optimized connection settings
            builder.Services.AddSignalR(options => 
            {
                // Enable detailed errors only in development
                options.EnableDetailedErrors = builder.Environment.IsDevelopment();
                
                // Increase keepalive interval to reduce network traffic
                // This matches the client-side timing in BotAgentSignalRClient
                options.KeepAliveInterval = TimeSpan.FromMinutes(2);
                
                // Set client timeout to be longer than the keepalive interval
                // to prevent premature disconnections
                options.ClientTimeoutInterval = TimeSpan.FromMinutes(5);
                
                // Set maximum message size (default is 32KB)
                options.MaximumReceiveMessageSize = 64 * 1024; // 64KB
                
                // Reduce streaming buffer capacity for more efficient memory usage
                options.StreamBufferCapacity = 8;
            });
        }
        
        private static void ConfigureCors(WebApplicationBuilder builder)
        {
            var corsSettings = builder.Configuration
                .GetSection("AppSettings")
                .GetSection("Cors")
                .Get<CorsSettings>();
                
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                    policy.WithOrigins(corsSettings.AllowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .WithExposedHeaders("Token-Expired"));
            });
        }
        
        private static void RegisterApplicationServices(WebApplicationBuilder builder)
        {
            // Register core services
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IOrganizationUnitService, OrganizationUnitService>();
            builder.Services.AddScoped<IBotAgentService, BotAgentService>();
            builder.Services.AddScoped<IAssetService, AssetService>();
            builder.Services.AddScoped<IEmailService, AwsSesEmailService>();
            builder.Services.AddScoped<IAuthorizationManager, AuthorizationManager>();
            
            // Register email verification services
            builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
        }
        
        private static void ConfigureAuthentication(WebApplicationBuilder builder)
        {
            // Get JWT settings
            var jwtSettings = builder.Configuration
                .GetSection("AppSettings")
                .GetSection("Jwt")
                .Get<JwtSettings>();
                
            // Setup the authentication service
            var authBuilder = builder.Services.AddAuthentication(options =>
            {
                // Default scheme used for cookie authentication
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // Default scheme for API authentication challenges
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                // Default scheme for handling API authentication
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            });
            
            // Configure JWT authentication
            ConfigureJwtAuthentication(authBuilder, jwtSettings);
            
            // Configure cookie authentication
            ConfigureCookieAuthentication(authBuilder);
        }
        
        private static void ConfigureJwtAuthentication(
            Microsoft.AspNetCore.Authentication.AuthenticationBuilder authBuilder, 
            JwtSettings jwtSettings)
        {
            authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        }
        
        private static void ConfigureCookieAuthentication(
            Microsoft.AspNetCore.Authentication.AuthenticationBuilder authBuilder)
        {
            authBuilder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/access-denied";
            });
        }
    
     
        private static void ConfigureSwagger(WebApplicationBuilder builder)
        {
            builder.Services.AddSwaggerGen(options =>
            {
                // Set up XML comments for Swagger
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
                
                // Add security definition for JWT
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                
                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }
        
        private static void ConfigureMiddleware(WebApplication app)
        {
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                // Add OData route debugging in development
                app.UseODataRouteDebug();
            }
            
            // Apply CORS policy globally
            app.UseCors();
            
            app.UseHttpsRedirection();
            
            // Enable OData query capabilities
            app.UseODataQueryRequest();
            
            // Add routing
            app.UseRouting();
            
            // Add authentication and authorization middleware
            app.UseAuthentication();
            app.UseJwtAuthentication();
            app.UseTenantResolution();
            app.UseAuthorization();
            
            // Map controller endpoints
            app.MapControllers();

            // Map SignalR hub with tenant slug in the path
            app.MapHub<BotAgentHub>("/{tenant}/hubs/botagent");
        }
        
        private static void ApplyDatabaseMigrations(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                try
                {
                    context.Database.Migrate();
                    Console.WriteLine("Database migrations applied successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred applying migrations: {ex.Message}");
                }
            }
        }
    }
}
