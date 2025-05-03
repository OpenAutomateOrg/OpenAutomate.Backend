using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Base controller that provides common functionality for all API controllers
    /// </summary>
    [Controller]
    public abstract class CustomControllerBase : ControllerBase
    {
        /// <summary>
        /// Gets the current authenticated user from the HttpContext
        /// </summary>
        /// <remarks>Returns null if no user is authenticated</remarks>
        public User? currentUser => HttpContext.Items["User"] as User;

        /// <summary>
        /// Gets the ID of the currently authenticated user
        /// </summary>
        /// <returns>The user's ID</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if no user is authenticated</exception>
        protected Guid GetCurrentUserId()
        {
            if (currentUser == null)
                throw new UnauthorizedAccessException("User is not authenticated");
                
            return currentUser.Id;
        }
    }
}
