using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenAutomate.Core.Dto.Schedule;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for validating schedules and detecting conflicts
    /// </summary>
    public interface IScheduleValidationService
    {
        /// <summary>
        /// Validates a schedule creation request
        /// </summary>
        /// <param name="dto">The schedule creation DTO</param>
        /// <returns>Validation result with any errors</returns>
        Task<ValidationResult> ValidateScheduleAsync(CreateScheduleDto dto);
        
        /// <summary>
        /// Validates a one-time schedule creation request
        /// </summary>
        /// <param name="dto">The one-time schedule creation DTO</param>
        /// <returns>Validation result with any errors</returns>
        Task<ValidationResult> ValidateOneTimeScheduleAsync(CreateOneTimeScheduleDto dto);
        
        /// <summary>
        /// Detects scheduling conflicts for a package
        /// </summary>
        /// <param name="packageId">The package ID</param>
        /// <param name="startTime">The proposed start time</param>
        /// <param name="endTime">The proposed end time (optional)</param>
        /// <returns>List of detected conflicts</returns>
        Task<List<ScheduleConflict>> DetectConflictsAsync(Guid packageId, DateTime startTime, DateTime? endTime = null);
        
        /// <summary>
        /// Validates a cron expression
        /// </summary>
        /// <param name="cronExpression">The cron expression to validate</param>
        /// <returns>True if valid</returns>
        bool ValidateCronExpression(string cronExpression);
        
        /// <summary>
        /// Validates package access for the current tenant
        /// </summary>
        /// <param name="packageId">The package ID to validate</param>
        /// <returns>True if the current tenant has access to the package</returns>
        Task<bool> ValidatePackageAccessAsync(Guid packageId);
        
        /// <summary>
        /// Validates execution date for one-time schedules
        /// </summary>
        /// <param name="executionDate">The proposed execution date</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateExecutionDate(DateTime executionDate);
        

    }
    
    /// <summary>
    /// Represents a validation result with errors
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public Dictionary<string, List<string>> Errors { get; set; } = new();
        
        public void AddError(string field, string message)
        {
            if (!Errors.ContainsKey(field))
            {
                Errors[field] = new List<string>();
            }
            Errors[field].Add(message);
        }
        
        public void AddErrors(string field, IEnumerable<string> messages)
        {
            foreach (var message in messages)
            {
                AddError(field, message);
            }
        }
    }
    
    /// <summary>
    /// Represents a scheduling conflict
    /// </summary>
    public class ScheduleConflict
    {
        public Guid ScheduleId { get; set; }
        public string ScheduleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ConflictTime { get; set; }
        public ConflictType Type { get; set; }
    }
    
    /// <summary>
    /// Types of scheduling conflicts
    /// </summary>
    public enum ConflictType
    {
        TimeOverlap,
        ResourceUnavailable,
        AgentBusy,
        PackageConflict
    }
} 