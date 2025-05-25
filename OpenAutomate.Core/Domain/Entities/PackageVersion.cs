using OpenAutomate.Core.Domain.Base;
using System;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Domain.Entities
{
    public class PackageVersion : TenantEntity
    {
        public string VersionNumber { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty; // This will store the S3 object key
        public string FileName { get; set; } = string.Empty; // Original filename
        public long FileSize { get; set; } // File size in bytes
        public string ContentType { get; set; } = "application/zip";
        public bool IsActive { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        // Foreign key for AutomationPackage
        public Guid PackageId { get; set; }
        [JsonIgnore]
        public virtual AutomationPackage? Package { get; set; }
    }
} 