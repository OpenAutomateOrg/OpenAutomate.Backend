using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.Enums;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Constants;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OpenAutomate.API.Attributes
{
    /// <summary>
    /// Requires a specific permission on a resource for authorization
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _resourceName;
        private readonly int _permission;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class
        /// </summary>
        /// <param name="resource">The resource type</param>
        /// <param name="permission">The permission required</param>
        public RequirePermissionAttribute(string resource, int permission)
        {
            _resourceName = resource;
            _permission = permission;
        }
        
        /// <summary>
        /// Executes the permission check before the action executes
        /// </summary>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.Items["User"] as User;

            if (user == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (user.SystemRole == SystemRole.Admin)
            {
                await next();
                return;
            }

            // Get the authorization service from DI
            var authorizationManager = context.HttpContext.RequestServices
                .GetRequiredService<IAuthorizationManager>();
                
            // Check if user has permission
            var hasPermission = await authorizationManager.HasPermissionAsync(
                user.Id, 
                _resourceName, 
                _permission
            );
            
            if (!hasPermission)
            {
                var permissionName = GetPermissionName(_permission);
                var resourceDisplayName = Resources.GetDisplayName(_resourceName);

                var errorMessage = $"Access denied. You need '{permissionName}' permission for '{resourceDisplayName}' to perform this action.";

                context.Result = new ObjectResult(new { message = errorMessage })
                {
                    StatusCode = 403
                };
                return;
            }
            
            await next();
        }

        /// <summary>
        /// Gets the human-readable name for a permission level
        /// </summary>
        /// <param name="permission">The permission level</param>
        /// <returns>Human-readable permission name</returns>
        private static string GetPermissionName(int permission)
        {
            return permission switch
            {
                Permissions.NoAccess => "No Access",
                Permissions.View => "View",
                Permissions.Create => "Create",
                Permissions.Update => "Update",
                Permissions.Delete => "Full Access",
                _ => $"Permission Level {permission}"
            };
        }
    }
}