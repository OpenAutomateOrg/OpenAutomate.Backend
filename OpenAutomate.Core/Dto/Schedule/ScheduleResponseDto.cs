using System;
using OpenAutomate.Core.Domain.Enums;

namespace OpenAutomate.Core.Dto.Schedule
{
    /// <summary>
    /// DTO for schedule responses
    /// </summary>
    public class ScheduleResponseDto
    {
        /// <summary>
        /// Unique identifier of the schedule
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the schedule
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the schedule
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether the schedule is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Type of recurrence for the schedule
        /// </summary>
        public RecurrenceType RecurrenceType { get; set; }

        /// <summary>
        /// Cron expression for advanced scheduling
        /// </summary>
        public string? CronExpression { get; set; }

        /// <summary>
        /// Specific date and time for one-time execution
        /// </summary>
        public DateTime? OneTimeExecution { get; set; }

        /// <summary>
        /// IANA Time Zone ID for schedule execution
        /// </summary>
        public string TimeZoneId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the automation package to execute
        /// </summary>
        public Guid AutomationPackageId { get; set; }

        /// <summary>
        /// ID of the bot agent that will execute the package
        /// </summary>
        public Guid BotAgentId { get; set; }

        /// <summary>
        /// Name of the automation package
        /// </summary>
        public string? AutomationPackageName { get; set; }

        /// <summary>
        /// Name of the bot agent
        /// </summary>
        public string? BotAgentName { get; set; }

        /// <summary>
        /// Calculated next run time based on the schedule
        /// </summary>
        public DateTime? NextRunTime { get; set; }

        /// <summary>
        /// ID of the organization unit (tenant)
        /// </summary>
        public Guid OrganizationUnitId { get; set; }

        /// <summary>
        /// Date and time when the schedule was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time when the schedule was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Recurrence object for FE to fill edit form
        /// </summary>
        public RecurrenceDto? Recurrence { get; set; }
    }

    /// <summary>
    /// Recurrence details for schedule
    /// </summary>
    public class RecurrenceDto
    {
        public string Type { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? StartTime { get; set; }
        public string? DailyHour { get; set; }
        public string? DailyMinute { get; set; }
        public string? WeeklyHour { get; set; }
        public string? WeeklyMinute { get; set; }
        public List<string>? SelectedDays { get; set; }
        public string? MonthlyHour { get; set; }
        public string? MonthlyMinute { get; set; }
        public string? MonthlyOnType { get; set; }
        public string? SelectedDay { get; set; }
        public string? SelectedOrdinal { get; set; }
        public string? SelectedWeekday { get; set; }
        public List<string>? SelectedMonths { get; set; }
    }
} 