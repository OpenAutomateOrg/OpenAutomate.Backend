using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using OpenAutomate.Core.Domain.Base;

namespace OpenAutomate.Core.Domain.Entities
{
    /// <summary>
    /// Represents a secure asset or credential that can be used by automation
    /// </summary>
    public class Asset : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// The display name for the Asset
        /// </summary>
        [Required]
        public string Name { get; set; }
        
        /// <summary>
        /// The unique key used to reference this Asset
        /// </summary>
        [Required]
        public string Key { get; set; }
        
        /// <summary>
        /// The value of the Asset (may be encrypted)
        /// </summary>
        [Required]
        public string Value { get; set; }
        
        /// <summary>
        /// Description of the Asset
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Whether the Asset value is encrypted
        /// </summary>
        public bool IsEncrypted { get; set; } = true;
        
        /// <summary>
        /// The tenant ID this Asset belongs to
        /// </summary>
        [Required]
        public Guid OrganizationUnitId { get; set; }
        
        /// <summary>
        /// Navigation property for tenant
        /// </summary>
        [ForeignKey("OrganizationUnitId")]
        [JsonIgnore]
        public virtual OrganizationUnit OrganizationUnit { get; set; }
        
        /// <summary>
        /// Bot Agents that have access to this Asset
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<AssetBotAgent> AssetBotAgents { get; set; }
    }
} 