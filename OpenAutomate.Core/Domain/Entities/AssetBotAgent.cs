using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenAutomate.Core.Domain.Base;

namespace OpenAutomate.Core.Domain.Entities
{
    /// <summary>
    /// Maps the relationship between Assets and BotAgents (many-to-many)
    /// </summary>
    public class AssetBotAgent : TenantEntity
    {
        [Required]
        public Guid AssetId { get; set; }
        
        [Required]
        public Guid BotAgentId { get; set; }
        
        // Navigation properties
        [ForeignKey("AssetId")]
        public virtual Asset Asset { get; set; } = null!;
        
        [ForeignKey("BotAgentId")]
        public virtual BotAgent BotAgent { get; set; } = null!;
    }
} 