using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.Dto.Common;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service interface for managing schedules
    /// </summary>
    public interface IScheduleService
    {
        /// <summary>
        /// Creates a new schedule
        /// </summary>
        /// <param name="dto">Schedule creation data</param>
        /// <returns>Created schedule response</returns>
        Task<ScheduleResponseDto> CreateScheduleAsync(CreateScheduleDto dto);

        /// <summary>
        /// Gets a schedule by its ID
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Schedule response or null if not found</returns>
        Task<ScheduleResponseDto?> GetScheduleByIdAsync(Guid id);

        /// <summary>
        /// Gets all schedules for the current tenant
        /// </summary>
        /// <returns>Collection of schedules</returns>
        Task<IEnumerable<ScheduleResponseDto>> GetAllSchedulesAsync();

        /// <summary>
        /// Updates an existing schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <param name="dto">Schedule update data</param>
        /// <returns>Updated schedule response or null if not found</returns>
        Task<ScheduleResponseDto?> UpdateScheduleAsync(Guid id, UpdateScheduleDto dto);

        /// <summary>
        /// Deletes a schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteScheduleAsync(Guid id);

        /// <summary>
        /// Enables a schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Updated schedule response or null if not found</returns>
        Task<ScheduleResponseDto?> EnableScheduleAsync(Guid id);

        /// <summary>
        /// Disables a schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Updated schedule response or null if not found</returns>
        Task<ScheduleResponseDto?> DisableScheduleAsync(Guid id);

        /// <summary>
        /// Calculates the next run time for a schedule
        /// </summary>
        /// <param name="schedule">Schedule to calculate next run for</param>
        /// <returns>Next run time or null if schedule is disabled or invalid</returns>
        DateTime? CalculateNextRunTime(ScheduleResponseDto schedule);

        /// <summary>
        /// Calculates multiple upcoming run times for a schedule
        /// </summary>
        /// <param name="schedule">Schedule to calculate upcoming runs for</param>
        /// <param name="count">Number of upcoming run times to calculate</param>
        /// <returns>List of upcoming run times</returns>
        List<DateTime> CalculateUpcomingRunTimes(ScheduleResponseDto schedule, int count = 5);

        
        /// <summary>
        /// Deletes multiple Schedules in a single operation
        /// </summary>
        /// <param name="ids">List of Schedule IDs to delete</param>
        /// <returns>Result of the bulk delete operation</returns>
        Task<BulkDeleteResultDto> BulkDeleteSchedulesAsync(List<Guid> ids);


        /// <summary>
        /// Recalculates next run time for an existing schedule and updates Quartz job
        /// </summary>
        /// <param name="scheduleId">Schedule ID to recalculate</param>
        /// <returns>Updated schedule response</returns>
        Task<ScheduleResponseDto?> RecalculateScheduleAsync(Guid scheduleId);

    }
} 