using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Common
{
    /// <summary>
    /// Base DTO for bulk delete operations
    /// </summary>
    public class BulkDeleteDto
    {
        /// <summary>
        /// List of IDs to delete
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one ID must be provided")]
        public List<Guid> Ids { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// Result of bulk delete operation
    /// </summary>
    public class BulkDeleteResultDto
    {
        /// <summary>
        /// Total number of items requested for deletion
        /// </summary>
        public int TotalRequested { get; set; }

        /// <summary>
        /// Number of items successfully deleted
        /// </summary>
        public int SuccessfullyDeleted { get; set; }

        /// <summary>
        /// Number of items that failed to delete
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// List of IDs that were successfully deleted
        /// </summary>
        public List<Guid> DeletedIds { get; set; } = new List<Guid>();

        /// <summary>
        /// List of errors for items that failed to delete
        /// </summary>
        public List<BulkDeleteErrorDto> Errors { get; set; } = new List<BulkDeleteErrorDto>();

        /// <summary>
        /// Whether the operation was completely successful
        /// </summary>
        public bool IsCompletelySuccessful => Failed == 0;
    }

    /// <summary>
    /// Error information for failed delete operations
    /// </summary>
    public class BulkDeleteErrorDto
    {
        /// <summary>
        /// ID that failed to delete
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Error code (NotFound, Conflict, etc.)
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;
    }


}
