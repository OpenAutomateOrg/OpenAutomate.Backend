using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.Dto.Common;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Exceptions;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for managing automation schedules
    /// </summary>
    [ApiController]
    [Route("{tenant}/api/schedules")]
    [Authorize]
    public class SchedulesController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;
        private readonly ILogger<SchedulesController> _logger;

        public SchedulesController(
            IScheduleService scheduleService,
            ILogger<SchedulesController> logger)
        {
            _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new one-time schedule
        /// </summary>
        /// <param name="dto">One-time schedule creation data</param>
        /// <returns>Created schedule response</returns>
        [HttpPost("one-time")]
        [RequirePermission(Resources.ScheduleResource, Permissions.Create)]
        public async Task<ActionResult<ScheduleResponseDto>> CreateOneTimeSchedule([FromBody] CreateOneTimeScheduleDto dto)
        {
            try
            {
                var schedule = await _scheduleService.CreateOneTimeScheduleAsync(dto);
                return CreatedAtAction(nameof(GetScheduleById), new { id = schedule.Id }, schedule);
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating one-time schedule");
                return StatusCode(500, "Failed to create one-time schedule");
            }
        }

        /// <summary>
        /// Creates a new recurring schedule
        /// </summary>
        /// <param name="dto">Schedule creation data</param>
        /// <returns>Created schedule response</returns>
        [HttpPost]
        [RequirePermission(Resources.ScheduleResource, Permissions.Create)]
        public async Task<ActionResult<ScheduleResponseDto>> CreateSchedule([FromBody] CreateScheduleDto dto)
        {
            try
            {
                var schedule = await _scheduleService.CreateScheduleAsync(dto);
                return CreatedAtAction(nameof(GetScheduleById), new { id = schedule.Id }, schedule);
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule");
                return StatusCode(500, "Failed to create schedule");
            }
        }

        /// <summary>
        /// Gets a schedule by ID
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Schedule details</returns>
        [HttpGet("{id}")]
        [RequirePermission(Resources.ScheduleResource, Permissions.View)]
        public async Task<ActionResult<ScheduleResponseDto>> GetScheduleById(Guid id)
        {
            try
            {
                var schedule = await _scheduleService.GetScheduleByIdAsync(id);
                if (schedule == null)
                {
                    return NotFound("Schedule not found");
                }

                return Ok(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule {ScheduleId}", id);
                return StatusCode(500, "Failed to get schedule");
            }
        }

        /// <summary>
        /// Gets all schedules for the current tenant with pagination and filtering
        /// </summary>
        /// <param name="parameters">Query parameters for filtering and pagination</param>
        /// <returns>Paged list of schedules</returns>
        [HttpGet]
        [RequirePermission(Resources.ScheduleResource, Permissions.View)]
        public async Task<ActionResult<PagedResult<ScheduleResponseDto>>> GetSchedules([FromQuery] ScheduleQueryParameters parameters)
        {
            try
            {
                var schedules = await _scheduleService.GetTenantSchedulesAsync(parameters);
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedules");
                return StatusCode(500, "Failed to get schedules");
            }
        }

        /// <summary>
        /// Updates an existing schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <param name="dto">Schedule update data</param>
        /// <returns>Updated schedule response</returns>
        [HttpPut("{id}")]
        [RequirePermission(Resources.ScheduleResource, Permissions.Update)]
        public async Task<ActionResult<ScheduleResponseDto>> UpdateSchedule(Guid id, [FromBody] UpdateScheduleDto dto)
        {
            try
            {
                var schedule = await _scheduleService.UpdateScheduleAsync(id, dto);
                if (schedule == null)
                {
                    return NotFound("Schedule not found");
                }

                return Ok(schedule);
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schedule {ScheduleId}", id);
                return StatusCode(500, "Failed to update schedule");
            }
        }

        /// <summary>
        /// Deletes a schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [RequirePermission(Resources.ScheduleResource, Permissions.Delete)]
        public async Task<ActionResult> DeleteSchedule(Guid id)
        {
            try
            {
                var success = await _scheduleService.DeleteScheduleAsync(id);
                if (!success)
                {
                    return NotFound("Schedule not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting schedule {ScheduleId}", id);
                return StatusCode(500, "Failed to delete schedule");
            }
        }

        /// <summary>
        /// Pauses a schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/pause")]
        [RequirePermission(Resources.ScheduleResource, Permissions.Update)]
        public async Task<ActionResult> PauseSchedule(Guid id)
        {
            try
            {
                var success = await _scheduleService.PauseScheduleAsync(id);
                if (!success)
                {
                    return NotFound("Schedule not found");
                }

                return Ok(new { message = "Schedule paused successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing schedule {ScheduleId}", id);
                return StatusCode(500, "Failed to pause schedule");
            }
        }

        /// <summary>
        /// Resumes a paused schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/resume")]
        [RequirePermission(Resources.ScheduleResource, Permissions.Update)]
        public async Task<ActionResult> ResumeSchedule(Guid id)
        {
            try
            {
                var success = await _scheduleService.ResumeScheduleAsync(id);
                if (!success)
                {
                    return NotFound("Schedule not found");
                }

                return Ok(new { message = "Schedule resumed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming schedule {ScheduleId}", id);
                return StatusCode(500, "Failed to resume schedule");
            }
        }

        /// <summary>
        /// Gets all active schedules for the current tenant
        /// </summary>
        /// <returns>List of active schedules</returns>
        [HttpGet("active")]
        [RequirePermission(Resources.ScheduleResource, Permissions.View)]
        public async Task<ActionResult<System.Collections.Generic.List<ScheduleResponseDto>>> GetActiveSchedules()
        {
            try
            {
                var schedules = await _scheduleService.GetActiveSchedulesForTenantAsync();
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active schedules");
                return StatusCode(500, "Failed to get active schedules");
            }
        }
    }
} 