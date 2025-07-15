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
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Microsoft.OpenApi.Models;
using Quartz;
using StackExchange.Redis;

namespace OpenAutomate.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add application configuration
            ConfigureAppSettings(builder);

            // Configure logging
            ConfigureLogging(builder);

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
            await ApplyDatabaseMigrationsAsync(app);

            await app.RunAsync();
        }
        
        private static void ConfigureAppSettings(WebApplicationBuilder builder)
        {
            builder.Configuration.AddEnvironmentVariables();
            
            // Register configuration sections with the DI container
            var appSettingsSection = builder.Configuration.GetSection("AppSettings");
            builder.Services.Configure<AppSettings>(options => {
                appSettingsSection.Bind(options);
                options.FrontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:3001";
            });
            builder.Services.Configure<JwtSettings>(appSettingsSection.GetSection("Jwt"));
            builder.Services.Configure<DatabaseSettings>(appSettingsSection.GetSection("Database"));
            builder.Services.Configure<CorsSettings>(appSettingsSection.GetSection("Cors"));
            builder.Services.Configure<RedisSettings>(appSettingsSection.GetSection("Redis"));
            builder.Services.Configure<EmailSettings>(appSettingsSection.GetSection("EmailSettings"));
        }
        
        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            // Clear default providers and use Serilog
            builder.Logging.ClearProviders();
            builder.Host.UseSerilog();
        }
        
        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Get configuration for DbContext
            var dbSettings = builder.Configuration
                .GetSection("AppSettings")
                .GetSection("Database")
                .Get<DatabaseSettings>();
                
            // Register TenantContext with caching decorator for proper tenant isolation per request
            builder.Services.AddScoped<TenantContext>();
            builder.Services.AddScoped<ITenantContext>(provider =>
            {
                var innerContext = provider.GetRequiredService<TenantContext>();
                var cacheService = provider.GetRequiredService<ICacheService>();
                var logger = provider.GetRequiredService<ILogger<TenantContextCachingDecorator>>();
                
                return new TenantContextCachingDecorator(innerContext, cacheService, logger);
            });
            
            // Add DbContext
            builder.Services.AddDbContext<ApplicationDbContext>((provider, options) =>
            {
                options.UseSqlServer(dbSettings.DefaultConnection);
            });
            
            // Configure Redis
            ConfigureRedis(builder);
            
            // Configure CORS
            ConfigureCors(builder);
            
            // Register application services
            RegisterApplicationServices(builder);
            
            // Configure Quartz.NET for scheduling
            ConfigureQuartz(builder);
            
            // Add controllers with OData support
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Configure JSON serialization to use camelCase for property names
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    
                    // Configure enums to be serialized as strings instead of integers
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                })
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

            // Configure SignalR with Redis backplane
            ConfigureSignalR(builder);
        }
        
        private static void ConfigureCors(WebApplicationBuilder builder)
        {
            var corsSettings = builder.Configuration
                .GetSection("AppSettings")
                .GetSection("Cors")
                .Get<CorsSettings>();

            // Get frontend URL for additional CORS origins
            var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:3001";

            // Create comprehensive allowed origins list
            var allAllowedOrigins = new List<string>(corsSettings.AllowedOrigins ?? Array.Empty<string>());

            // Add frontend URL if not already present
            if (!allAllowedOrigins.Contains(frontendUrl))
            {
                allAllowedOrigins.Add(frontendUrl);
            }

            // Add common localhost variations for development
            var localhostOrigins = new[]
            {
                "http://localhost:3000", "http://localhost:3001",
                "https://localhost:3000", "https://localhost:3001",
                "http://localhost:5252", "https://localhost:5252"  // Backend URLs
            };

            foreach (var origin in localhostOrigins)
            {
                if (!allAllowedOrigins.Contains(origin))
                {
                    allAllowedOrigins.Add(origin);
                }
            }

            builder.Services.AddCors(options =>
            {
                // Default policy for API endpoints - restrictive but comprehensive
                options.AddDefaultPolicy(policy =>
                    policy.WithOrigins(allAllowedOrigins.ToArray())
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .WithExposedHeaders("Token-Expired"));

                // SignalR hub policy - must allow credentials for authentication
                // Cannot use AllowAnyOrigin() with AllowCredentials()
                options.AddPolicy("SignalRHubPolicy", policy =>
                    policy.WithOrigins(allAllowedOrigins.ToArray())  // Use same origins as default
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()  // Required for SignalR authentication
                          .WithExposedHeaders("Token-Expired")
                          .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));  // Cache preflight for performance
            });
        }
        
        private static void RegisterApplicationServices(WebApplicationBuilder builder)
        {
            // Register core services
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IOrganizationUnitService, OrganizationUnitService>();
            builder.Services.AddScoped<IBotAgentService, BotAgentService>();
            builder.Services.AddScoped<IAssetService, AssetService>();
            builder.Services.AddScoped<IEmailService, AwsSesEmailService>();
            // Register caching service
            builder.Services.AddScoped<ICacheService, RedisCacheService>();
            
            // Register JWT blocklist service
            builder.Services.AddScoped<IJwtBlocklistService, JwtBlocklistService>();
            
            // Register authorization manager with caching decorator
            builder.Services.AddScoped<AuthorizationManager>();
            builder.Services.AddScoped<IAuthorizationManager>(provider =>
            {
                var innerManager = provider.GetRequiredService<AuthorizationManager>();
                var cacheService = provider.GetRequiredService<ICacheService>();
                var tenantContext = provider.GetRequiredService<ITenantContext>();
                var logger = provider.GetRequiredService<ILogger<AuthorizationManagerCachingDecorator>>();
                
                return new AuthorizationManagerCachingDecorator(innerManager, cacheService, tenantContext, logger);
            });

            builder.Services.AddScoped<IExecutionService, ExecutionService>();
            builder.Services.AddScoped<IScheduleService, ScheduleService>();
            builder.Services.AddScoped<IQuartzScheduleManager, QuartzScheduleManager>();
            builder.Services.AddScoped<IQuartzSchemaService, QuartzSchemaService>();
            
            // Register execution trigger service with SignalR support
            builder.Services.AddScoped<IExecutionTriggerService>(provider =>
            {
                var executionService = provider.GetRequiredService<IExecutionService>();
                var botAgentService = provider.GetRequiredService<IBotAgentService>();
                var packageService = provider.GetRequiredService<IAutomationPackageService>();
                var tenantContext = provider.GetRequiredService<ITenantContext>();
                var logger = provider.GetRequiredService<ILogger<ExecutionTriggerService>>();
                var hubContext = provider.GetRequiredService<IHubContext<BotAgentHub>>();

                // Create SignalR sender delegate
                Func<Guid, string, object, Task> signalRSender = async (botAgentId, command, payload) =>
                {
                    await hubContext.Clients.Group($"bot-{botAgentId}")
                        .SendAsync("ReceiveCommand", command, payload);
                };

                return new ExecutionTriggerService(
                    executionService,
                    botAgentService,
                    packageService,
                    tenantContext,
                    logger,
                    signalRSender);
            });

            builder.Services.AddScoped<IOrganizationUnitInvitationService, OrganizationUnitInvitationService>();
            builder.Services.AddScoped<IOrganizationUnitUserService, OrganizationUnitUserService>();

            
            // Register email verification services
            builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            
            // Register AWS configuration and S3 package storage services
            builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection("AWS"));
            builder.Services.AddScoped<IPackageStorageService, S3PackageStorageService>();
            builder.Services.AddScoped<ILogStorageService, S3LogStorageService>();
            builder.Services.AddScoped<IAutomationPackageService, AutomationPackageService>();
            builder.Services.AddScoped<IPackageMetadataService, PackageMetadataService>();
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
                    OnMessageReceived = context =>
                    {
                        // Support SignalR: allow JWT via access_token query string for hub endpoints
                        var accessToken = context.Request.Query["access_token"];
                        var machineKey = context.Request.Query["machineKey"];
                        var path = context.HttpContext.Request.Path;
                        
                        // Check if it's a SignalR hub request
                        if (path.Value != null && path.Value.Contains("/hubs/botagent"))
                        {
                            // If access_token is provided, use it for JWT auth
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                context.Token = accessToken;
                            }
                            
                            // If machineKey is provided, flag it for Hub to handle auth
                            if (!string.IsNullOrEmpty(machineKey))
                            {
                                context.HttpContext.Request.Headers["X-MachineKey"] = machineKey;
                            }
                        }
                        
                        return Task.CompletedTask;
                    },
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

                // Configure file upload support
                options.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                });
            });
        }
        
        private static void ConfigureMiddleware(WebApplication app)
        {
            // Add request logging middleware as early as possible in the pipeline
            app.UseRequestLogging();
            
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                // Add OData route debugging in development
                app.UseODataRouteDebug();
            }
            
            app.UseHttpsRedirection();

            // Enable OData query capabilities
            app.UseODataQueryRequest();

            // Add routing
            app.UseRouting();

            // Apply CORS policy after routing but before authentication
            app.UseCors();

            // Add authentication middleware
            app.UseAuthentication();

            // Add custom middleware after authentication but before authorization
            app.UseJwtAuthentication();
            app.UseTenantResolution();

            // Add authorization middleware
            app.UseAuthorization();
            
            // Map controller endpoints
            app.MapControllers();

            // Add health check endpoint for Docker/AWS health checks
            app.MapGet("/health", () => Results.Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                environment = app.Environment.EnvironmentName,
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown"
            })).AllowAnonymous();

            // Add basic health check that doesn't depend on external services
            app.MapGet("/ping", () => Results.Ok("pong")).AllowAnonymous();

            // Map SignalR hubs with tenant slug in the path
            // Configure to support both JWT and machine key auth
            // Use permissive CORS policy for direct agent connections
            app.MapHub<BotAgentHub>("/{tenant}/hubs/botagent")
                .RequireCors("SignalRHubPolicy");
        }
        
        private static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                try
                {
                    await context.Database.MigrateAsync();
                    Console.WriteLine("Database migrations applied successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred applying migrations: {ex.Message}");
                }

                // Ensure Quartz.NET schema exists after EF migrations
                var quartzSchemaService = scope.ServiceProvider.GetRequiredService<IQuartzSchemaService>();
                try
                {
                    var schemaCreated = await quartzSchemaService.EnsureSchemaExistsAsync();
                    if (schemaCreated)
                    {
                        Console.WriteLine("Quartz.NET schema verified/created successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Warning: Quartz.NET schema creation failed.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred ensuring Quartz.NET schema: {ex.Message}");
                }
            }
        }

        private static void ConfigureQuartz(WebApplicationBuilder builder)
        {
            // Get database connection string
            var dbSettings = builder.Configuration
                .GetSection("AppSettings")
                .GetSection("Database")
                .Get<DatabaseSettings>();

            // Configure Quartz.NET for scheduling
            builder.Services.AddQuartz(options =>
            {
                options.UseMicrosoftDependencyInjectionJobFactory();
                options.UseSimpleTypeLoader();

                // Use ADO.NET job store with SQL Server for persistence
                options.UsePersistentStore(storeOptions =>
                {
                    storeOptions.UseProperties = true;
                    storeOptions.RetryInterval = TimeSpan.FromSeconds(15);
                    storeOptions.UseSqlServer(dbSettings.DefaultConnection);
                    storeOptions.UseJsonSerializer();
                });

                // Configure scheduler
                options.SchedulerId = "OpenAutomate-Scheduler";
                options.SchedulerName = "OpenAutomate Scheduler";
                
                // Configure misfire handling
                options.MisfireThreshold = TimeSpan.FromMinutes(1);
            });

            // Register the Quartz hosted service
            builder.Services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
                options.AwaitApplicationStarted = true;
            });
        }

        private static void ConfigureRedis(WebApplicationBuilder builder)
        {
            // Get Redis settings
            var redisSettings = builder.Configuration
                .GetSection("AppSettings")
                .GetSection("Redis")
                .Get<RedisSettings>();

            // Build Redis connection string with authentication if provided
            var connectionString = redisSettings.ConnectionString;
            if (!string.IsNullOrEmpty(redisSettings.Username) && !string.IsNullOrEmpty(redisSettings.Password))
            {
                // For authenticated Redis, we need to build a more complete connection string
                var configuration = ConfigurationOptions.Parse(connectionString);
                configuration.User = redisSettings.Username;
                configuration.Password = redisSettings.Password;
                configuration.AbortOnConnectFail = redisSettings.AbortOnConnectFail;
                connectionString = configuration.ToString();
            }

            // Add Redis distributed cache
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = redisSettings.InstanceName;
            });

            // Add Redis connection multiplexer as singleton for SignalR backplane
            builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var configuration = ConfigurationOptions.Parse(redisSettings.ConnectionString);
                configuration.AbortOnConnectFail = redisSettings.AbortOnConnectFail;
                
                // Add authentication if provided
                if (!string.IsNullOrEmpty(redisSettings.Username) && !string.IsNullOrEmpty(redisSettings.Password))
                {
                    configuration.User = redisSettings.Username;
                    configuration.Password = redisSettings.Password;
                }
                
                return ConnectionMultiplexer.Connect(configuration);
            });
        }

        private static void ConfigureSignalR(WebApplicationBuilder builder)
        {
            // Get Redis settings for SignalR backplane
            var redisSettings = builder.Configuration
                .GetSection("AppSettings")
                .GetSection("Redis")
                .Get<RedisSettings>();

            // Build Redis connection string with authentication if provided
            var signalRConnectionString = redisSettings.ConnectionString;
            if (!string.IsNullOrEmpty(redisSettings.Username) && !string.IsNullOrEmpty(redisSettings.Password))
            {
                var config = ConfigurationOptions.Parse(redisSettings.ConnectionString);
                config.AbortOnConnectFail = redisSettings.AbortOnConnectFail;
                config.User = redisSettings.Username;
                config.Password = redisSettings.Password;
                signalRConnectionString = config.ToString();
            }

            // Add SignalR services with optimized connection settings and Redis backplane
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

                // Add enhanced logging for connections
                if (builder.Environment.IsDevelopment())
                {
                    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
                }
            })
            // Add Redis backplane for scaling across multiple server instances
            .AddStackExchangeRedis(signalRConnectionString, options =>
            {
                options.Configuration.ChannelPrefix = "OpenAutomate";
            })
            // Add diagnostics to help debug connection issues
            .AddJsonProtocol(options => {
                // Configure SignalR JSON protocol to use camelCase for consistency
                options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.PayloadSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                
                // Configure enums to be serialized as strings for consistency with API
                options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });
        }
    }
}
