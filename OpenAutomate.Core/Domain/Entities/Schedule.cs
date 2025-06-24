using System;
using System.ComponentModel.DataAnnotations;
using OpenAutomate.Core.Domain.Base;
using OpenAutomate.Core.Domain.Enums;

namespace OpenAutomate.Core.Domain.Entities
{
    /// <summary>
    /// Represents a schedule for automated execution of packages on bot agents
    /// </summary>
    public class Schedule : TenantEntity
    {
        /// <summary>
        /// Name of the schedule
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the schedule
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Whether the schedule is currently enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Type of recurrence for the schedule
        /// </summary>
        [Required]
        public RecurrenceType RecurrenceType { get; set; }

        /// <summary>
        /// Cron expression for complex scheduling (used with Advanced recurrence type)
        /// </summary>
        public string? CronExpression { get; set; }

        /// <summary>
        /// Specific date and time for one-time execution (used with Once recurrence type)
        /// </summary>
        public DateTime? OneTimeExecution { get; set; }

        /// <summary>
        /// IANA Time Zone ID for schedule execution (e.g., "America/New_York")
        /// </summary>
        [Required]
        public string TimeZoneId { get; set; } = "UTC";

        /// <summary>
        /// Foreign key to the automation package to execute
        /// </summary>
        [Required]
        public Guid AutomationPackageId { get; set; }

        /// <summary>
        /// Foreign key to the bot agent that will execute the package
        /// </summary>
        [Required]
        public Guid BotAgentId { get; set; }

        /// <summary>
        /// Navigation property to the automation package
        /// </summary>
        public virtual AutomationPackage AutomationPackage { get; set; } = null!;

        /// <summary>
        /// Navigation property to the bot agent
        /// </summary>
        public virtual BotAgent BotAgent { get; set; } = null!;
    }
} 