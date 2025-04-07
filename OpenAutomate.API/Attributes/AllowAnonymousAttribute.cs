using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace OpenAutomate.API.Attributes
{
    /// <summary>
    /// Attribute to mark a controller or action method as accessible without authentication
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AllowAnonymousAttribute : Attribute
    {
        // This is a marker attribute, no implementation needed
    }
} 