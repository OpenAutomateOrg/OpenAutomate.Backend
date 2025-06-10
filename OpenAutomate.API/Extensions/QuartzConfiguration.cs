using Quartz;
using OpenAutomate.Core.Configurations;

namespace OpenAutomate.API.Extensions
{
    /// <summary>
    /// Extension methods for configuring Quartz.NET scheduling services
    /// </summary>
    public static class QuartzConfiguration
    {
        /// <summary>
        /// Adds Quartz.NET scheduling services to the dependency injection container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddQuartzScheduling(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure Quartz.NET
            services.AddQuartz(q =>
            {
                // Microsoft DI is used by default in Quartz 3.7+
                
                // Use simple type loader for job types
                q.UseSimpleTypeLoader();
                
                // Configure thread pool
                q.UseDefaultThreadPool(tp => 
                {
                    tp.MaxConcurrency = Environment.ProcessorCount * 2; // Scale with CPU cores
                });
                
                // Set scheduler instance name and ID
                q.SchedulerName = "OpenAutomate-Scheduler";
                q.SchedulerId = Environment.MachineName + "-" + Environment.ProcessId;
                
                // Configure job store based on environment
                var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
                
                if (isDevelopment)
                {
                    // Use in-memory store for development
                    q.UseInMemoryStore();
                }
                else
                {
                    // Use persistent SQL Server store for production
                    q.UsePersistentStore(s =>
                    {
                        s.UseProperties = true;
                        s.RetryInterval = TimeSpan.FromSeconds(15);
                        s.UseSqlServer(connectionString => 
                        {
                            connectionString.ConnectionString = GetConnectionString(configuration);
                            connectionString.TablePrefix = "QRTZ_";
                        });
                        s.UseNewtonsoftJsonSerializer();
                        s.UseClustering(c =>
                        {
                            c.CheckinMisfireThreshold = TimeSpan.FromMinutes(2);
                            c.CheckinInterval = TimeSpan.FromMinutes(1);
                        });
                    });
                }
                
                // JSON serialization is configured above in UsePersistentStore
            });
            
            // Add Quartz.NET hosted service
            services.AddQuartzHostedService(q => 
            {
                q.WaitForJobsToComplete = true;
                q.AwaitApplicationStarted = true;
            });
            
            return services;
        }
        
        /// <summary>
        /// Gets the database connection string from configuration
        /// </summary>
        private static string GetConnectionString(IConfiguration configuration)
        {
            var dbSettings = configuration
                .GetSection("AppSettings")
                .GetSection("Database")
                .Get<DatabaseSettings>();
                
            return dbSettings?.DefaultConnection ?? 
                   throw new InvalidOperationException("Database connection string not found in configuration");
        }
    }
} 