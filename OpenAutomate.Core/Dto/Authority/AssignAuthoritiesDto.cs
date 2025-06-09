using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Dto.Authority
{
    public class AssignAuthoritiesDto
    {
        [Required]
        public List<Guid> AuthorityIds { get; set; } = new();
    }
}
