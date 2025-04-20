using OpenAutomate.Core.Domain.Base;
using System;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Domain.Entities
{
    public class PackageVersion : TenantEntity
    {
        public string VersionNumber { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Foreign key for AutomationPackage
        public Guid PackageId { get; set; }
        [JsonIgnore]
        public virtual AutomationPackage? Package { get; set; }
    }
} 