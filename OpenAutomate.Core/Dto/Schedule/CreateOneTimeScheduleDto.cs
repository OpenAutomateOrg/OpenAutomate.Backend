using System;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Schedule
{
    /// <summary>
    /// DTO for creating a one-time schedule
    /// </summary>
    public class CreateOneTimeScheduleDto
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
        /// Execution date and time for the one-time schedule
        /// </summary>
        [Required]
        public DateTime ExecutionDate { get; set; }
        
        /// <summary>
        /// ID of the automation package to execute
        /// </summary>
        [Required]
        public Guid PackageId { get; set; }
    }
} 