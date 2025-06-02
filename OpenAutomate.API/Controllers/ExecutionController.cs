using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Execution;
using OpenAutomate.Core.IServices;
using OpenAutomate.API.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for managing bot executions
    /// </summary>
    [ApiController]
    [Route("{tenant}/api/executions")]
    [Authorize]
    public class ExecutionController : ControllerBase
    {
        private readonly IExecutionService _executionService;
        private readonly IBotAgentService _botAgentService;
        private readonly IAutomationPackageService _packageService;
        private readonly IHubContext<BotAgentHub> _hubContext;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<ExecutionController> _logger;

        /// <summary>
        /// Initializes a new instance of the ExecutionController
        /// </summary>
        public ExecutionController(
            IExecutionService executionService,
            IBotAgentService botAgentService,
            IAutomationPackageService packageService,
            IHubContext<BotAgentHub> hubContext,
            ITenantContext tenantContext,
            ILogger<ExecutionController> logger)
        {
            _executionService = executionService;
            _botAgentService = botAgentService;
            _packageService = packageService;
            _hubContext = hubContext;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        /// <summary>
        /// Triggers a new execution
        /// </summary>
        /// <param name="dto">Execution trigger data</param>
        /// <returns>Created execution response</returns>
        [HttpPost("trigger")]
        [RequirePermission(Resources.ExecutionResource, Permissions.Create)]
        public async Task<ActionResult<ExecutionResponseDto>> TriggerExecution([FromBody] TriggerExecutionDto dto)
        {
            try
            {
                // Validate bot agent exists and is available
                var botAgent = await _botAgentService.GetBotAgentByIdAsync(dto.BotAgentId);
                if (botAgent == null)
                    return NotFound("Bot agent not found");

                if (botAgent.Status != "Available")
                    return BadRequest($"Bot agent is not available (Status: {botAgent.Status})");

                // Validate package exists
                var package = await _packageService.GetPackageByIdAsync(dto.PackageId);
                if (package == null)
                    return NotFound("Package not found");

                // Create execution record
                var execution = await _executionService.CreateExecutionAsync(new CreateExecutionDto
                {
                    BotAgentId = dto.BotAgentId,
                    PackageId = dto.PackageId
                });

                // Send command to bot agent via SignalR
                var commandPayload = new
                {
                    ExecutionId = execution.Id.ToString(),
                    PackageId = dto.PackageId.ToString(),
                    PackageName = dto.PackageName,
                    Version = dto.Version,
                    TenantSlug = _tenantContext.CurrentTenantSlug
                };

                await _hubContext.Clients.Group($"bot-{dto.BotAgentId}")
                    .SendAsync("ReceiveCommand", "ExecutePackage", commandPayload);

                _logger.LogInformation("Execution {ExecutionId} triggered for bot agent {BotAgentId}", 
                    execution.Id, dto.BotAgentId);

                var responseDto = MapToResponseDto(execution);
                responseDto.BotAgentName = botAgent.Name;
                responseDto.PackageName = package.Name;
                responseDto.PackageVersion = dto.Version;

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering execution");
                return StatusCode(500, "Failed to trigger execution");
            }
        }

        /// <summary>
        /// Gets all executions for the current tenant
        /// </summary>
        /// <returns>List of executions</returns>
        [HttpGet]
        [RequirePermission(Resources.ExecutionResource, Permissions.View)]
        public async Task<ActionResult<IEnumerable<ExecutionResponseDto>>> GetAllExecutions()
        {
            try
            {
                var executions = await _executionService.GetAllExecutionsAsync();
                var responseDtos = executions.Select(MapToResponseDto);
                return Ok(responseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all executions");
                return StatusCode(500, "Failed to get executions");
            }
        }

        /// <summary>
        /// Gets an execution by ID
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <returns>Execution details</returns>
        [HttpGet("{id}")]
        [RequirePermission(Resources.ExecutionResource, Permissions.View)]
        public async Task<ActionResult<ExecutionResponseDto>> GetExecutionById(Guid id)
        {
            try
            {
                var execution = await _executionService.GetExecutionByIdAsync(id);
                if (execution == null)
                    return NotFound("Execution not found");

                return Ok(MapToResponseDto(execution));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution {ExecutionId}", id);
                return StatusCode(500, "Failed to get execution");
            }
        }

        /// <summary>
        /// Updates execution status (typically called by bot agents)
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <param name="updateDto">Status update data</param>
        /// <returns>Updated execution</returns>
        [HttpPut("{id}/status")]
        [RequirePermission(Resources.ExecutionResource, Permissions.Update)]
        public async Task<ActionResult<ExecutionResponseDto>> UpdateExecutionStatus(
            Guid id, 
            [FromBody] UpdateExecutionStatusDto updateDto)
        {
            try
            {
                var execution = await _executionService.UpdateExecutionStatusAsync(
                    id, 
                    updateDto.Status, 
                    updateDto.ErrorMessage, 
                    updateDto.LogOutput);

                if (execution == null)
                    return NotFound("Execution not found");

                // Broadcast status update via SignalR to connected clients
                await _hubContext.Clients.All.SendAsync("ExecutionStatusUpdate", MapToResponseDto(execution));

                return Ok(MapToResponseDto(execution));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating execution status {ExecutionId}", id);
                return StatusCode(500, "Failed to update execution status");
            }
        }

        /// <summary>
        /// Cancels an execution
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <returns>Updated execution</returns>
        [HttpPost("{id}/cancel")]
        [RequirePermission(Resources.ExecutionResource, Permissions.Update)]
        public async Task<ActionResult<ExecutionResponseDto>> CancelExecution(Guid id)
        {
            try
            {
                var execution = await _executionService.CancelExecutionAsync(id);
                if (execution == null)
                    return NotFound("Execution not found");

                // Send cancel command to bot agent
                await _hubContext.Clients.Group($"bot-{execution.BotAgentId}")
                    .SendAsync("ReceiveCommand", "CancelExecution", new { ExecutionId = id.ToString() });

                return Ok(MapToResponseDto(execution));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling execution {ExecutionId}", id);
                return StatusCode(500, "Failed to cancel execution");
            }
        }

        /// <summary>
        /// Maps execution entity to response DTO
        /// </summary>
        private static ExecutionResponseDto MapToResponseDto(Core.Domain.Entities.Execution execution)
        {
            return new ExecutionResponseDto
            {
                Id = execution.Id,
                BotAgentId = execution.BotAgentId,
                PackageId = execution.PackageId,
                Status = execution.Status,
                StartTime = execution.StartTime,
                EndTime = execution.EndTime,
                ErrorMessage = execution.ErrorMessage,
                LogOutput = execution.LogOutput,
                BotAgentName = execution.BotAgent?.Name,
                PackageName = execution.Package?.Name,
                PackageVersion = execution.Package?.Versions?.FirstOrDefault()?.VersionNumber
            };
        }
    }
} 