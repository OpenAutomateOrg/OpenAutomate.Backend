using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for schedule management operations
    /// </summary>
    [ApiController]
    [Route("{tenant}/api/schedules")]
    [Authorize]
    public class ScheduleController : CustomControllerBase
    {
        private readonly IScheduleService _scheduleService;
        private readonly ILogger<ScheduleController> _logger;

        /// <summary>
        /// Initializes a new instance of the ScheduleController
        /// </summary>
        public ScheduleController(
            IScheduleService scheduleService,
            ILogger<ScheduleController> logger)
        {
            _scheduleService = scheduleService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new schedule
        /// </summary>
        /// <param name="dto">Schedule creation data</param>
        /// <returns>Created schedule response</returns>
        [HttpPost]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.ScheduleResource, Permissions.Create)]
        public async Task<ActionResult<ScheduleResponseDto>> CreateSchedule([FromBody] CreateScheduleDto dto)
        {
            try
            {
                // Set the current user as the creator
                dto.CreatedBy = GetCurrentUserId();
                
                var schedule = await _scheduleService.CreateScheduleAsync(dto);
                
                // Get the tenant from the route data
                var tenant = RouteData.Values["tenant"]?.ToString();
                
                return CreatedAtAction(
                    nameof(GetScheduleById), 
                    new { tenant = tenant, id = schedule.Id }, 
                    schedule);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when creating schedule: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating schedule: {Message}", ex.Message);
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule");
                return StatusCode(500, new { error = "Failed to create schedule" });
            }
        }

        /// <summary>
        /// Gets a schedule by ID
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Schedule response</returns>
        [HttpGet("{id}")]
        [RequireSubscription(SubscriptionOperationType.Read)]
        [RequirePermission(Resources.ScheduleResource, Permissions.View)]
        public async Task<ActionResult<ScheduleResponseDto>> GetScheduleById(Guid id)
        {
            try
            {
                var schedule = await _scheduleService.GetScheduleByIdAsync(id);
                if (schedule == null)
                    return NotFound(new { error = "Schedule not found" });

                return Ok(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule {ScheduleId}", id);
                return StatusCode(500, new { error = "Failed to get schedule" });
            }
        }

        /// <summary>
        /// Gets all schedules for the current tenant
        /// </summary>
        /// <returns>Collection of schedules</returns>
        [HttpGet]
        [RequireSubscription(SubscriptionOperationType.Read)]
        [RequirePermission(Resources.ScheduleResource, Permissions.View)]
        public async Task<ActionResult<IEnumerable<ScheduleResponseDto>>> GetAllSchedules()
        {
            try
            {
                var schedules = await _scheduleService.GetAllSchedulesAsync();
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all schedules");
                return StatusCode(500, new { error = "Failed to get schedules" });
            }
        }

        /// <summary>
        /// Updates an existing schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <param name="dto">Schedule update data</param>
        /// <returns>Updated schedule response</returns>
        [HttpPut("{id}")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.ScheduleResource, Permissions.Update)]
        public async Task<ActionResult<ScheduleResponseDto>> UpdateSchedule(Guid id, [FromBody] UpdateScheduleDto dto)
        {
            try
            {
                var schedule = await _scheduleService.UpdateScheduleAsync(id, dto);
                if (schedule == null)
                    return NotFound(new { error = "Schedule not found" });

                return Ok(schedule);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when updating schedule {ScheduleId}: {Message}", id, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when updating schedule {ScheduleId}: {Message}", id, ex.Message);
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schedule {ScheduleId}", id);
                return StatusCode(500, new { error = "Failed to update schedule" });
            }
        }

        /// <summary>
        /// Deletes a schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>No content response</returns>
        [HttpDelete("{id}")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.ScheduleResource, Permissions.Delete)]
        public async Task<IActionResult> DeleteSchedule(Guid id)
        {
            try
            {
                var deleted = await _scheduleService.DeleteScheduleAsync(id);
                if (!deleted)
                    return NotFound(new { error = "Schedule not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting schedule {ScheduleId}", id);
                return StatusCode(500, new { error = "Failed to delete schedule" });
            }
        }

        /// <summary>
        /// Enables a schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Updated schedule response</returns>
        [HttpPost("{id}/enable")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.ScheduleResource, Permissions.Update)]
        public async Task<ActionResult<ScheduleResponseDto>> EnableSchedule(Guid id)
        {
            try
            {
                var schedule = await _scheduleService.EnableScheduleAsync(id);
                if (schedule == null)
                    return NotFound(new { error = "Schedule not found" });

                return Ok(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling schedule {ScheduleId}", id);
                return StatusCode(500, new { error = "Failed to enable schedule" });
            }
        }

        /// <summary>
        /// Disables a schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Updated schedule response</returns>
        [HttpPost("{id}/disable")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.ScheduleResource, Permissions.Update)]
        public async Task<ActionResult<ScheduleResponseDto>> DisableSchedule(Guid id)
        {
            try
            {
                var schedule = await _scheduleService.DisableScheduleAsync(id);
                if (schedule == null)
                    return NotFound(new { error = "Schedule not found" });

                return Ok(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling schedule {ScheduleId}", id);
                return StatusCode(500, new { error = "Failed to disable schedule" });
            }
        }

        /// <summary>
        /// Gets upcoming run times for a schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <param name="count">Number of upcoming run times to return (default: 5)</param>
        /// <returns>List of upcoming run times in UTC</returns>
        [HttpGet("{id}/upcoming-runs")]
        [RequireSubscription(SubscriptionOperationType.Read)]
        [RequirePermission(Resources.ScheduleResource, Permissions.View)]
        public async Task<ActionResult<object>> GetUpcomingRunTimes(Guid id, [FromQuery] int count = 5)
        {
            try
            {
                var schedule = await _scheduleService.GetScheduleByIdAsync(id);
                if (schedule == null)
                    return NotFound(new { error = "Schedule not found" });

                var upcomingTimes = _scheduleService.CalculateUpcomingRunTimes(schedule, Math.Min(count, 10)); // Max 10 times
                
                return Ok(new
                {
                    scheduleId = id,
                    scheduleName = schedule.Name,
                    isEnabled = schedule.IsEnabled,
                    timeZone = schedule.TimeZoneId,
                    upcomingRuns = upcomingTimes.Select(time => new
                    {
                        utc = time,
                        local = TimeZoneInfo.ConvertTimeFromUtc(time, GetTimeZoneInfo(schedule.TimeZoneId)),
                        localFormatted = TimeZoneInfo.ConvertTimeFromUtc(time, GetTimeZoneInfo(schedule.TimeZoneId))
                            .ToString("yyyy-MM-dd HH:mm:ss"),
                        timeZone = schedule.TimeZoneId
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming run times for schedule {ScheduleId}", id);
                return StatusCode(500, new { error = "Failed to get upcoming run times" });
            }
        }

        /// <summary>
        /// Recalculates next run time for a schedule with corrected timezone logic
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Updated schedule response</returns>
        [HttpPost("{id}/recalculate")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.ScheduleResource, Permissions.Update)]
        public async Task<ActionResult<ScheduleResponseDto>> RecalculateSchedule(Guid id)
        {
            try
            {
                var schedule = await _scheduleService.RecalculateScheduleAsync(id);
                if (schedule == null)
                    return NotFound(new { error = "Schedule not found" });

                _logger.LogInformation("Successfully recalculated schedule {ScheduleId} with new next run time {NextRunTime}", 
                    id, schedule.NextRunTime);

                return Ok(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating schedule {ScheduleId}", id);
                return StatusCode(500, new { error = "Failed to recalculate schedule" });
            }
        }

        /// <summary>
        /// Gets Quartz job status for debugging schedule execution issues
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Job status information</returns>
        [HttpGet("{id}/job-status")]
        [RequireSubscription(SubscriptionOperationType.Read)]
        [RequirePermission(Resources.ScheduleResource, Permissions.View)]
        public async Task<ActionResult<object>> GetJobStatus(Guid id)
        {
            try
            {
                var quartzManager = HttpContext.RequestServices.GetRequiredService<IQuartzScheduleManager>();
                var status = await quartzManager.GetJobStatusAsync(id);
                
                if (status == null)
                    return NotFound(new { error = "Job status not available" });

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job status for schedule {ScheduleId}", id);
                return StatusCode(500, new { error = "Failed to get job status" });
            }
        }

        /// <summary>
        /// Manually triggers a schedule execution for testing
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Execution result</returns>
        [HttpPost("{id}/manual-trigger")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.ScheduleResource, Permissions.Update)]
        public async Task<IActionResult> ManualTrigger(Guid id)
        {
            try
            {
                var executionTriggerService = HttpContext.RequestServices.GetRequiredService<IExecutionTriggerService>();
                var schedule = await _scheduleService.GetScheduleByIdAsync(id);
                
                if (schedule == null)
                    return NotFound(new { error = "Schedule not found" });

                _logger.LogInformation("Manually triggering execution for schedule {ScheduleId}", id);

                var execution = await executionTriggerService.TriggerScheduledExecutionAsync(
                    schedule.Id,
                    schedule.BotAgentId,
                    schedule.AutomationPackageId,
                    schedule.AutomationPackageName,
                    "latest");

                return Ok(new { 
                    success = true, 
                    executionId = execution.Id,
                    message = "Manual execution triggered successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error manually triggering schedule {ScheduleId}", id);
                return StatusCode(500, new { 
                    error = ex.Message, 
                    details = ex.ToString() 
                });
            }
        }

        private static TimeZoneInfo GetTimeZoneInfo(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
} 