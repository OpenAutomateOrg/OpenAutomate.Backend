using System;
using System.ComponentModel.DataAnnotations;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Dto.Schedule
{
    /// <summary>
    /// DTO for creating a new schedule
    /// </summary>
    public class CreateScheduleDto
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
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Type of schedule (OneTime, Recurring, Cron)
        /// </summary>
        [Required]
        public ScheduleType Type { get; set; }
        
        /// <summary>
        /// Cron expression for scheduling (required for Cron and Recurring types)
        /// </summary>
        [StringLength(100)]
        public string? CronExpression { get; set; }
        
        /// <summary>
        /// Execution date for one-time schedules
        /// </summary>
        public DateTime? OneTimeExecutionDate { get; set; }
        
        /// <summary>
        /// ID of the automation package to execute
        /// </summary>
        [Required]
        public Guid PackageId { get; set; }
    }
} 