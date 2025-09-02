using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Enums;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Utilities;
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

        public async Task<IScheduler> GetSchedulerAsync()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            if (!scheduler.IsStarted)
            {
                await scheduler.Start();
            }
            return scheduler;
        }

        public async Task TriggerJobAsync(Guid scheduleId)
        {
            var scheduler = await GetSchedulerAsync();
            var jobKey = GetJobKey(scheduleId);

            if (!await scheduler.CheckExists(jobKey))
            {
                throw new InvalidOperationException($"Job for schedule {scheduleId} does not exist");
            }

            await scheduler.TriggerJob(jobKey);
            _logger.LogInformation("Manually triggered job for schedule {ScheduleId}", scheduleId);
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

            var newTrigger = await CreateTriggerAsync(triggerKey, schedule);
            if (newTrigger == null)
            {
                _logger.LogWarning("Unable to create updated trigger for schedule {ScheduleId}", schedule.Id);
                return;
            }

            // Check if trigger exists and use appropriate method
            if (await scheduler.GetTrigger(triggerKey) != null)
            {
                // Trigger exists, use RescheduleJob to replace it
                await scheduler.RescheduleJob(triggerKey, newTrigger);
                _logger.LogInformation("Rescheduled existing trigger for schedule {ScheduleId}", schedule.Id);
            }
            else
            {
                // Trigger doesn't exist, schedule the new trigger with the existing job
                await scheduler.ScheduleJob(newTrigger);
                _logger.LogInformation("Scheduled new trigger for existing job {ScheduleId}", schedule.Id);
            }

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

        public async Task<bool> IsJobPausedAsync(Guid scheduleId)
        {
            var scheduler = await GetSchedulerAsync();
            var jobKey = GetJobKey(scheduleId);
            var triggerKey = GetTriggerKey(scheduleId);

            if (!await scheduler.CheckExists(jobKey))
                return false;

            var triggerState = await scheduler.GetTriggerState(triggerKey);
            return triggerState == TriggerState.Paused;
        }

        public async Task<object?> GetJobStatusAsync(Guid scheduleId)
        {
            try
            {
                var scheduler = await GetSchedulerAsync();
                var jobKey = GetJobKey(scheduleId);
                var triggerKey = GetTriggerKey(scheduleId);

                // Check if job exists
                var jobExists = await scheduler.CheckExists(jobKey);
                if (!jobExists)
                {
                    return new { exists = false, message = "Job does not exist" };
                }

                // Get job detail
                var jobDetail = await scheduler.GetJobDetail(jobKey);
                var trigger = await scheduler.GetTrigger(triggerKey);

                // Get trigger state
                var triggerState = await scheduler.GetTriggerState(triggerKey);

                // Get next and previous fire times
                var nextFireTime = trigger?.GetNextFireTimeUtc();
                var previousFireTime = trigger?.GetPreviousFireTimeUtc();

                return new
                {
                    exists = true,
                    jobKey = jobKey.ToString(),
                    triggerKey = triggerKey.ToString(),
                    jobDescription = jobDetail?.Description,
                    triggerState = triggerState.ToString(),
                    nextFireTimeUtc = nextFireTime?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    previousFireTimeUtc = previousFireTime?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    jobData = jobDetail?.JobDataMap?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString()),
                    schedulerRunning = scheduler.IsStarted,
                    schedulerName = scheduler.SchedulerName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job status for schedule {ScheduleId}", scheduleId);
                return new { exists = false, error = ex.Message };
            }
        }

        private async Task<ITrigger?> CreateTriggerAsync(TriggerKey triggerKey, ScheduleResponseDto schedule)
        {
            // IMPORTANT: Always use UTC for Quartz.NET triggers to avoid timezone issues in containers
            // We'll convert the cron expression to UTC time before creating the trigger
            var utcTimeZone = TimeZoneInfo.Utc;
            var triggerBuilder = TriggerBuilder.Create()
                .WithIdentity(triggerKey)
                .WithDescription($"Trigger for schedule '{schedule.Name}' (UTC converted)");

            return schedule.RecurrenceType switch
            {
                RecurrenceType.Once => CreateOnceTrigger(triggerBuilder, schedule, utcTimeZone),
                RecurrenceType.Minutes => CreateMinutesTrigger(triggerBuilder, schedule),
                RecurrenceType.Hourly => CreateHourlyTrigger(triggerBuilder, schedule, utcTimeZone),
                RecurrenceType.Daily => CreateDailyTrigger(triggerBuilder, schedule, utcTimeZone),
                RecurrenceType.Weekly => CreateWeeklyTrigger(triggerBuilder, schedule, utcTimeZone),
                RecurrenceType.Advanced => CreateAdvancedTrigger(triggerBuilder, schedule, utcTimeZone),
                _ => LogUnsupportedRecurrenceType(schedule.RecurrenceType)
            };
        }

        private ITrigger? CreateOnceTrigger(TriggerBuilder triggerBuilder, ScheduleResponseDto schedule, TimeZoneInfo timeZone)
        {
            if (!schedule.OneTimeExecution.HasValue)
            {
                _logger.LogError("OneTimeExecution is required for Once recurrence type");
                return null;
            }

            var oneTime = schedule.OneTimeExecution.Value;
            DateTime utcTime;

            // Handle different DateTimeKind values
            // OneTimeExecution is always stored as UTC in the database
            if (oneTime.Kind == DateTimeKind.Unspecified)
            {
                // Database values are stored as UTC but have Unspecified kind
                utcTime = DateTime.SpecifyKind(oneTime, DateTimeKind.Utc);
                _logger.LogDebug("Treating unspecified time {Time} as UTC {UtcTime}",
                    oneTime, utcTime);
            }
            else if (oneTime.Kind == DateTimeKind.Utc)
            {
                // Already UTC
                utcTime = oneTime;
                _logger.LogDebug("Using UTC time {UtcTime} directly", utcTime);
            }
            else
            {
                // DateTimeKind.Local - convert from system timezone to UTC
                // NOTE: This should not happen in normal operation since we store UTC times
                // This is a fallback that depends on server timezone
                utcTime = oneTime.ToUniversalTime();
                _logger.LogDebug("Converted local time {LocalTime} to UTC {UtcTime}", oneTime, utcTime);
            }

            if (utcTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("One-time execution date {UtcTime} is in the past for schedule {ScheduleId}", 
                    utcTime, schedule.Id);
                return null;
            }

            _logger.LogInformation("Creating once trigger for schedule {ScheduleId} to execute at {UtcTime}", 
                schedule.Id, utcTime);

            return triggerBuilder
                .StartAt(utcTime)
                .WithSimpleSchedule(x => x.WithRepeatCount(0))
                .Build();
        }

        private ITrigger CreateMinutesTrigger(TriggerBuilder triggerBuilder, ScheduleResponseDto schedule)
        {
            // Extract interval from cron expression or default to 30 minutes
            int intervalMinutes = 30; // Default
            
            if (!string.IsNullOrWhiteSpace(schedule.CronExpression))
            {
                // Parse cron expression like "0 */5 * * * *" to get the interval
                if (TryParseMinuteInterval(schedule.CronExpression, out var parsedInterval))
                {
                    intervalMinutes = parsedInterval;
                }
            }
            
            _logger.LogInformation("Creating minutes trigger with {IntervalMinutes} minute interval for schedule {ScheduleId}", 
                intervalMinutes, schedule.Id);

            return triggerBuilder
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(intervalMinutes)
                    .RepeatForever())
                .Build();
        }

        private ITrigger CreateHourlyTrigger(TriggerBuilder triggerBuilder, ScheduleResponseDto schedule, TimeZoneInfo timeZone)
        {
            // Use cron expression if available for more precise timing
            var cronTrigger = TryCreateCronTrigger(triggerBuilder, schedule.CronExpression, timeZone, "hourly", schedule.Id);
            if (cronTrigger != null)
                return cronTrigger;

            // Fallback to simple hourly schedule
            return triggerBuilder
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(1)
                    .RepeatForever())
                .Build();
        }

        private ITrigger CreateDailyTrigger(TriggerBuilder triggerBuilder, ScheduleResponseDto schedule, TimeZoneInfo timeZone)
        {
            // Use cron expression to respect the specific time set in the schedule
            if (!string.IsNullOrWhiteSpace(schedule.CronExpression))
            {
                var fixedCronExpression = FixDailyCronExpression(schedule.CronExpression);

                // Convert cron expression from local timezone to UTC
                var utcCronExpression = ConvertCronExpressionToUtc(fixedCronExpression, schedule.TimeZoneId);

                _logger.LogInformation("Creating daily trigger for schedule {ScheduleId}: Original cron '{OriginalCron}', Fixed cron '{FixedCron}', UTC cron '{UtcCron}', Original timezone '{OriginalTimeZone}'",
                    schedule.Id, schedule.CronExpression, fixedCronExpression, utcCronExpression, schedule.TimeZoneId);

                var cronTrigger = TryCreateCronTrigger(triggerBuilder, utcCronExpression, TimeZoneInfo.Utc, "daily", schedule.Id);
                if (cronTrigger != null)
                {
                    return cronTrigger;
                }
            }

            // Fallback to simple daily schedule if no cron expression
            _logger.LogWarning("Using fallback simple schedule for daily schedule {ScheduleId}", schedule.Id);
            return triggerBuilder
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(24)
                    .RepeatForever())
                .Build();
        }

        private ITrigger CreateWeeklyTrigger(TriggerBuilder triggerBuilder, ScheduleResponseDto schedule, TimeZoneInfo timeZone)
        {
            // Use cron expression to respect the specific days and time set in the schedule
            if (!string.IsNullOrWhiteSpace(schedule.CronExpression))
            {
                _logger.LogInformation("Starting weekly trigger creation for schedule {ScheduleId}: Original cron '{OriginalCron}', Original timezone '{OriginalTimeZone}'",
                    schedule.Id, schedule.CronExpression, schedule.TimeZoneId);

                // Convert cron expression from local timezone to UTC
                var utcCronExpression = ConvertCronExpressionToUtc(schedule.CronExpression, schedule.TimeZoneId);

                _logger.LogInformation("Converted weekly trigger for schedule {ScheduleId}: Original cron '{OriginalCron}', UTC cron '{UtcCron}', Original timezone '{OriginalTimeZone}'",
                    schedule.Id, schedule.CronExpression, utcCronExpression, schedule.TimeZoneId);

                var cronTrigger = TryCreateCronTrigger(triggerBuilder, utcCronExpression, TimeZoneInfo.Utc, "weekly", schedule.Id);
                if (cronTrigger != null)
                {
                    _logger.LogInformation("Successfully created weekly cron trigger for schedule {ScheduleId}", schedule.Id);
                    return cronTrigger;
                }
                else
                {
                    _logger.LogError("Failed to create weekly cron trigger for schedule {ScheduleId} with UTC cron '{UtcCron}'", schedule.Id, utcCronExpression);
                }
            }
            else
            {
                _logger.LogWarning("No cron expression provided for weekly schedule {ScheduleId}", schedule.Id);
            }

            // Fallback to simple weekly schedule if no cron expression or cron trigger creation failed
            _logger.LogWarning("Using fallback simple schedule for weekly schedule {ScheduleId} - THIS WILL CAUSE IMMEDIATE EXECUTION", schedule.Id);
            return triggerBuilder
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(24 * 7)
                    .RepeatForever())
                .Build();
        }



        private ITrigger? CreateAdvancedTrigger(TriggerBuilder triggerBuilder, ScheduleResponseDto schedule, TimeZoneInfo timeZone)
        {
            if (string.IsNullOrWhiteSpace(schedule.CronExpression))
            {
                _logger.LogError("CronExpression is required for Advanced recurrence type");
                return null;
            }

            // Convert cron expression from local timezone to UTC
            var utcCronExpression = ConvertCronExpressionToUtc(schedule.CronExpression, schedule.TimeZoneId);

            _logger.LogInformation("Creating advanced trigger for schedule {ScheduleId}: Original cron '{OriginalCron}', UTC cron '{UtcCron}', Original timezone '{OriginalTimeZone}'",
                schedule.Id, schedule.CronExpression, utcCronExpression, schedule.TimeZoneId);

            var cronTrigger = TryCreateCronTrigger(triggerBuilder, utcCronExpression, TimeZoneInfo.Utc, "advanced", schedule.Id);
            return cronTrigger;
        }

        private ITrigger? TryCreateCronTrigger(TriggerBuilder triggerBuilder, string cronExpression, TimeZoneInfo timeZone, string scheduleType, Guid scheduleId)
        {
            try
            {
                // Create a temporary trigger to calculate the next fire time
                var tempTrigger = TriggerBuilder.Create()
                    .WithCronSchedule(cronExpression, x => x.InTimeZone(timeZone))
                    .Build();

                var nextFireTime = tempTrigger.GetNextFireTimeUtc();
                var now = DateTimeOffset.UtcNow;

                // If the next fire time is too soon (within 5 minutes), use StartAt to prevent immediate execution
                DateTimeOffset? startAt = null;
                if (nextFireTime.HasValue && nextFireTime.Value.Subtract(now).TotalMinutes < 5)
                {
                    // For schedules created close to their fire time, start from the next occurrence after that
                    startAt = nextFireTime.Value.AddMinutes(1); // Start 1 minute after the calculated time

                    _logger.LogWarning("Next fire time for {ScheduleType} schedule {ScheduleId} is too soon ({NextFireTime}). " +
                        "Current time: {CurrentTime}. Using StartAt: {StartAt} to prevent immediate execution.",
                        scheduleType, scheduleId, nextFireTime.Value, now, startAt);
                }

                // Build the actual trigger with StartAt if needed
                var triggerBuilderWithSchedule = triggerBuilder
                    .WithCronSchedule(cronExpression, x => x.InTimeZone(timeZone));

                if (startAt.HasValue)
                {
                    triggerBuilderWithSchedule = triggerBuilderWithSchedule.StartAt(startAt.Value);
                }

                var trigger = triggerBuilderWithSchedule.Build();

                _logger.LogInformation("Created cron trigger for {ScheduleType} schedule {ScheduleId}: Cron '{CronExpression}', Timezone '{TimeZone}', Next fire time '{NextFireTime}', Current time '{CurrentTime}', StartAt '{StartAt}'",
                    scheduleType, scheduleId, cronExpression, timeZone.Id, trigger.GetNextFireTimeUtc(), now, startAt);

                return trigger;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid cron expression '{CronExpression}' for {ScheduleType} schedule {ScheduleId}",
                    cronExpression, scheduleType, scheduleId);
                return null;
            }
        }

        private ITrigger? LogUnsupportedRecurrenceType(RecurrenceType recurrenceType)
        {
            _logger.LogError("Unsupported recurrence type: {RecurrenceType}", recurrenceType);
            return null;
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
                return DateTimeUtility.GetTimeZoneInfo(timeZoneId);
            }
            catch
            {
                // Fallback to UTC if timezone is invalid
                return DateTimeUtility.GetTimeZoneInfo(null);
            }
        }

        /// <summary>
        /// Converts a cron expression from a local timezone to UTC
        /// </summary>
        private string ConvertCronExpressionToUtc(string cronExpression, string originalTimeZoneId)
        {
            try
            {
                // Parse the cron expression to extract hour and minute
                var parts = cronExpression.Split(' ');
                if (parts.Length < 6)
                {
                    _logger.LogWarning("Invalid cron expression format: {CronExpression}", cronExpression);
                    return cronExpression;
                }

                // Extract minute and hour (positions 1 and 2 in Quartz cron format)
                if (!int.TryParse(parts[1], out var minute) || !int.TryParse(parts[2], out var hour))
                {
                    _logger.LogWarning("Could not parse hour/minute from cron expression: {CronExpression}", cronExpression);
                    return cronExpression;
                }

                // Get the original timezone
                var originalTimeZone = GetTimeZoneInfo(originalTimeZoneId);

                // Create a sample date in the original timezone (today at the specified time)
                var today = DateTime.Today;
                var localDateTime = new DateTime(today.Year, today.Month, today.Day, hour, minute, 0, DateTimeKind.Unspecified);

                // Convert to UTC
                var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, originalTimeZone);

                // Rebuild the cron expression with UTC time
                // Fix Quartz.NET issue: when day-of-week is specified, day-of-month must be '?' not '*'
                var dayOfMonth = parts[3];
                var dayOfWeek = parts[5];

                // If day-of-week is not '*' (i.e., specific days are specified), set day-of-month to '?'
                if (dayOfWeek != "*")
                {
                    dayOfMonth = "?";
                }

                var utcCronExpression = $"{parts[0]} {utcDateTime.Minute} {utcDateTime.Hour} {dayOfMonth} {parts[4]} {dayOfWeek}";

                _logger.LogInformation("Converted cron expression from {OriginalCron} ({OriginalTimeZone}) to {UtcCron} (UTC)",
                    cronExpression, originalTimeZoneId, utcCronExpression);

                return utcCronExpression;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting cron expression to UTC: {CronExpression}", cronExpression);
                return cronExpression; // Return original if conversion fails
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

        /// <summary>
        /// Tries to parse minute interval from cron expressions like "0 */5 * * * *"
        /// </summary>
        private static bool TryParseMinuteInterval(string cronExpression, out int interval)
        {
            interval = 30; // Default fallback
            
            try
            {
                var parts = cronExpression.Split(' ');
                if (parts.Length == 6)
                {
                    // Look for minute part like "*/5" 
                    var minutePart = parts[1];
                    if (minutePart.StartsWith("*/"))
                    {
                        var intervalStr = minutePart.Substring(2);
                        if (int.TryParse(intervalStr, out var parsedInterval) && parsedInterval > 0 && parsedInterval <= 60)
                        {
                            interval = parsedInterval;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Fall back to default
            }
            
            return false;
        }
    }
} 