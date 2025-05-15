using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Asset
{
    /// <summary>
    /// Data transfer object containing just the Bot Agent machine key
    /// </summary>
    public class BotAgentKeyDto
    {
        /// <summary>
        /// The machine key for authenticating the Bot Agent
        /// </summary>
        [Required]
        public string MachineKey { get; set; }
    }
} 