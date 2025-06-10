using System;
using System.Collections.Generic;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for handling cron expression operations
    /// </summary>
    public interface ICronExpressionService
    {
        /// <summary>
        /// Validates if a cron expression is valid
        /// </summary>
        /// <param name="cronExpression">The cron expression to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        bool IsValid(string cronExpression);
        
        /// <summary>
        /// Gets a human-readable description of the cron expression
        /// </summary>
        /// <param name="cronExpression">The cron expression</param>
        /// <returns>Human-readable description</returns>
        string GetDescription(string cronExpression);
        
        /// <summary>
        /// Gets the next execution times for a cron expression
        /// </summary>
        /// <param name="cronExpression">The cron expression</param>
        /// <param name="count">Number of next executions to return</param>
        /// <param name="startDate">Start date for calculation (defaults to now)</param>
        /// <returns>List of next execution times</returns>
        List<DateTime> GetNextExecutions(string cronExpression, int count = 10, DateTime? startDate = null);
        
        /// <summary>
        /// Gets the next single execution time for a cron expression
        /// </summary>
        /// <param name="cronExpression">The cron expression</param>
        /// <param name="startDate">Start date for calculation (defaults to now)</param>
        /// <returns>Next execution time or null if no more executions</returns>
        DateTime? GetNextExecution(string cronExpression, DateTime? startDate = null);
        
        /// <summary>
        /// Parses a cron expression into its components
        /// </summary>
        /// <param name="cronExpression">The cron expression</param>
        /// <returns>Cron expression info with parsed components</returns>
        CronExpressionInfo ParseExpression(string cronExpression);
    }
    
    /// <summary>
    /// Information about a parsed cron expression
    /// </summary>
    public class CronExpressionInfo
    {
        public string Second { get; set; } = string.Empty;
        public string Minute { get; set; } = string.Empty;
        public string Hour { get; set; } = string.Empty;
        public string DayOfMonth { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public string DayOfWeek { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public string Description { get; set; } = string.Empty;
    }
} 