using System;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Execution
{
    /// <summary>
    /// DTO for triggering a new execution from the frontend
    /// </summary>
    public class TriggerExecutionDto
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
        /// Name of the package (for logging and display)
        /// </summary>
        [Required]
        public string PackageName { get; set; } = string.Empty;

        /// <summary>
        /// Version of the package to execute
        /// </summary>
        [Required]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// ID of the user creating the execution (for scheduled executions, this is the schedule creator)
        /// </summary>
        public Guid? CreatedBy { get; set; }
    }
} 