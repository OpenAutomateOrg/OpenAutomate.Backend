using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.Enums;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.Dto.Common;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Utilities;
using OpenAutomate.Infrastructure.DbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service for managing schedules
    /// </summary>
    public class ScheduleService : IScheduleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBotAgentService _botAgentService;
        private readonly IAutomationPackageService _packageService;
        private readonly IQuartzScheduleManager _quartzManager;
        private readonly ITenantContext _tenantContext;

        private readonly ILogger<ScheduleService> _logger;

        public ScheduleService(
            ApplicationDbContext context,
            IBotAgentService botAgentService,
            IAutomationPackageService packageService,
            IQuartzScheduleManager quartzManager,
            ITenantContext tenantContext,

            ILogger<ScheduleService> logger)
        {
            _context = context;
            _botAgentService = botAgentService;
            _packageService = packageService;
            _quartzManager = quartzManager;
            _tenantContext = tenantContext;

            _logger = logger;
        }

        public async Task<ScheduleResponseDto> CreateScheduleAsync(CreateScheduleDto dto)
        {
            // Validate that the bot agent exists and belongs to the current tenant
            var botAgent = await _botAgentService.GetBotAgentByIdAsync(dto.BotAgentId);
            if (botAgent == null)
            {
                throw new ArgumentException("Bot agent not found or does not belong to the current tenant");
            }

            // Validate that the automation package exists and belongs to the current tenant
            var package = await _packageService.GetPackageByIdAsync(dto.AutomationPackageId);
            if (package == null)
            {
                throw new ArgumentException("Automation package not found or does not belong to the current tenant");
            }https://github.com/OpenAutomateOrg/OpenAutomate.Backend/pull/258

            // Validate schedule configuration
            ValidateScheduleConfiguration(dto.RecurrenceType, dto.CronExpression, dto.OneTimeExecution);

            // Note: Bot agents can have multiple schedules (many-to-one relationship)
            // No validation needed here - each schedule just needs a valid bot agent

            var schedule = new Schedule
            {
                Name = dto.Name,
                Description = dto.Description,
                IsEnabled = dto.IsEnabled,
                RecurrenceType = dto.RecurrenceType,
                CronExpression = dto.CronExpression,
                OneTimeExecution = dto.OneTimeExecution.HasValue ? DateTimeUtility.EnsureUtc(dto.OneTimeExecution.Value, DateTimeUtility.GetTimeZoneInfo(dto.TimeZoneId)) : null,
                TimeZoneId = dto.TimeZoneId,
                AutomationPackageId = dto.AutomationPackageId,
                BotAgentId = dto.BotAgentId
            };

            await _context.Schedules.AddAsync(schedule);
            await _context.SaveChangesAsync();

            var responseDto = await MapToResponseDto(schedule);

            // Create Quartz job for the schedule
            try
            {
                await _quartzManager.CreateJobAsync(responseDto);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the schedule creation
                // The job can be created later when the schedule is updated
                // Consider adding a retry mechanism or background job for this
                using var scope = _context.Database.BeginTransaction();
                try
                {
                    // The schedule was created successfully, but job creation failed
                    // We could mark it as having a job creation error for later retry
                    throw new InvalidOperationException($"Schedule created but Quartz job creation failed: {ex.Message}", ex);
                }
                catch
                {
                    scope.Rollback();
                    throw;
                }
            }

            return responseDto;
        }

        public async Task<ScheduleResponseDto?> GetScheduleByIdAsync(Guid id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.AutomationPackage)
                .Include(s => s.BotAgent)
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();

            return schedule != null ? await MapToResponseDto(schedule) : null;
        }

        public async Task<IEnumerable<ScheduleResponseDto>> GetAllSchedulesAsync()
        {
            var schedules = await _context.Schedules
                .Include(s => s.AutomationPackage)
                .Include(s => s.BotAgent)
                .ToListAsync();

            var responseDtos = new List<ScheduleResponseDto>();
            foreach (var schedule in schedules)
            {
                responseDtos.Add(await MapToResponseDto(schedule));
            }

            return responseDtos;
        }

        public async Task<ScheduleResponseDto?> UpdateScheduleAsync(Guid id, UpdateScheduleDto dto)
        {
            var schedule = await _context.Schedules
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();

            if (schedule == null)
                return null;

            // Validate that the bot agent exists and belongs to the current tenant
            var botAgent = await _botAgentService.GetBotAgentByIdAsync(dto.BotAgentId);
            if (botAgent == null)
            {
                throw new ArgumentException("Bot agent not found or does not belong to the current tenant");
            }

            // Validate that the automation package exists and belongs to the current tenant
            var package = await _packageService.GetPackageByIdAsync(dto.AutomationPackageId);
            if (package == null)
            {
                throw new ArgumentException("Automation package not found or does not belong to the current tenant");
            }

            // Note: Bot agents can have multiple schedules (many-to-one relationship)
            // No constraint validation needed when changing bot agent

            // Validate schedule configuration
            ValidateScheduleConfiguration(dto.RecurrenceType, dto.CronExpression, dto.OneTimeExecution);

            // Update the schedule properties
            schedule.Name = dto.Name;
            schedule.Description = dto.Description;
            schedule.IsEnabled = dto.IsEnabled;
            schedule.RecurrenceType = dto.RecurrenceType;
            schedule.CronExpression = dto.CronExpression;
            schedule.OneTimeExecution = dto.OneTimeExecution;
            schedule.TimeZoneId = dto.TimeZoneId;
            schedule.AutomationPackageId = dto.AutomationPackageId;
            schedule.BotAgentId = dto.BotAgentId;

            _context.Schedules.Update(schedule);
            await _context.SaveChangesAsync();

            var responseDto = await MapToResponseDto(schedule);

            // Update Quartz job
            try
            {
                await _quartzManager.UpdateJobAsync(responseDto);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the schedule update
                // TODO: Consider adding error logging or retry mechanism
                throw new InvalidOperationException($"Schedule updated but Quartz job update failed: {ex.Message}", ex);
            }

            return responseDto;
        }

        public async Task<bool> DeleteScheduleAsync(Guid id)
        {
            var schedule = await _context.Schedules
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();

            if (schedule == null)
                return false;

            // Delete Quartz job first
            try
            {
                await _quartzManager.DeleteJobAsync(id);
            }
            catch (Exception)
            {
                // Log the error but continue with schedule deletion
                // The job might not exist or there might be a temporary issue
                // TODO: Consider adding error logging
            }

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<ScheduleResponseDto?> EnableScheduleAsync(Guid id)
        {
            var schedule = await _context.Schedules
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();

            if (schedule == null)
                return null;

            schedule.IsEnabled = true;
            _context.Schedules.Update(schedule);
            await _context.SaveChangesAsync();

            var responseDto = await MapToResponseDto(schedule);

            // Resume or create Quartz job
            try
            {
                if (await _quartzManager.JobExistsAsync(id))
                {
                    await _quartzManager.ResumeJobAsync(id);
                }
                else
                {
                    await _quartzManager.CreateJobAsync(responseDto);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the enable operation
                throw new InvalidOperationException($"Schedule enabled but Quartz job operation failed: {ex.Message}", ex);
            }

            return responseDto;
        }

        public async Task<ScheduleResponseDto?> DisableScheduleAsync(Guid id)
        {
            var schedule = await _context.Schedules
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();

            if (schedule == null)
                return null;

            schedule.IsEnabled = false;
            _context.Schedules.Update(schedule);
            await _context.SaveChangesAsync();

            var responseDto = await MapToResponseDto(schedule);

            // Pause Quartz job
            try
            {
                await _quartzManager.PauseJobAsync(id);
            }
            catch (Exception)
            {
                // Log the error but don't fail the disable operation
                // The job might not exist, which is fine for a disabled schedule
                // TODO: Consider adding error logging
            }

            return responseDto;
        }

        public List<DateTime> CalculateUpcomingRunTimes(ScheduleResponseDto schedule, int count = 5)
        {
            if (!schedule.IsEnabled)
                return new List<DateTime>();

            var timeZone = DateTimeUtility.GetTimeZoneInfo(schedule.TimeZoneId);
            var upcomingTimes = TryCalculateMultipleCronTimes(schedule, timeZone, count);

            // If we couldn't calculate multiple times, fall back to single next time
            if (upcomingTimes.Count == 0)
            {
                var nextTime = CalculateNextRunTime(schedule);
                if (nextTime.HasValue)
                {
                    upcomingTimes.Add(nextTime.Value);
                }
            }

            return upcomingTimes;
        }

        private static List<DateTime> TryCalculateMultipleCronTimes(ScheduleResponseDto schedule, TimeZoneInfo timeZone, int count)
        {
            var upcomingTimes = new List<DateTime>();

            if (string.IsNullOrWhiteSpace(schedule.CronExpression))
                return upcomingTimes;

            try
            {
                var dailyTimes = TryCalculateDailyCronTimes(schedule.CronExpression, timeZone, count);
                upcomingTimes.AddRange(dailyTimes);
            }
            catch
            {
                // Fall back to single calculation
            }

            return upcomingTimes;
        }

        private static List<DateTime> TryCalculateDailyCronTimes(string cronExpression, TimeZoneInfo timeZone, int count)
        {
            var upcomingTimes = new List<DateTime>();
            var parts = cronExpression.Split(' ');

            if (!IsDailyCronPattern(parts))
                return upcomingTimes;

            if (TryParseCronTimeParts(parts, out var hour, out var minute, out var second))
            {
                var now = DateTimeUtility.UtcNow;
                var localNow = DateTimeUtility.ConvertFromUtc(now, timeZone);
                var baseTime = localNow.Date.AddHours(hour).AddMinutes(minute).AddSeconds(second);
                var nextTime = baseTime > localNow ? baseTime : baseTime.AddDays(1);

                for (int i = 0; i < count; i++)
                {
                    upcomingTimes.Add(DateTimeUtility.EnsureUtc(nextTime.AddDays(i), timeZone));
                }
            }

            return upcomingTimes;
        }

        private static bool IsDailyCronPattern(string[] parts)
        {
            return parts.Length == 6 && parts[3] == "*" && parts[4] == "*" && parts[5] == "*";
        }

        private static bool TryParseCronTimeParts(string[] parts, out int hour, out int minute, out int second)
        {
            hour = minute = second = 0;
            return int.TryParse(parts[2], out hour) &&
                   int.TryParse(parts[1], out minute) &&
                   int.TryParse(parts[0], out second);
        }

        public DateTime? CalculateNextRunTime(ScheduleResponseDto schedule)
        {
            if (!schedule.IsEnabled)
                return null;

            var now = DateTimeUtility.UtcNow;
            var timeZone = DateTimeUtility.GetTimeZoneInfo(schedule.TimeZoneId);

            return schedule.RecurrenceType switch
            {
                RecurrenceType.Once => CalculateOnceNextRunTime(schedule, timeZone, now),
                RecurrenceType.Minutes => CalculateMinutesNextRunTime(schedule, now),
                RecurrenceType.Hourly => CalculateHourlyNextRunTime(schedule, timeZone, now),
                RecurrenceType.Daily => CalculateDailyNextRunTime(schedule, timeZone, now),
                RecurrenceType.Weekly => CalculateWeeklyNextRunTime(schedule, timeZone, now),
                RecurrenceType.Monthly => CalculateMonthlyNextRunTime(schedule, timeZone, now),
                RecurrenceType.Advanced => CalculateAdvancedNextRunTime(schedule, timeZone),
                _ => null
            };
        }

        private static DateTime? CalculateMinutesNextRunTime(ScheduleResponseDto schedule, DateTime now)
        {
            // Extract interval from cron expression or default to 30 minutes
            int intervalMinutes = 30; // Default
            
            if (!string.IsNullOrWhiteSpace(schedule.CronExpression))
            {
                if (TryParseMinuteInterval(schedule.CronExpression, out var parsedInterval))
                {
                    intervalMinutes = parsedInterval;
                }
            }

            return now.AddMinutes(intervalMinutes);
        }

        private static DateTime? CalculateOnceNextRunTime(ScheduleResponseDto schedule, TimeZoneInfo timeZone, DateTime now)
        {
            if (!schedule.OneTimeExecution.HasValue)
                return null;

            var oneTimeValue = schedule.OneTimeExecution.Value;
            
            // If the datetime has a timezone specified (Kind.Utc or Kind.Local), use it as-is
            // If it's unspecified, treat it as local time in the schedule's timezone
            DateTime targetTime;
            
            if (oneTimeValue.Kind == DateTimeKind.Unspecified)
            {
                // Treat as local time in the schedule's timezone
                targetTime = DateTimeUtility.EnsureUtc(oneTimeValue, timeZone);
            }
            else if (oneTimeValue.Kind == DateTimeKind.Utc)
            {
                // Already UTC, use as-is
                targetTime = oneTimeValue;
            }
            else
            {
                // Local time, convert to UTC assuming system timezone
                targetTime = oneTimeValue.ToUniversalTime();
            }

            // Only return if the time is in the future
            if (targetTime <= now)
                return null;

            return targetTime;
        }

        private static DateTime? CalculateHourlyNextRunTime(ScheduleResponseDto schedule, TimeZoneInfo timeZone, DateTime now)
        {
            var cronResult = TryCalculateFromCronExpression(schedule.CronExpression, timeZone);
            return cronResult ?? now.AddHours(1);
        }

        private static DateTime? CalculateDailyNextRunTime(ScheduleResponseDto schedule, TimeZoneInfo timeZone, DateTime now)
        {
            var cronResult = TryCalculateFromCronExpression(schedule.CronExpression, timeZone);
            if (cronResult.HasValue)
                return cronResult;

            // Fallback: Calculate next occurrence at the same time
            var localNow = DateTimeUtility.ConvertFromUtc(now, timeZone);
            
            // Default to 9:00 AM if no cron expression
            var targetHour = 9;
            var targetMinute = 0;
            
            if (schedule.CronExpression != null && TryParseDailyCronTime(schedule.CronExpression, out var hour, out var minute))
            {
                targetHour = hour;
                targetMinute = minute;
            }

            // Calculate today's scheduled time
            var todayScheduledTime = localNow.Date.AddHours(targetHour).AddMinutes(targetMinute);
            
            // Add a small buffer to handle timing precision issues
            // If the scheduled time is more than 10 seconds in the future, use today; otherwise use tomorrow
            var timeDifference = todayScheduledTime.Subtract(localNow).TotalSeconds;
            var nextRunTime = timeDifference > 10 ? todayScheduledTime : todayScheduledTime.AddDays(1);

            return DateTimeUtility.EnsureUtc(nextRunTime, timeZone);
        }

        private static DateTime? CalculateWeeklyNextRunTime(ScheduleResponseDto schedule, TimeZoneInfo timeZone, DateTime now)
        {
            var cronResult = TryCalculateFromCronExpression(schedule.CronExpression, timeZone);
            return cronResult ?? now.AddDays(7);
        }

        private static DateTime? CalculateMonthlyNextRunTime(ScheduleResponseDto schedule, TimeZoneInfo timeZone, DateTime now)
        {
            var cronResult = TryCalculateFromCronExpression(schedule.CronExpression, timeZone);
            return cronResult ?? now.AddMonths(1);
        }

        private static DateTime? CalculateAdvancedNextRunTime(ScheduleResponseDto schedule, TimeZoneInfo timeZone)
        {
            return TryCalculateFromCronExpression(schedule.CronExpression, timeZone);
        }

        private static DateTime? TryCalculateFromCronExpression(string? cronExpression, TimeZoneInfo timeZone)
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
                return null;

            try
            {
                return CalculateNextRunTimeFromCron(cronExpression, timeZone);
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? CalculateNextRunTimeFromCron(string cronExpression, TimeZoneInfo timeZone)
        {
            try
            {
                // Parse cron expression: "second minute hour day month dayOfWeek"
                // Example: "0 00 09 * * *" = every day at 09:00:00
                var parts = cronExpression.Split(' ');
                if (parts.Length != 6)
                    return null;

                var now = DateTimeUtility.UtcNow;
                var localNow = DateTimeUtility.ConvertFromUtc(now, timeZone);

                // For daily schedules, calculate next occurrence
                if (parts[3] == "*" && parts[4] == "*" && parts[5] == "*" &&
                    int.TryParse(parts[2], out var hour) && int.TryParse(parts[1], out var minute) && int.TryParse(parts[0], out var second))
                {
                    var todayAtTime = localNow.Date.AddHours(hour).AddMinutes(minute).AddSeconds(second);
                    
                    // For daily schedules, if the time has already passed today, schedule for tomorrow
                    // Add a small buffer (10 seconds) to handle timing precision issues
                    var timeDifference = todayAtTime.Subtract(localNow).TotalSeconds;
                    var nextRun = timeDifference >= 10 ? todayAtTime : todayAtTime.AddDays(1);
                    
                    // Convert the local time to UTC for storage
                    return DateTimeUtility.EnsureUtc(nextRun, timeZone);
                }

                // For other patterns, we'd need a full cron parser
                // For now, return null to indicate we can't calculate it
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryParseDailyCronTime(string cronExpression, out int hour, out int minute)
        {
            hour = 0;
            minute = 0;

            try
            {
                var parts = cronExpression.Split(' ');
                if (parts.Length == 6)
                {
                    return int.TryParse(parts[2], out hour) && int.TryParse(parts[1], out minute);
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return false;
        }

        private static void ValidateScheduleConfiguration(RecurrenceType recurrenceType, string? cronExpression, DateTime? oneTimeExecution)
        {
            switch (recurrenceType)
            {
                case RecurrenceType.Once:
                    if (!oneTimeExecution.HasValue)
                    {
                        throw new ArgumentException("OneTimeExecution must be specified for Once recurrence type");
                    }
                    if (oneTimeExecution.Value <= DateTimeUtility.UtcNow)
                    {
                        throw new ArgumentException("OneTimeExecution must be in the future");
                    }
                    break;

                case RecurrenceType.Advanced:
                    if (string.IsNullOrWhiteSpace(cronExpression))
                    {
                        throw new ArgumentException("CronExpression must be specified for Advanced recurrence type");
                    }
                    // TODO: Add cron expression validation
                    break;

                case RecurrenceType.Minutes:
                case RecurrenceType.Hourly:
                case RecurrenceType.Daily:
                case RecurrenceType.Weekly:
                case RecurrenceType.Monthly:
                    // These types are valid as-is
                    break;

                default:
                    throw new ArgumentException($"Unsupported recurrence type: {recurrenceType}");
            }
        }

        private async Task<ScheduleResponseDto> MapToResponseDto(Schedule schedule)
        {
            // Ensure navigation properties are loaded
            if (schedule.AutomationPackage == null || schedule.BotAgent == null)
            {
                schedule = await _context.Schedules
                    .Include(s => s.AutomationPackage)
                    .Include(s => s.BotAgent)
                    .Where(s => s.Id == schedule.Id)
                    .FirstAsync();
            }

            var responseDto = new ScheduleResponseDto
            {
                Id = schedule.Id,
                Name = schedule.Name,
                Description = schedule.Description,
                IsEnabled = schedule.IsEnabled,
                RecurrenceType = schedule.RecurrenceType,
                CronExpression = schedule.CronExpression,
                OneTimeExecution = schedule.OneTimeExecution,
                TimeZoneId = schedule.TimeZoneId,
                AutomationPackageId = schedule.AutomationPackageId,
                BotAgentId = schedule.BotAgentId,
                AutomationPackageName = schedule.AutomationPackage?.Name,
                BotAgentName = schedule.BotAgent?.Name,
                OrganizationUnitId = schedule.OrganizationUnitId,
                CreatedAt = schedule.CreatedAt ?? DateTimeUtility.UtcNow,
                UpdatedAt = schedule.LastModifyAt ?? DateTimeUtility.UtcNow
            };

            // Calculate next run time
            responseDto.NextRunTime = CalculateNextRunTime(responseDto);

            return responseDto;
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

        public async Task<ScheduleResponseDto?> RecalculateScheduleAsync(Guid scheduleId)
        {
            var schedule = await _context.Schedules
                .Where(s => s.Id == scheduleId)
                .FirstOrDefaultAsync();

            if (schedule == null)
                return null;

            try
            {
                // Get the current schedule as DTO to recalculate next run time
                var responseDto = await MapToResponseDto(schedule);
                
                // Recalculate next run time with the corrected logic
                var newNextRunTime = CalculateNextRunTime(responseDto);
                
                _logger.LogInformation("Recalculating schedule {ScheduleId}: Old next run {OldNextRun}, New next run {NewNextRun}", 
                    scheduleId, responseDto.NextRunTime, newNextRunTime);

                // Update the response DTO with new calculation
                responseDto.NextRunTime = newNextRunTime;

                // Recreate Quartz job with updated timing
                await _quartzManager.DeleteJobAsync(scheduleId);
                
                if (schedule.IsEnabled && newNextRunTime.HasValue)
                {
                    await _quartzManager.CreateJobAsync(responseDto);
                    _logger.LogInformation("Successfully recreated Quartz job for schedule {ScheduleId} with next run time {NextRunTime}", 
                        scheduleId, newNextRunTime);
                }

                return responseDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating schedule {ScheduleId}: {Message}", scheduleId, ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<BulkDeleteResultDto> BulkDeleteSchedulesAsync(List<Guid> ids)
        {
            var result = new BulkDeleteResultDto
            {
                TotalRequested = ids.Count
            };

            try
            {
                _logger.LogInformation("Starting bulk delete for {Count} schedules", ids.Count);

                // Get schedules that exist and belong to the current tenant
                var schedulesToDelete = await _context.Schedules
                    .Where(s => ids.Contains(s.Id) && s.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .ToListAsync();

                var foundIds = schedulesToDelete.Select(s => s.Id).ToList();

                // Track schedules not found
                var notFoundIds = ids.Except(foundIds).ToList();
                foreach (var notFoundId in notFoundIds)
                {
                    result.Errors.Add(new BulkDeleteErrorDto
                    {
                        Id = notFoundId,
                        ErrorMessage = "Schedule not found or access denied",
                        ErrorCode = "NotFound"
                    });
                }

                 // Delete each schedule
                 foreach (var schedule in schedulesToDelete)
                 {
                     try
                     {
                         // Remove Quartz job first
                         await _quartzManager.DeleteJobAsync(schedule.Id);
                         
                         // Remove the schedule from database
                         _context.Schedules.Remove(schedule);
                         
                         result.DeletedIds.Add(schedule.Id);
                         result.SuccessfullyDeleted++;
                         
                         _logger.LogInformation("Schedule and Quartz job deleted: {ScheduleId}", schedule.Id);
                     }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting schedule {ScheduleId}: {Message}", schedule.Id, ex.Message);
                        result.Errors.Add(new BulkDeleteErrorDto
                        {
                            Id = schedule.Id,
                            ErrorMessage = ex.Message,
                            ErrorCode = "DeleteError"
                        });
                        result.Failed++;
                    }
                }

                // Save changes if there were successful deletions
                if (result.SuccessfullyDeleted > 0)
                {
                    await _context.SaveChangesAsync();
                }

                // result.Failed is already calculated correctly from incremental result.Failed++

                _logger.LogInformation("Bulk delete completed. Successful: {Success}, Failed: {Failed}", 
                    result.SuccessfullyDeleted, result.Failed);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk delete schedules operation: {Message}", ex.Message);
                throw new InvalidOperationException("Error occurred during bulk delete operation", ex);
            }
        }
    }
} 