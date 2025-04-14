using System;

namespace OpenAutomate.Core.Dto.OrganizationUnit
{
    public class OrganizationUnitResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int UserCount { get; set; }
    }
} 