using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Asset
{
    /// <summary>
    /// DTO for updating the Bot Agents that can access an Asset
    /// </summary>
    public class AssetBotAgentDto
    {
        /// <summary>
        /// List of Bot Agent IDs that can access this Asset
        /// </summary>
        [Required]
        public List<Guid> BotAgentIds { get; set; } = new List<Guid>();
    }
} 