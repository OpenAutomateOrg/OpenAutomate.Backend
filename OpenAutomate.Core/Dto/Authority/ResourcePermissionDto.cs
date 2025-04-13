using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Authority
{
    public class ResourcePermissionDto
    {
        [Required]
        public string AuthorityName { get; set; }
        
        [Required]
        public string ResourceName { get; set; }
        
        [Required]
        [Range(1, 4)]
        public int Permission { get; set; }
    }
} 