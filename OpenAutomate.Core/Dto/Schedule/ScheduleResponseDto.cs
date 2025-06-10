using System;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Dto.Schedule
{
    /// <summary>
    /// DTO for schedule response data
    /// </summary>
    public class ScheduleResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public ScheduleType Type { get; set; }
        public DateTime? OneTimeExecutionDate { get; set; }
        public Guid PackageId { get; set; }
        public Guid CreatedById { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastModifyAt { get; set; }
        
        // Navigation properties
        public string? PackageName { get; set; }
        public string? CreatedByName { get; set; }
        
        // Calculated properties
        public DateTime? NextExecution { get; set; }
        public DateTime? LastExecution { get; set; }
        public string? LastExecutionStatus { get; set; }
    }
} 