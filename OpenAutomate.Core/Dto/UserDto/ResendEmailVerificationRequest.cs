using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Dto.UserDto
{
    public class ResendEmailVerificationRequest
    {
        required public string Email { get; set; }
    }
}
