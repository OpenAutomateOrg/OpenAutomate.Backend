// OpenAutomate.API/Program.cs
using Microsoft.EntityFrameworkCore;
using OpenAutomate.Infrastructure.DbContext;
using OpenAutomate.Core.Domain.Services;
using OpenAutomate.Infrastructure.Services;
using OpenAutomate.API.Middleware;

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

            // Register services
            builder.Services.AddScoped<ITenantContext, TenantContext>();
            builder.Services.AddHttpContextAccessor();

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
                AllowedOrigins = { "*" } 
            });

            // Add tenant resolution middleware before MVC/API controllers but after authentication
            app.UseAuthentication();
            app.UseTenantResolution();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
