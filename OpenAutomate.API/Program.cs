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
            
            // Add services to the container.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(dbSettings.DefaultConnection));
            
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
            
            // Add JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Bearer";
                options.DefaultChallengeScheme = "Bearer";
            })
            .AddJwtBearer(options =>
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

            // Register application services
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddSingleton<ITenantContext, TenantContext>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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

            // Configure WebSockets with allowedOrigins
            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            };
            
            // Add allowed origins properly
            foreach (var origin in corsSettings.AllowedOrigins)
            {
                webSocketOptions.AllowedOrigins.Add(origin);
            }
            
            app.UseWebSockets(webSocketOptions);

            // Add tenant resolution middleware before MVC/API controllers but after authentication
            app.UseAuthentication();
            app.UseJwtAuthentication();
            app.UseTenantResolution();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
