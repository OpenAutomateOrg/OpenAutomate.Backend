using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Extensions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace OpenAutomate.API.Attributes
{
    /// <summary>
    /// Attribute to require authentication for a controller or action method
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthenticationAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Skip authentication if AllowAnonymous attribute is present
            if (HasAllowAnonymousAttribute(context))
            {
                return;
            }
            
            // Check if the user is authenticated
            if (!context.HttpContext.IsAuthenticated())
            {
                // Return 401 Unauthorized if not authenticated
                context.Result = new UnauthorizedResult();
            }
        }
        
        private bool HasAllowAnonymousAttribute(AuthorizationFilterContext context)
        {
            // Check if AllowAnonymous is applied at the action level
            var actionAttributes = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
                
            if (actionAttributes)
            {
                return true;
            }
            
            // Check if AllowAnonymous is applied at the controller level
            if (context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
            {
                var controllerAttributes = controllerActionDescriptor.ControllerTypeInfo.GetCustomAttributes<AllowAnonymousAttribute>(true).Any();;
                return controllerAttributes;
            }
            
            return false;
        }
    }
} 