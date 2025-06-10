using Quartz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for managing Quartz.NET scheduler and job operations
    /// </summary>
    public interface IJobManagementService
    {
        /// <summary>
        /// Starts the Quartz scheduler
        /// </summary>
        /// <returns>True if started successfully</returns>
        Task<bool> StartSchedulerAsync();
        
        /// <summary>
        /// Stops the Quartz scheduler
        /// </summary>
        /// <returns>True if stopped successfully</returns>
        Task<bool> StopSchedulerAsync();
        
        /// <summary>
        /// Pauses a specific job
        /// </summary>
        /// <param name="jobKey">The job key</param>
        /// <param name="groupName">The job group name</param>
        /// <returns>True if paused successfully</returns>
        Task<bool> PauseJobAsync(string jobKey, string groupName = "DEFAULT");
        
        /// <summary>
        /// Resumes a paused job
        /// </summary>
        /// <param name="jobKey">The job key</param>
        /// <param name="groupName">The job group name</param>
        /// <returns>True if resumed successfully</returns>
        Task<bool> ResumeJobAsync(string jobKey, string groupName = "DEFAULT");
        
        /// <summary>
        /// Gets all currently executing jobs
        /// </summary>
        /// <returns>List of currently executing job contexts</returns>
        Task<IReadOnlyCollection<IJobExecutionContext>> GetRunningJobsAsync();
        
        /// <summary>
        /// Gets running jobs for a specific tenant
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <returns>List of running jobs for the tenant</returns>
        Task<List<IJobExecutionContext>> GetTenantRunningJobsAsync(Guid tenantId);
        
        /// <summary>
        /// Deletes a job and its triggers
        /// </summary>
        /// <param name="jobKey">The job key</param>
        /// <param name="groupName">The job group name</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteJobAsync(string jobKey, string groupName = "DEFAULT");
        
        /// <summary>
        /// Checks if the scheduler is running
        /// </summary>
        /// <returns>True if scheduler is running</returns>
        Task<bool> IsSchedulerRunningAsync();
        
        /// <summary>
        /// Gets job details for a specific job
        /// </summary>
        /// <param name="jobKey">The job key</param>
        /// <param name="groupName">The job group name</param>
        /// <returns>Job detail or null if not found</returns>
        Task<IJobDetail?> GetJobDetailAsync(string jobKey, string groupName = "DEFAULT");
        
        /// <summary>
        /// Gets triggers for a specific job
        /// </summary>
        /// <param name="jobKey">The job key</param>
        /// <param name="groupName">The job group name</param>
        /// <returns>List of triggers for the job</returns>
        Task<IReadOnlyCollection<ITrigger>> GetTriggersForJobAsync(string jobKey, string groupName = "DEFAULT");
        
        /// <summary>
        /// Validates tenant access to a job
        /// </summary>
        /// <param name="jobKey">The job key</param>
        /// <param name="groupName">The job group name</param>
        /// <returns>True if current tenant has access to the job</returns>
        Task<bool> ValidateTenantJobAccessAsync(string jobKey, string groupName = "DEFAULT");
    }
} 