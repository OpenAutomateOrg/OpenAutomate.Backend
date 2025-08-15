using System.Collections.Generic;

namespace OpenAutomate.Core.Dto.Asset
{
    /// <summary>
    /// DTO representing the result of a CSV import operation
    /// </summary>
    public class CsvImportResultDto
    {
        /// <summary>
        /// Total number of rows processed
        /// </summary>
        public int TotalRows { get; set; }
        
        /// <summary>
        /// Number of assets successfully imported
        /// </summary>
        public int SuccessfulImports { get; set; }
        
        /// <summary>
        /// Number of assets that failed to import
        /// </summary>
        public int FailedImports { get; set; }
        
        /// <summary>
        /// List of errors that occurred during import
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// List of warnings that occurred during import
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
