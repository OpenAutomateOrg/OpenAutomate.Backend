using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Dto.Execution;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.Services;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Jobs
{
    /// <summary>
    /// Quartz.NET job that triggers automation package executions based on schedules
    /// </summary>
    public class TriggerExecutionJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TriggerExecutionJob> _logger;

        public TriggerExecutionJob(IServiceProvider serviceProvider, ILogger<TriggerExecutionJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            
            // Parse Guids from strings since Quartz stores them as strings
            if (!Guid.TryParse(jobDataMap.GetString("ScheduleId"), out var scheduleId))
            {
                _logger.LogError("Invalid ScheduleId in job data map");
                return;
            }
            
            if (!Guid.TryParse(jobDataMap.GetString("TenantId"), out var tenantId))
            {
                _logger.LogError("Invalid TenantId in job data map");
                return;
            }
            
            var tenantSlug = jobDataMap.GetString("TenantSlug");

            _logger.LogInformation("Executing scheduled job for Schedule {ScheduleId} in tenant {TenantId}", 
                scheduleId, tenantId);

            // Create a new scope for this job execution to get scoped services
            using var scope = _serviceProvider.CreateScope();
            
            try
            {
                // Get scoped services - get concrete TenantContext to set tenant info
                var concreteTenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
                var scheduleService = scope.ServiceProvider.GetRequiredService<IScheduleService>();
                var executionTriggerService = scope.ServiceProvider.GetRequiredService<IExecutionTriggerService>();
                var packageService = scope.ServiceProvider.GetRequiredService<IAutomationPackageService>();
                
                // Set tenant context for this job execution
                concreteTenantContext.SetTenant(tenantId, tenantSlug);
                _logger.LogInformation("Set tenant context for scheduled job: Tenant {TenantId} ({TenantSlug})", tenantId, tenantSlug);

                // Get the schedule details
                var schedule = await scheduleService.GetScheduleByIdAsync(scheduleId);
                if (schedule == null)
                {
                    _logger.LogWarning("Schedule {ScheduleId} not found, skipping execution", scheduleId);
                    return;
                }

                if (!schedule.IsEnabled)
                {
                    _logger.LogInformation("Schedule {ScheduleId} is disabled, skipping execution", scheduleId);
                    return;
                }

                // Get package details for the execution
                var package = await packageService.GetPackageByIdAsync(schedule.AutomationPackageId);
                if (package == null)
                {
                    _logger.LogWarning("Package {PackageId} not found for schedule {ScheduleId}, skipping execution", 
                        schedule.AutomationPackageId, scheduleId);
                    return;
                }

                // Get the latest version or use a default
                var version = package.Versions?.FirstOrDefault()?.VersionNumber ?? "latest";

                // Trigger the scheduled execution using the new service
                // This will handle bot agent validation, execution creation, and SignalR communication
                var execution = await executionTriggerService.TriggerScheduledExecutionAsync(
                    scheduleId,
                    schedule.BotAgentId,
                    schedule.AutomationPackageId,
                    package.Name,
                    version);

                _logger.LogInformation("Successfully triggered scheduled execution {ExecutionId} for schedule {ScheduleId}", 
                    execution.Id, scheduleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing scheduled job for Schedule {ScheduleId}", scheduleId);
                
                // Create a JobExecutionException to let Quartz know the job failed
                // This can trigger retry mechanisms if configured
                throw new JobExecutionException(ex);
            }
        }
    }
} 