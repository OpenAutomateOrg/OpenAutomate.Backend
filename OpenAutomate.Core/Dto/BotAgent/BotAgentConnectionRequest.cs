using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.BotAgent
{
    /// <summary>
    /// DTO for Bot Agent connection request
    /// </summary>
    public class BotAgentConnectionRequest
    {
        /// <summary>
        /// The machine key used for authentication
        /// </summary>
        [Required]
        public string MachineKey { get; set; }
        
        /// <summary>
        /// The machine name of the computer where the Bot Agent runs
        /// </summary>
        [Required]
        public string MachineName { get; set; }
    }
} 