using OpenAutomate.Core.Dto.Schedule;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service interface for managing Quartz.NET jobs for schedules
    /// </summary>
    public interface IQuartzScheduleManager
    {
        /// <summary>
        /// Creates a new Quartz job and trigger for a schedule
        /// </summary>
        /// <param name="schedule">Schedule details</param>
        /// <returns>Task representing the async operation</returns>
        Task CreateJobAsync(ScheduleResponseDto schedule);

        /// <summary>
        /// Updates an existing Quartz job and trigger for a schedule
        /// </summary>
        /// <param name="schedule">Updated schedule details</param>
        /// <returns>Task representing the async operation</returns>
        Task UpdateJobAsync(ScheduleResponseDto schedule);

        /// <summary>
        /// Deletes a Quartz job and trigger for a schedule
        /// </summary>
        /// <param name="scheduleId">Schedule ID</param>
        /// <returns>Task representing the async operation</returns>
        Task DeleteJobAsync(Guid scheduleId);

        /// <summary>
        /// Pauses a Quartz job for a schedule
        /// </summary>
        /// <param name="scheduleId">Schedule ID</param>
        /// <returns>Task representing the async operation</returns>
        Task PauseJobAsync(Guid scheduleId);

        /// <summary>
        /// Resumes a Quartz job for a schedule
        /// </summary>
        /// <param name="scheduleId">Schedule ID</param>
        /// <returns>Task representing the async operation</returns>
        Task ResumeJobAsync(Guid scheduleId);

        /// <summary>
        /// Checks if a Quartz job exists for a schedule
        /// </summary>
        /// <param name="scheduleId">Schedule ID</param>
        /// <returns>True if job exists, false otherwise</returns>
        Task<bool> JobExistsAsync(Guid scheduleId);

        /// <summary>
        /// Checks if a Quartz job is paused for a schedule
        /// </summary>
        /// <param name="scheduleId">Schedule ID</param>
        /// <returns>True if job is paused, false otherwise</returns>
        Task<bool> IsJobPausedAsync(Guid scheduleId);

        /// <summary>
        /// Gets detailed status information about a scheduled job
        /// </summary>
        /// <param name="scheduleId">Schedule ID</param>
        /// <returns>Job status information or null if job doesn't exist</returns>
        Task<object?> GetJobStatusAsync(Guid scheduleId);

        /// <summary>
        /// Manually triggers a job for a schedule
        /// </summary>
        /// <param name="scheduleId">Schedule ID</param>
        /// <returns>Task representing the async operation</returns>
        Task TriggerJobAsync(Guid scheduleId);
    }
} 