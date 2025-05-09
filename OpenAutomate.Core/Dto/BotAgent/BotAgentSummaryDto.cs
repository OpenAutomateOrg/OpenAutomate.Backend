using System;

namespace OpenAutomate.Core.Dto.BotAgent
{
    /// <summary>
    /// DTO for summarizing Bot Agent data (used in Asset management)
    /// </summary>
    public class BotAgentSummaryDto
    {
        /// <summary>
        /// The unique identifier of the Bot Agent
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// The name of the Bot Agent
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// The machine name of the Bot Agent
        /// </summary>
        public string MachineName { get; set; } = string.Empty;
        
        /// <summary>
        /// The current status of the Bot Agent
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
} 