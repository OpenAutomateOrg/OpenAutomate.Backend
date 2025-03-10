// OpenAutomate.API/Program.cs
using Microsoft.EntityFrameworkCore;
using OpenAutomate.API.Services;
using OpenAutomate.Domain.Interfaces;
using OpenAutomate.Core.Services;
using OpenAutomate.Infrastructure.DbContext;
using OpenAutomate.Infrastructure.Repositories;

namespace OpenAutomate.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register repositories
            builder.Services.AddScoped<IRobotRepository, RobotRepository>();

            // Register services
            builder.Services.AddScoped<RobotService>();

            // Register WebSocket manager as a singleton (shared across all requests)
            builder.Services.AddScoped<WebSocketConnectionManager>();
            // In Program.cs or Startup.cs
            // Register services
            builder.Services.AddSingleton<ConnectionMonitorService>();
            builder.Services.AddSingleton<WebSocketConnectionManager>();
            builder.Services.AddHostedService<ConnectionMonitorService>();
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

            app.UseHttpsRedirection();

            // Configure WebSockets before routing and endpoints
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            });
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2),
                AllowedOrigins = { "*" } // Or specify your allowed origins
            });

            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
