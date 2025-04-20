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
using Microsoft.AspNetCore.Authentication.Google;
using System.Reflection;
using System.IO;

namespace OpenAutomate.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args); 

            builder.Configuration.AddEnvironmentVariables();
            
            // Register configuration sections with the DI container
            var appSettingsSection = builder.Configuration.GetSection("AppSettings");
            builder.Services.Configure<AppSettings>(appSettingsSection);
            builder.Services.Configure<JwtSettings>(appSettingsSection.GetSection("Jwt"));
            builder.Services.Configure<DatabaseSettings>(appSettingsSection.GetSection("Database"));
            builder.Services.Configure<CorsSettings>(appSettingsSection.GetSection("Cors"));
            
            // Get configuration for DbContext
            var dbSettings = appSettingsSection.GetSection("Database").Get<DatabaseSettings>();
            
            // Register TenantContext before ApplicationDbContext
            builder.Services.AddSingleton<ITenantContext, TenantContext>();
            
            // Add services to the container.
            builder.Services.AddDbContext<ApplicationDbContext>((provider, options) =>
            {
                options.UseSqlServer(dbSettings.DefaultConnection);
            });
            
            // Get CORS settings
            var corsSettings = appSettingsSection.GetSection("Cors").Get<CorsSettings>();
            
            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy => 
                    policy.WithOrigins(corsSettings.AllowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .WithExposedHeaders("Token-Expired"));
            });

            // Get JWT settings
            var jwtSettings = appSettingsSection.GetSection("Jwt").Get<JwtSettings>();
            
            // Configure Authentication - properly separate Cookie/Google auth and JWT auth
            builder.Services.AddAuthentication(options =>
            {
                // Default scheme used for cookie authentication (Google auth)
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // Default scheme for API authentication challenges
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.LoginPath = "/api/ExternalAuth/google-login"; // Set login path for cookie auth
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                var googleKeys = builder.Configuration.GetSection("AppSettings:GoogleAuth");
                options.ClientId = googleKeys["ClientId"];
                options.ClientSecret = googleKeys["ClientSecret"];
                options.CallbackPath = "/signin-google";
                options.SaveTokens = true;
                // Set which authentication scheme to use after Google authentication
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            // Register application services
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IOrganizationUnitService, OrganizationUnitService>();
            builder.Services.AddScoped<IBotAgentService, BotAgentService>();
            
            builder.Services.AddScoped<IAuthorizationManager, AuthorizationManager>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Apply CORS policy globally
            app.UseCors();

            app.UseHttpsRedirection();
            

            app.UseAuthentication();
            app.UseJwtAuthentication();
            app.UseTenantResolution();
            
            // Authentication and authorization middleware

            app.UseAuthorization();
            
            app.MapControllers();

            // Automatically apply migrations at startup
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
            
            app.Run();
        }
    }
}
