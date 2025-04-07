// OpenAutomate.API/Program.cs
using Microsoft.EntityFrameworkCore;
using OpenAutomate.Infrastructure.DbContext;
using OpenAutomate.Infrastructure.Services;
using OpenAutomate.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OpenAutomate.API.Config;
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
            builder.Configuration.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
            builder.Configuration.AddJsonFile("appsettings.json", false, true);
            builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);
            builder.Configuration.AddEnvironmentVariables();
            // Map AppSettings section in appsettings.json file value to AppSetting model
            builder.Configuration.GetSection("AppSettings").Get<AppSettings>(options => options.BindNonPublicProperties = true);
            
            // Add services to the container.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(AppSettings.DefaultConnection));
            
            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy => 
                    policy.WithOrigins("http://localhost:3000")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials());
            });

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
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"])),
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
            builder.Services.AddScoped<IUserService, OpenAutomate.Infrastructure.Services.UserService>();
            builder.Services.AddSingleton<ITenantContext, TenantContext>();

            builder.Configuration.AddEnvironmentVariables();
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

            // Configure WebSockets 
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2),
                AllowedOrigins = { "http://localhost:3000" } 
            });

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
