using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.DbContext
{
    public static class DatabaseInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger<ApplicationDbContext> logger)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                logger.LogInformation("Applying migrations if needed...");
                await context.Database.MigrateAsync();

                logger.LogInformation("Checking for existing users...");
                if (!context.Users.Any())
                {
                    logger.LogInformation("Seeding admin user...");
                    await SeedUsersAsync(context);
                }
                
                logger.LogInformation("Checking for existing bot agents...");
                if (!context.BotAgents.Any())
                {
                    logger.LogInformation("Seeding bot agents...");
                    await SeedBotAgentsAsync(context);
                }
                
                logger.LogInformation("Checking for existing automation packages...");
                if (!context.AutomationPackages.Any())
                {
                    logger.LogInformation("Seeding automation packages...");
                    await SeedAutomationPackagesAsync(context);
                }
                
                logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
        
        private static async Task SeedUsersAsync(ApplicationDbContext context)
        {
            var adminUser = new User
            {
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@openautomate.com",
                Login = "admin",
                // In real app, hash the password properly
                PasswordHash = "AQAAAAEAACcQAAAAEIyvWCE4DWSI09DJnDvZZZkx7qIMKpcjfhArb27VKvvFIXSKNXYnfDHZ7OZnmzl+xQ=="
            };
            
            var devUser = new User
            {
                FirstName = "Developer",
                LastName = "User",
                Email = "dev@openautomate.com",
                Login = "developer",
                // In real app, hash the password properly
                PasswordHash = "AQAAAAEAACcQAAAAEIyvWCE4DWSI09DJnDvZZZkx7qIMKpcjfhArb27VKvvFIXSKNXYnfDHZ7OZnmzl+xQ=="
            };
            
            await context.Users.AddRangeAsync(adminUser, devUser);
            await context.SaveChangesAsync();
        }
        
        private static async Task SeedBotAgentsAsync(ApplicationDbContext context)
        {
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@openautomate.com");
            if (adminUser == null) return;
            
            var botAgents = new List<BotAgent>
            {
                new BotAgent
                {
                    Name = "Finance-Bot-01",
                    MachineName = "FINANCE-PC-01",
                    IpAddress = "192.168.1.100",
                    Status = "Active",
                    OwnerId = adminUser.Id,
                    RegisteredAt = DateTime.UtcNow.AddDays(-10),
                    LastHeartbeat = DateTime.UtcNow
                },
                new BotAgent
                {
                    Name = "HR-Bot-01",
                    MachineName = "HR-PC-01",
                    IpAddress = "192.168.1.101",
                    Status = "Active",
                    OwnerId = adminUser.Id,
                    RegisteredAt = DateTime.UtcNow.AddDays(-5),
                    LastHeartbeat = DateTime.UtcNow
                }
            };
            
            await context.BotAgents.AddRangeAsync(botAgents);
            await context.SaveChangesAsync();
        }
        
        private static async Task SeedAutomationPackagesAsync(ApplicationDbContext context)
        {
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@openautomate.com");
            if (adminUser == null) return;
            
            var packages = new List<AutomationPackage>
            {
                new AutomationPackage
                {
                    Name = "Invoice Processing",
                    Description = "Automates invoice data extraction and processing",
                    CreatorId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new AutomationPackage
                {
                    Name = "Employee Onboarding",
                    Description = "Automates HR onboarding process for new employees",
                    CreatorId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                }
            };
            
            await context.AutomationPackages.AddRangeAsync(packages);
            await context.SaveChangesAsync();
            
            // Add package versions
            foreach (var package in packages)
            {
                var version = new PackageVersion
                {
                    PackageId = package.Id,
                    VersionNumber = "1.0.0",
                    FilePath = $"/packages/{package.Id}/v1.0.0.zip",
                    IsActive = true,
                    CreatedAt = package.CreatedAt.AddHours(2)
                };
                
                await context.PackageVersions.AddAsync(version);
            }
            
            await context.SaveChangesAsync();
        }
    }
} 