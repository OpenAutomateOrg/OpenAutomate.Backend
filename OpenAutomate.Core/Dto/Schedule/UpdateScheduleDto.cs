using System;
using System.ComponentModel.DataAnnotations;
using OpenAutomate.Core.Domain.Enums;

namespace OpenAutomate.Core.Dto.Schedule
{
    /// <summary>
    /// DTO for updating an existing schedule
    /// </summary>
    public class UpdateScheduleDto
    {
        /// <summary>
        /// Name of the schedule
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the schedule
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Whether the schedule is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Type of recurrence for the schedule
        /// </summary>
        [Required]
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
        [Required]
        public string TimeZoneId { get; set; } = "UTC";

        /// <summary>
        /// ID of the automation package to execute
        /// </summary>
        [Required]
        public Guid AutomationPackageId { get; set; }

        /// <summary>
        /// ID of the bot agent that will execute the package
        /// </summary>
        [Required]
        public Guid BotAgentId { get; set; }
    }
} 