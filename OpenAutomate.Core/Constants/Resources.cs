using OpenAutomate.Core.Dto.Authority;

namespace OpenAutomate.Core.Constants
{
    /// <summary>
    /// Defines resource types for authorization
    /// </summary>
    public static class Resources
    {
        /// <summary>
        /// Bot Agent resource
        /// </summary>
        public const string AgentResource = "BotAgent";
        
        /// <summary>
        /// Asset resource
        /// </summary>
        public const string AssetResource = "Asset";
        
        /// <summary>
        /// Automation Package resource
        /// </summary>
        public const string PackageResource = "AutomationPackage";
        
        /// <summary>
        /// Execution resource
        /// </summary>
        public const string ExecutionResource = "Execution";
        
        /// <summary>
        /// Schedule resource
        /// </summary>
        public const string ScheduleResource = "Schedule";
        
        /// <summary>
        /// User resource
        /// </summary>
        public const string UserResource = "User";
        
        /// <summary>
        /// Organization Unit resource
        /// </summary>
        public const string OrganizationUnitResource = "OrganizationUnit";
        
        /// <summary>
        /// Subscription resource
        /// </summary>
        public const string SubscriptionResource = "Subscription";
        
        /// <summary>
        /// Gets all available resources with their display information
        /// </summary>
        /// <returns>List of available resources for role creation</returns>
        public static List<AvailableResourceDto> GetAvailableResources()
        {
            return new List<AvailableResourceDto>
            {
                new AvailableResourceDto
                {
                    ResourceName = AgentResource,
                    DisplayName = "Bot Agents",
                    Description = "Manage bot agents and their configurations",
                    AvailablePermissions = GetPermissionLevels()
                },
                new AvailableResourceDto
                {
                    ResourceName = AssetResource,
                    DisplayName = "Assets",
                    Description = "Manage secrets, configurations, and other assets",
                    AvailablePermissions = GetPermissionLevels()
                },
                new AvailableResourceDto
                {
                    ResourceName = PackageResource,
                    DisplayName = "Automation Packages",
                    Description = "Manage automation packages and versions",
                    AvailablePermissions = GetPermissionLevels()
                },
                new AvailableResourceDto
                {
                    ResourceName = ExecutionResource,
                    DisplayName = "Executions",
                    Description = "View and manage automation executions",
                    AvailablePermissions = GetPermissionLevels()
                },
                new AvailableResourceDto
                {
                    ResourceName = ScheduleResource,
                    DisplayName = "Schedules",
                    Description = "Manage automation schedules and recurring tasks",
                    AvailablePermissions = GetPermissionLevels()
                },
                new AvailableResourceDto
                {
                    ResourceName = UserResource,
                    DisplayName = "Users",
                    Description = "Manage user accounts and access",
                    AvailablePermissions = GetPermissionLevels()
                },
                new AvailableResourceDto
                {
                    ResourceName = OrganizationUnitResource,
                    DisplayName = "Organization",
                    Description = "Manage organization settings and roles",
                    AvailablePermissions = GetPermissionLevels()
                },
                new AvailableResourceDto
                {
                    ResourceName = SubscriptionResource,
                    DisplayName = "Subscription",
                    Description = "Manage subscription plans and billing",
                    AvailablePermissions = GetPermissionLevels()
                }
            };
        }
        
        /// <summary>
        /// Gets all permission levels with descriptions
        /// </summary>
        /// <returns>List of permission levels</returns>
        private static List<PermissionLevelDto> GetPermissionLevels()
        {
            return new List<PermissionLevelDto>
            {
                new PermissionLevelDto
                {
                    Level = Permissions.NoAccess,
                    Name = "No Access",
                    Description = "No access to this resource"
                },
                new PermissionLevelDto
                {
                    Level = Permissions.View,
                    Name = "View",
                    Description = "View-only access"
                },
                new PermissionLevelDto
                {
                    Level = Permissions.Create,
                    Name = "Create",
                    Description = "View and create new items"
                },
                new PermissionLevelDto
                {
                    Level = Permissions.Update,
                    Name = "Update",
                    Description = "View, create, and modify items"
                },
                new PermissionLevelDto
                {
                    Level = Permissions.Delete,
                    Name = "Full Access",
                    Description = "Complete access including delete operations"
                }
            };
        }
        
        /// <summary>
        /// Gets the display name for a resource
        /// </summary>
        /// <param name="resourceName">The resource name</param>
        /// <returns>Display name or the resource name if not found</returns>
        public static string GetDisplayName(string resourceName)
        {
            var resource = GetAvailableResources().FirstOrDefault(r => r.ResourceName == resourceName);
            return resource?.DisplayName ?? resourceName;
        }
    }
} 