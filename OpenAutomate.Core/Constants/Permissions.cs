namespace OpenAutomate.Core.Constants
{
    /// <summary>
    /// Defines permission types for authorization
    /// </summary>
    public static class Permissions
    {
        /// <summary>
        /// View permission (read-only access)
        /// </summary>
        public const int View = 1;
        
        /// <summary>
        /// Create permission (ability to create new resources)
        /// </summary>
        public const int Create = 2;
        
        /// <summary>
        /// Execute permission (ability to run operations, higher than View, lower than Update)
        /// </summary>
        public const int Execute = 3;
        
        /// <summary>
        /// Update permission (ability to modify existing resources)
        /// </summary>
        public const int Update = 4;
        
        /// <summary>
        /// Delete permission (highest level - can delete resources)
        /// </summary>
        public const int Delete = 5;
        
    }
} 