using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.API.Controllers
{

    [Controller]
    public abstract class CustomControllerBase : ControllerBase
    {
        // returns the current authenticated account (null if not logged in)
        public User currentUser => (User)HttpContext.Items["User"];
    }
}
