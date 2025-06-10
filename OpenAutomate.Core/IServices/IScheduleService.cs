using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.Common;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for managing automation schedules
    /// </summary>
    public interface IScheduleService
    {
        /// <summary>
        /// Creates a new one-time schedule
        /// </summary>
        /// <param name="dto">The one-time schedule creation DTO</param>
        /// <returns>The created schedule response</returns>
        Task<ScheduleResponseDto> CreateOneTimeScheduleAsync(CreateOneTimeScheduleDto dto);
        
        /// <summary>
        /// Creates a new recurring schedule
        /// </summary>
        /// <param name="dto">The schedule creation DTO</param>
        /// <returns>The created schedule response</returns>
        Task<ScheduleResponseDto> CreateScheduleAsync(CreateScheduleDto dto);
        
        /// <summary>
        /// Gets a schedule by ID for the current tenant
        /// </summary>
        /// <param name="id">The schedule ID</param>
        /// <returns>The schedule if found, null otherwise</returns>
        Task<ScheduleResponseDto?> GetScheduleByIdAsync(Guid id);
        
        /// <summary>
        /// Gets all schedules for the current tenant with pagination and filtering
        /// </summary>
        /// <param name="parameters">Query parameters for filtering and pagination</param>
        /// <returns>Paged result of schedules</returns>
        Task<PagedResult<ScheduleResponseDto>> GetTenantSchedulesAsync(ScheduleQueryParameters parameters);
        
        /// <summary>
        /// Updates an existing schedule
        /// </summary>
        /// <param name="id">The schedule ID</param>
        /// <param name="dto">The update DTO</param>
        /// <returns>The updated schedule if successful, null otherwise</returns>
        Task<ScheduleResponseDto?> UpdateScheduleAsync(Guid id, UpdateScheduleDto dto);
        
        /// <summary>
        /// Deletes a schedule
        /// </summary>
        /// <param name="id">The schedule ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteScheduleAsync(Guid id);
        
        /// <summary>
        /// Pauses a schedule
        /// </summary>
        /// <param name="id">The schedule ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> PauseScheduleAsync(Guid id);
        
        /// <summary>
        /// Resumes a paused schedule
        /// </summary>
        /// <param name="id">The schedule ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> ResumeScheduleAsync(Guid id);
        
        /// <summary>
        /// Gets active schedules for the current tenant
        /// </summary>
        /// <returns>List of active schedules</returns>
        Task<List<ScheduleResponseDto>> GetActiveSchedulesForTenantAsync();
    }
} 