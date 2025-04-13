using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Authority
{
    public class AssignAuthorityDto
    {
        [Required]
        public string AuthorityName { get; set; }
    }
} 