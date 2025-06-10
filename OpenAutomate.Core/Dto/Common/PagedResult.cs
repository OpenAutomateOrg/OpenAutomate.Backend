using System.Collections.Generic;

namespace OpenAutomate.Core.Dto.Common
{
    /// <summary>
    /// Represents a paged result with items and pagination metadata
    /// </summary>
    /// <typeparam name="T">The type of items in the result</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// The items in the current page
        /// </summary>
        public List<T> Items { get; set; } = new();
        
        /// <summary>
        /// The current page number (1-based)
        /// </summary>
        public int PageNumber { get; set; }
        
        /// <summary>
        /// The number of items per page
        /// </summary>
        public int PageSize { get; set; }
        
        /// <summary>
        /// The total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// The total number of pages
        /// </summary>
        public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);
        
        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;
        
        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;
    }
} 