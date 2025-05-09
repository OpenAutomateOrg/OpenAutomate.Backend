using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.BotAgent
{
    /// <summary>
    /// DTO for sending commands to Bot Agents
    /// </summary>
    public class BotAgentCommandDto
    {
        /// <summary>
        /// The type of command to execute
        /// </summary>
        [Required]
        public required string CommandType { get; set; }
        
        /// <summary>
        /// Additional data for the command
        /// </summary>
        public object? Payload { get; set; }
    }
} 