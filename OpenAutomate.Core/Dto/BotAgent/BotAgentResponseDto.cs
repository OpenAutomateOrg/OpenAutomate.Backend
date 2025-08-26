using System;

namespace OpenAutomate.Core.Dto.BotAgent
{
    /// <summary>
    /// DTO for Bot Agent response data
    /// </summary>
    public class BotAgentResponseDto
    {
        /// <summary>
        /// Unique identifier for the Bot Agent
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// The display name for the Bot Agent
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The machine name of the computer where the Bot Agent runs
        /// </summary>
        public string MachineName { get; set; }
        
        /// <summary>
        /// The machine key used for authentication (only returned during creation/regeneration)
        /// </summary>
        public string? MachineKey { get; set; }

        /// <summary>
        /// Current status of the Bot Agent (Pending, Online, Offline, etc.)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Timestamp of the last successful connection from this Bot Agent (excluded from general responses)
        /// </summary>
        public DateTime? LastConnected { get; set; }
        
        /// <summary>
        /// Whether the Bot Agent is active and allowed to connect
        /// </summary>
        public bool IsActive { get; set; }
    }
} 