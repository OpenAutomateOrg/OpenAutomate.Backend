using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.Enums;
using OpenAutomate.Core.IServices;
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
                context.Result = new ForbidResult();
                return;
            }
            
            await next();
        }
    }
} 