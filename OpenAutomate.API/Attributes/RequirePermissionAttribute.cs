using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.IServices;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OpenAutomate.API.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _resourceName;
        private readonly int _permission;
        
        public RequirePermissionAttribute(string resourceName, int permission)
        {
            _resourceName = resourceName;
            _permission = permission;
        }
        
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.Items["User"] as User;

            if (user == null)
            {
                context.Result = new UnauthorizedResult();
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