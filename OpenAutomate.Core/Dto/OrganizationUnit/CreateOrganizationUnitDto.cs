using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.OrganizationUnit
{
    public class CreateOrganizationUnitDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
    }
} 