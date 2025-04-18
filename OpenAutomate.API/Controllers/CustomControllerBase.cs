using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.API.Controllers
{

    [Controller]
    public abstract class CustomControllerBase : ControllerBase
    {
        // returns the current authenticated account (null if not logged in)
        public User currentUser => (User)HttpContext.Items["User"];
        
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
