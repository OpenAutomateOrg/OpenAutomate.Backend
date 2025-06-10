using System;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Dto.Schedule
{
    /// <summary>
    /// Query parameters for filtering and paginating schedules
    /// </summary>
    public class ScheduleQueryParameters
    {
        /// <summary>
        /// Page number for pagination (1-based)
        /// </summary>
        public int PageNumber { get; set; } = 1;
        
        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; } = 10;
        
        /// <summary>
        /// Search term to filter by name or description
        /// </summary>
        public string? Search { get; set; }
        
        /// <summary>
        /// Filter by schedule type (1=OneTime, 2=Recurring, 3=Cron)
        /// </summary>
        public int? Type { get; set; }
        
        /// <summary>
        /// Filter by active status
        /// </summary>
        public bool? IsActive { get; set; }
        
        /// <summary>
        /// Filter by package ID
        /// </summary>
        public Guid? PackageId { get; set; }
        
        /// <summary>
        /// Filter by creation date from
        /// </summary>
        public DateTime? CreatedFrom { get; set; }
        
        /// <summary>
        /// Filter by creation date to
        /// </summary>
        public DateTime? CreatedTo { get; set; }
        
        /// <summary>
        /// Sort field (Name, Type, CreatedAt, NextExecution)
        /// </summary>
        public string? SortBy { get; set; } = "CreatedAt";
        
        /// <summary>
        /// Sort direction (asc, desc)
        /// </summary>
        public string? SortDirection { get; set; } = "desc";
    }
} 