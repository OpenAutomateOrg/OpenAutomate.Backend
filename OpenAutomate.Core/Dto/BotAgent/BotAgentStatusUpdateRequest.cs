using System;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.BotAgent
{
    /// <summary>
    /// DTO for Bot Agent status update request
    /// </summary>
    public class BotAgentStatusUpdateRequest
    {
        /// <summary>
        /// The machine key used for authentication
        /// </summary>
        [Required]
        public string MachineKey { get; set; }
        
        /// <summary>
        /// Current status of the Bot Agent (Online, Busy, Error, etc.)
        /// </summary>
        [Required]
        public string Status { get; set; }
        
        /// <summary>
        /// Optional details about the current status
        /// </summary>
        public string Details { get; set; }
        
        /// <summary>
        /// Timestamp when this status was recorded
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
} 