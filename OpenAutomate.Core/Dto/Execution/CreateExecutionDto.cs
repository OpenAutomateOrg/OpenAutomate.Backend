using System;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Execution
{
    /// <summary>
    /// DTO for creating a new execution
    /// </summary>
    public class CreateExecutionDto
    {
        /// <summary>
        /// ID of the bot agent that will execute the package
        /// </summary>
        [Required]
        public Guid BotAgentId { get; set; }

        /// <summary>
        /// ID of the package to execute
        /// </summary>
        [Required]
        public Guid PackageId { get; set; }

        /// <summary>
        /// Optional schedule ID if this execution is part of a schedule
        /// </summary>
        public Guid? ScheduleId { get; set; }
    }
} 