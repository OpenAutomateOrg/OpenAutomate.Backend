using System;

namespace OpenAutomate.Core.Dto.BotAgent
{
    /// <summary>
    /// DTO for Bot Agent OData responses - excludes sensitive information like machine keys
    /// </summary>
    public class BotAgentODataDto
    {
        /// <summary>
        /// Unique identifier for the Bot Agent
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// The display name for the Bot Agent
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// The machine name of the computer where the Bot Agent runs
        /// </summary>
        public string MachineName { get; set; } = string.Empty;
        
        /// <summary>
        /// Current status of the Bot Agent (Pending, Online, Offline, etc.)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Whether the Bot Agent is active and allowed to connect
        /// </summary>
        public bool IsActive { get; set; }
    }
}
