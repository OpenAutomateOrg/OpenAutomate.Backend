using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Execution
{
    /// <summary>
    /// DTO for updating execution status
    /// </summary>
    public class UpdateExecutionStatusDto
    {
        /// <summary>
        /// New execution status
        /// </summary>
        [Required]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Optional error message if execution failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Optional log output from execution
        /// </summary>
        public string? LogOutput { get; set; }
    }
} 