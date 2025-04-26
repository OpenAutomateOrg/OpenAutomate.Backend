using OpenAutomate.Core.Domain.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Domain.Entities
{
    /// <summary>
    /// Represents a Bot Agent that can execute automations
    /// </summary>
    public class BotAgent : TenantEntity
    {
        /// <summary>
        /// The display name of the Bot Agent
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// The secure key used for authentication
        /// </summary>
        [Required]
        public string MachineKey { get; set; } = string.Empty;
        
        /// <summary>
        /// The name of the machine where the Bot Agent runs
        /// </summary>
        [Required]
        public string MachineName { get; set; } = string.Empty;
        
        /// <summary>
        /// Current status of the Bot Agent (Pending, Online, Offline, etc.)
        /// </summary>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Last time the Bot Agent connected to the server
        /// </summary>
        public DateTime LastConnected { get; set; }
        
        /// <summary>
        /// Last time the Bot Agent sent a heartbeat
        /// </summary>
        public DateTime LastHeartbeat { get; set; }
        
        /// <summary>
        /// Whether the Bot Agent is active and allowed to connect
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// The user who CreatedAtthis Bot Agent
        /// </summary>
        [JsonIgnore]
        public virtual User? Owner { get; set; }
        
        /// <summary>
        /// Executions performed by this Bot Agent
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<Execution>? Executions { get; set; }
        
        /// <summary>
        /// Assets accessible to this Bot Agent
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<AssetBotAgent>? AssetBotAgents { get; set; }
    }
} 