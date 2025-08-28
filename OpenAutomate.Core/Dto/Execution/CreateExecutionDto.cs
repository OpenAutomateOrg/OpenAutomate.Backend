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
        /// ID of the user creating this execution
        /// </summary>
        public Guid? CreatedBy { get; set; }
    }
} 