using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Enums;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.Jobs;
using Quartz;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service for managing Quartz.NET jobs for schedules
    /// </summary>
    public class QuartzScheduleManager : IQuartzScheduleManager
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<QuartzScheduleManager> _logger;

        public QuartzScheduleManager(
            ISchedulerFactory schedulerFactory,
            ITenantContext tenantContext,
            ILogger<QuartzScheduleManager> logger)
        {
            _schedulerFactory = schedulerFactory;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        private async Task<IScheduler> GetSchedulerAsync()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            if (!scheduler.IsStarted)
            {
                await scheduler.Start();
            }
            return scheduler;
        }

        public async Task CreateJobAsync(ScheduleResponseDto schedule)
        {
            if (!schedule.IsEnabled)
            {
                _logger.LogInformation("Schedule {ScheduleId} is disabled, skipping job creation", schedule.Id);
                return;
            }

            var scheduler = await GetSchedulerAsync();
            var jobKey = GetJobKey(schedule.Id);
            var triggerKey = GetTriggerKey(schedule.Id);

            // Create job detail
            var jobDetail = JobBuilder.Create<TriggerExecutionJob>()
                .WithIdentity(jobKey)
                .WithDescription($"Scheduled execution job for schedule '{schedule.Name}'")
                .UsingJobData("ScheduleId", schedule.Id.ToString())
                .UsingJobData("TenantId", schedule.OrganizationUnitId.ToString())
                .UsingJobData("TenantSlug", _tenantContext.CurrentTenantSlug ?? "")
                .StoreDurably(false) // Delete job when no triggers reference it
                .Build();

            // Create trigger based on recurrence type
            var trigger = await CreateTriggerAsync(triggerKey, schedule);
            if (trigger == null)
            {
                _logger.LogWarning("Unable to create trigger for schedule {ScheduleId} with recurrence type {RecurrenceType}", 
                    schedule.Id, schedule.RecurrenceType);
                return;
            }

            // Schedule the job
            await scheduler.ScheduleJob(jobDetail, trigger);

            _logger.LogInformation("Created Quartz job for schedule {ScheduleId} with recurrence type {RecurrenceType}", 
                schedule.Id, schedule.RecurrenceType);
        }

        public async Task UpdateJobAsync(ScheduleResponseDto schedule)
        {
            var scheduler = await GetSchedulerAsync();
            var jobKey = GetJobKey(schedule.Id);
            var triggerKey = GetTriggerKey(schedule.Id);

            // Check if job exists
            if (!await scheduler.CheckExists(jobKey))
            {
                _logger.LogInformation("Job does not exist for schedule {ScheduleId}, creating new job", schedule.Id);
                await CreateJobAsync(schedule);
                return;
            }

            if (!schedule.IsEnabled)
            {
                // If schedule is disabled, pause the job
                await PauseJobAsync(schedule.Id);
                return;
            }

            // Delete existing trigger and create new one
            await scheduler.UnscheduleJob(triggerKey);

            var newTrigger = await CreateTriggerAsync(triggerKey, schedule);
            if (newTrigger == null)
            {
                _logger.LogWarning("Unable to create updated trigger for schedule {ScheduleId}", schedule.Id);
                return;
            }

            // Reschedule with new trigger
            await scheduler.RescheduleJob(triggerKey, newTrigger);

            // Ensure job is resumed if it was paused
            await scheduler.ResumeJob(jobKey);

            _logger.LogInformation("Updated Quartz job for schedule {ScheduleId}", schedule.Id);
        }

        public async Task DeleteJobAsync(Guid scheduleId)
        {
            var scheduler = await GetSchedulerAsync();
            var jobKey = GetJobKey(scheduleId);
            
            if (await scheduler.CheckExists(jobKey))
            {
                await scheduler.DeleteJob(jobKey);
                _logger.LogInformation("Deleted Quartz job for schedule {ScheduleId}", scheduleId);
            }
        }

        public async Task PauseJobAsync(Guid scheduleId)
        {
            var scheduler = await GetSchedulerAsync();
            var jobKey = GetJobKey(scheduleId);
            
            if (await scheduler.CheckExists(jobKey))
            {
                await scheduler.PauseJob(jobKey);
                _logger.LogInformation("Paused Quartz job for schedule {ScheduleId}", scheduleId);
            }
        }

        public async Task ResumeJobAsync(Guid scheduleId)
        {
            var scheduler = await GetSchedulerAsync();
            var jobKey = GetJobKey(scheduleId);
            
            if (await scheduler.CheckExists(jobKey))
            {
                await scheduler.ResumeJob(jobKey);
                _logger.LogInformation("Resumed Quartz job for schedule {ScheduleId}", scheduleId);
            }
        }

        public async Task<bool> JobExistsAsync(Guid scheduleId)
        {
            var scheduler = await GetSchedulerAsync();
            var jobKey = GetJobKey(scheduleId);
            return await scheduler.CheckExists(jobKey);
        }

        private async Task<ITrigger?> CreateTriggerAsync(TriggerKey triggerKey, ScheduleResponseDto schedule)
        {
            var timeZone = GetTimeZoneInfo(schedule.TimeZoneId);
            var triggerBuilder = TriggerBuilder.Create()
                .WithIdentity(triggerKey)
                .WithDescription($"Trigger for schedule '{schedule.Name}'");

            switch (schedule.RecurrenceType)
            {
                case RecurrenceType.Once:
                    if (!schedule.OneTimeExecution.HasValue)
                    {
                        _logger.LogError("OneTimeExecution is required for Once recurrence type");
                        return null;
                    }

                    var startTime = TimeZoneInfo.ConvertTimeToUtc(schedule.OneTimeExecution.Value, timeZone);
                    if (startTime <= DateTimeOffset.UtcNow)
                    {
                        _logger.LogWarning("One-time execution date is in the past for schedule {ScheduleId}", schedule.Id);
                        return null;
                    }

                    return triggerBuilder
                        .StartAt(startTime)
                        .WithSimpleSchedule(x => x.WithRepeatCount(0))
                        .Build();

                case RecurrenceType.Minutes:
                    // Default to 30 minutes for now - this could be configurable in the future
                    return triggerBuilder
                        .StartNow()
                        .WithSimpleSchedule(x => x
                            .WithIntervalInMinutes(30)
                            .RepeatForever())
                        .Build();

                case RecurrenceType.Hourly:
                    // Use cron expression if available for more precise timing
                    if (!string.IsNullOrWhiteSpace(schedule.CronExpression))
                    {
                        try
                        {
                            return triggerBuilder
                                .StartNow()
                                .WithCronSchedule(schedule.CronExpression, x => x.InTimeZone(timeZone))
                                .Build();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Invalid cron expression '{CronExpression}' for hourly schedule {ScheduleId}", 
                                schedule.CronExpression, schedule.Id);
                        }
                    }
                    
                    // Fallback to simple hourly schedule
                    return triggerBuilder
                        .StartNow()
                        .WithSimpleSchedule(x => x
                            .WithIntervalInHours(1)
                            .RepeatForever())
                        .Build();

                case RecurrenceType.Daily:
                    // Use cron expression to respect the specific time set in the schedule
                    if (!string.IsNullOrWhiteSpace(schedule.CronExpression))
                    {
                        try
                        {
                            // Fix cron expression format for daily schedules
                            var fixedCronExpression = FixDailyCronExpression(schedule.CronExpression);
                            
                            return triggerBuilder
                                .StartNow()
                                .WithCronSchedule(fixedCronExpression, x => x.InTimeZone(timeZone))
                                .Build();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Invalid cron expression '{CronExpression}' for daily schedule {ScheduleId}", 
                                schedule.CronExpression, schedule.Id);
                        }
                    }
                    
                    // Fallback to simple daily schedule if no cron expression
                    return triggerBuilder
                        .StartNow()
                        .WithSimpleSchedule(x => x
                            .WithIntervalInHours(24)
                            .RepeatForever())
                        .Build();

                case RecurrenceType.Weekly:
                    return triggerBuilder
                        .StartNow()
                        .WithSimpleSchedule(x => x
                            .WithIntervalInHours(24 * 7)
                            .RepeatForever())
                        .Build();

                case RecurrenceType.Monthly:
                    // Use cron for monthly since SimpleSchedule doesn't handle months well
                    return triggerBuilder
                        .StartNow()
                        .WithCronSchedule("0 0 0 1 * ?", x => x.InTimeZone(timeZone)) // First day of every month
                        .Build();

                case RecurrenceType.Advanced:
                    if (string.IsNullOrWhiteSpace(schedule.CronExpression))
                    {
                        _logger.LogError("CronExpression is required for Advanced recurrence type");
                        return null;
                    }

                    try
                    {
                        return triggerBuilder
                            .StartNow()
                            .WithCronSchedule(schedule.CronExpression, x => x.InTimeZone(timeZone))
                            .Build();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Invalid cron expression '{CronExpression}' for schedule {ScheduleId}", 
                            schedule.CronExpression, schedule.Id);
                        return null;
                    }

                default:
                    _logger.LogError("Unsupported recurrence type: {RecurrenceType}", schedule.RecurrenceType);
                    return null;
            }
        }

        private static JobKey GetJobKey(Guid scheduleId)
        {
            return new JobKey($"schedule-{scheduleId}", "schedules");
        }

        private static TriggerKey GetTriggerKey(Guid scheduleId)
        {
            return new TriggerKey($"schedule-trigger-{scheduleId}", "schedules");
        }

        private static TimeZoneInfo GetTimeZoneInfo(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
                // Fallback to UTC if timezone is invalid
                return TimeZoneInfo.Utc;
            }
        }

        /// <summary>
        /// Fixes cron expression format for daily schedules to be compatible with Quartz.NET
        /// Converts "0 MM HH * * *" to "0 MM HH ? * *" (day-of-month must be ? when day-of-week is *)
        /// </summary>
        private static string FixDailyCronExpression(string cronExpression)
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
                return cronExpression;

            var parts = cronExpression.Split(' ');
            if (parts.Length == 6)
            {
                // Format: second minute hour day-of-month month day-of-week
                // For daily schedules, we need day-of-month to be ? when day-of-week is *
                if (parts[3] == "*" && parts[5] == "*")
                {
                    parts[3] = "?"; // Change day-of-month from * to ?
                }
            }
            
            return string.Join(" ", parts);
        }
    }
} 