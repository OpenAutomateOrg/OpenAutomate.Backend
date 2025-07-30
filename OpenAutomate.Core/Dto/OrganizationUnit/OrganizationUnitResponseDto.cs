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
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsPendingDeletion { get; set; }
        public DateTime? ScheduledDeletionAt { get; set; }
        public int? DaysUntilDeletion { get; set; }
    }
} 