using System;
using OpenAutomate.Core.Domain.Base;

namespace OpenAutomate.Core.Domain.Entities
{
    /// <summary>
    /// Represents usage tracking for features within a tenant/organization unit
    /// </summary>
    public class UsageRecord : TenantEntity
    {
        /// <summary>
        /// The feature being tracked (e.g., "AIMessages")
        /// </summary>
        public string Feature { get; set; } = string.Empty;

        /// <summary>
        /// Current usage count for the feature
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// When the usage count will reset (typically monthly)
        /// </summary>
        public DateTime ResetDate { get; set; }

        /// <summary>
        /// Maximum allowed usage for this feature
        /// </summary>
        public int? UsageLimit { get; set; }

        /// <summary>
        /// Whether the usage limit has been exceeded
        /// </summary>
        public bool IsOverLimit => UsageLimit.HasValue && UsageCount >= UsageLimit.Value;

        /// <summary>
        /// Whether the usage count needs to be reset based on the reset date
        /// </summary>
        public bool NeedsReset => DateTime.UtcNow >= ResetDate;
    }
}