using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Asset
{
    /// <summary>
    /// Data transfer object for Bot Agent asset requests
    /// </summary>
    public class BotAgentAssetDto
    {
        /// <summary>
        /// The machine key for authenticating the Bot Agent
        /// </summary>
        [Required]
        public string MachineKey { get; set; }
    }
} 