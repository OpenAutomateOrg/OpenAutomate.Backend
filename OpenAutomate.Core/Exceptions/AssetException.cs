using System;

namespace OpenAutomate.Core.Exceptions
{
    /// <summary>
    /// Base exception for Asset-related errors
    /// </summary>
    public class AssetException : OpenAutomateException
    {
        public AssetException(string message) : base(message)
        {
        }

        public AssetException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when an Asset with the same key already exists
    /// </summary>
    public class AssetKeyAlreadyExistsException : AssetException
    {
        public string Key { get; }
        public Guid TenantId { get; }

        public AssetKeyAlreadyExistsException(string key, Guid tenantId) 
            : base($"Asset with key '{key}' already exists for tenant {tenantId}")
        {
            Key = key;
            TenantId = tenantId;
        }

        public AssetKeyAlreadyExistsException(string key, Guid tenantId, Exception innerException) 
            : base($"Asset with key '{key}' already exists for tenant {tenantId}", innerException)
        {
            Key = key;
            TenantId = tenantId;
        }
    }

    /// <summary>
    /// Exception thrown when an Asset is not found
    /// </summary>
    public class AssetNotFoundException : AssetException
    {
        public Guid AssetId { get; }
        public Guid TenantId { get; }

        public AssetNotFoundException(Guid assetId, Guid tenantId)
            : base($"Asset with ID {assetId} not found for tenant {tenantId}")
        {
            AssetId = assetId;
            TenantId = tenantId;
        }

        public AssetNotFoundException(Guid assetId, Guid tenantId, Exception innerException)
            : base($"Asset with ID {assetId} not found for tenant {tenantId}", innerException)
        {
            AssetId = assetId;
            TenantId = tenantId;
        }
    }

    /// <summary>
    /// Exception thrown when there's an error during asset encryption/decryption
    /// </summary>
    public class AssetEncryptionException : AssetException
    {
        public AssetEncryptionException(string message) : base(message)
        {
        }

        public AssetEncryptionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
} 