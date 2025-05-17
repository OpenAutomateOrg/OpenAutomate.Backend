using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using OpenAutomate.Core.Domain.Base;
using OpenAutomate.Core.Dto.Asset;

namespace OpenAutomate.Core.Domain.Entities
{
    /// <summary>
    /// Represents a secure asset or credential that can be used by automation
    /// </summary>
    public class Asset : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// The unique key used to reference this Asset
        /// </summary>
        [Required]
        public string Key { get; set; } = string.Empty;
        
        /// <summary>
        /// The value of the Asset (may be encrypted)
        /// </summary>
        [Required]
        public string Value { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the Asset
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
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
        public virtual OrganizationUnit OrganizationUnit { get; set; } = null!;
        
        /// <summary>
        /// Bot Agents that have access to this Asset
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<AssetBotAgent> AssetBotAgents { get; set; } = new List<AssetBotAgent>();

        /// <summary>
        /// Gets the asset type based on encryption status
        /// </summary>
        [NotMapped]
        public AssetType Type => IsEncrypted ? AssetType.Secret : AssetType.String;
    }   
} 