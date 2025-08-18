using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.Enums;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.Dto.Common;
using OpenAutomate.Core.IServices;
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
            }

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
                OneTimeExecution = dto.OneTimeExecution,
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

            var timeZone = GetTimeZoneInfo(schedule.TimeZoneId);
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
                var now = DateTime.UtcNow;
                var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, timeZone);
                var baseTime = localNow.Date.AddHours(hour).AddMinutes(minute).AddSeconds(second);
                var nextTime = baseTime > localNow ? baseTime : baseTime.AddDays(1);

                for (int i = 0; i < count; i++)
                {
                    upcomingTimes.Add(TimeZoneInfo.ConvertTimeToUtc(nextTime.AddDays(i), timeZone));
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

            var now = DateTime.UtcNow;
            var timeZone = GetTimeZoneInfo(schedule.TimeZoneId);

            return schedule.RecurrenceType switch
            {
                RecurrenceType.Once => CalculateOnceNextRunTime(schedule, timeZone, now),
                RecurrenceType.Minutes => now.AddMinutes(30), // Default to 30 minutes to match Quartz configuration
                RecurrenceType.Hourly => CalculateHourlyNextRunTime(schedule, timeZone, now),
                RecurrenceType.Daily => CalculateDailyNextRunTime(schedule, timeZone, now),
                RecurrenceType.Weekly => CalculateWeeklyNextRunTime(schedule, timeZone, now),
                RecurrenceType.Monthly => CalculateMonthlyNextRunTime(schedule, timeZone, now),
                RecurrenceType.Advanced => CalculateAdvancedNextRunTime(schedule, timeZone),
                _ => null
            };
        }

        private static DateTime? CalculateOnceNextRunTime(ScheduleResponseDto schedule, TimeZoneInfo timeZone, DateTime now)
        {
            if (!schedule.OneTimeExecution.HasValue)
                return null;

            // Ensure the stored time is treated as UTC
            var oneTimeUtc = DateTime.SpecifyKind(schedule.OneTimeExecution.Value, DateTimeKind.Utc);

            // Convert to the schedule's local time zone
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(oneTimeUtc, timeZone);

            // Only return if the time is in the future (in the schedule's timezone)
            if (localTime <= TimeZoneInfo.ConvertTimeFromUtc(now, timeZone))
                return null;

            return localTime;
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

            // Fallback: Calculate next occurrence at the same time tomorrow
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, timeZone);
            var tomorrow = localNow.Date.AddDays(1);

            if (schedule.CronExpression != null && TryParseDailyCronTime(schedule.CronExpression, out var hour, out var minute))
            {
                tomorrow = tomorrow.AddHours(hour).AddMinutes(minute);
            }

            return TimeZoneInfo.ConvertTimeToUtc(tomorrow, timeZone);
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

                var now = DateTime.UtcNow;
                var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, timeZone);

                // For daily schedules, calculate next occurrence
                if (parts[3] == "*" && parts[4] == "*" && parts[5] == "*" &&
                    int.TryParse(parts[2], out var hour) && int.TryParse(parts[1], out var minute) && int.TryParse(parts[0], out var second))
                {
                    var todayAtTime = localNow.Date.AddHours(hour).AddMinutes(minute).AddSeconds(second);
                    var nextRun = todayAtTime > localNow ? todayAtTime : todayAtTime.AddDays(1);
                    return TimeZoneInfo.ConvertTimeToUtc(nextRun, timeZone);
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
                    if (oneTimeExecution.Value <= DateTime.UtcNow)
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
                CreatedAt = schedule.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = schedule.LastModifyAt ?? DateTime.UtcNow
            };

            // Calculate next run time
            responseDto.NextRunTime = CalculateNextRunTime(responseDto);

            return responseDto;
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