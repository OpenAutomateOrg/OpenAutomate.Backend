namespace OpenAutomate.Core.Constants
{
    /// <summary>
    /// Defines permission levels for authorization (0-5)
    /// Higher permission levels include all lower levels (hierarchical)
    /// </summary>
    public static class Permissions
    {
        /// <summary>
        /// No access permission (0) - explicitly denies access to a resource
        /// </summary>
        public const int NoAccess = 0;
        
        /// <summary>
        /// View permission (1) - read-only access to resources
        /// </summary>
        public const int View = 1;
        
        /// <summary>
        /// Create permission (2) - ability to create new resources
        /// Includes: View
        /// </summary>
        public const int Create = 2;
        
        /// <summary>
        /// Execute permission (3) - ability to run operations and execute tasks
        /// Includes: View, Create
        /// </summary>
        public const int Execute = 3;
        
        /// <summary>
        /// Update permission (4) - ability to modify existing resources
        /// Includes: View, Create, Execute
        /// </summary>
        public const int Update = 4;
        
        /// <summary>
        /// Delete permission (5) - highest level with full administrative access including delete operations
        /// Includes: View, Create, Execute, Update
        /// </summary>
        public const int Delete = 5;
        
        /// <summary>
        /// Validates if a permission level is valid (0-5)
        /// </summary>
        /// <param name="permission">The permission level to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValid(int permission)
        {
            return permission >= NoAccess && permission <= Delete;
        }
        
        /// <summary>
        /// Gets a human-readable description of the permission level
        /// </summary>
        /// <param name="permission">The permission level</param>
        /// <returns>Description of the permission</returns>
        public static string GetDescription(int permission)
        {
            return permission switch
            {
                NoAccess => "No Access",
                View => "View Only",
                Create => "View & Create",
                Execute => "View, Create & Execute",
                Update => "View, Create, Execute & Update",
                Delete => "Full Administrative Access",
                _ => "Invalid Permission"
            };
        }
    }
} 