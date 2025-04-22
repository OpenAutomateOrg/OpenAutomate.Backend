using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.BotAgent
{
    /// <summary>
    /// DTO for creating a new Bot Agent
    /// </summary>
    public class CreateBotAgentDto
    {
        /// <summary>
        /// The display name for the Bot Agent
        /// </summary>
        [Required]
        public string Name { get; set; }
        
        /// <summary>
        /// The machine name of the computer where the Bot Agent will run
        /// </summary>
        [Required]
        public string MachineName { get; set; }
    }
} 