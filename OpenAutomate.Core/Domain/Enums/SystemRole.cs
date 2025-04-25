namespace OpenAutomate.Core.Domain.Enums
{
    /// <summary>
    /// Defines system-wide roles that determine global access levels
    /// </summary>
    /// <remarks>
    /// These roles are separate from tenant-specific authorities and control access to system-wide functionality
    /// </remarks>
    public enum SystemRole
    {
        /// <summary>
        /// Standard user with access limited to assigned tenant and permissions
        /// </summary>
        User = 0,
        
        /// <summary>
        /// System administrator with full access to system-wide functionality
        /// </summary>
        Admin = 1
    }
} 