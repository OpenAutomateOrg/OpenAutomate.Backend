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
        private readonly ILogStorageService _logStorageService;
        private readonly IHubContext<BotAgentHub> _hubContext;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<ExecutionController> _logger;

        // Standardized log message templates
        private static class LogMessages
        {
            public const string ExecutionTriggered = "Execution {ExecutionId} triggered for bot agent {BotAgentId}";
            public const string LogUploadStarted = "Log upload started for execution {ExecutionId}";
            public const string LogUploadCompleted = "Log upload completed for execution {ExecutionId}";
            public const string LogUploadFailed = "Log upload failed for execution {ExecutionId}";
            public const string LogDownloadRequested = "Log download requested for execution {ExecutionId}";
            public const string LogDownloadUrlGenerated = "Log download URL generated for execution {ExecutionId}";
            public const string LogDownloadFailed = "Log download failed for execution {ExecutionId}";
        }

        /// <summary>
        /// Initializes a new instance of the ExecutionController
        /// </summary>
        public ExecutionController(
            IExecutionService executionService,
            IBotAgentService botAgentService,
            IAutomationPackageService packageService,
            ILogStorageService logStorageService,
            IHubContext<BotAgentHub> hubContext,
            ITenantContext tenantContext,
            ILogger<ExecutionController> logger)
        {
            _executionService = executionService;
            _botAgentService = botAgentService;
            _packageService = packageService;
            _logStorageService = logStorageService;
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

                try
                {
                    await _hubContext.Clients.Group($"bot-{dto.BotAgentId}")
                        .SendAsync("ReceiveCommand", "ExecutePackage", commandPayload);
                }
                catch (Exception signalREx)
                {
                    _logger.LogWarning(signalREx, "Failed to send SignalR command to bot agent {BotAgentId}", dto.BotAgentId);
                }

                _logger.LogInformation(LogMessages.ExecutionTriggered, execution.Id, dto.BotAgentId);

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
        /// Uploads a log file for a specific execution
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <param name="logFile">Log file to upload</param>
        /// <returns>Success response</returns>
        [HttpPost("{id}/logs")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous] // Bot agents use machine key authentication
        public async Task<ActionResult> UploadExecutionLogs(Guid id, IFormFile logFile)
        {
            try
            {
                _logger.LogInformation(LogMessages.LogUploadStarted, id);

                if (logFile == null || logFile.Length == 0)
                    return BadRequest("Log file is required");

                // Authenticate using machine key
                var machineKey = Request.Headers["X-Machine-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(machineKey))
                {
                    _logger.LogWarning("Log upload attempted without machine key for execution {ExecutionId}", id);
                    return Unauthorized("Machine key is required");
                }

                // Verify machine key is valid for current tenant
                var botAgents = await _botAgentService.GetAllBotAgentsAsync();
                var botAgent = botAgents.FirstOrDefault(ba => ba.MachineKey == machineKey);
                
                if (botAgent == null)
                {
                    _logger.LogWarning("Log upload attempted with invalid machine key for execution {ExecutionId}", id);
                    return Unauthorized("Invalid machine key");
                }

                // Validate execution exists and belongs to the bot agent
                var execution = await _executionService.GetExecutionByIdAsync(id);
                if (execution == null)
                    return NotFound("Execution not found");

                if (execution.BotAgentId != botAgent.Id)
                {
                    _logger.LogWarning("Log upload attempted for execution {ExecutionId} by unauthorized bot agent {BotAgentId}", 
                        id, botAgent.Id);
                    return Forbid("Bot agent not authorized for this execution");
                }

                // Generate S3 object key for the log file
                var objectKey = $"execution_{id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.log";

                // Upload log file to S3
                using var stream = logFile.OpenReadStream();
                var s3Key = await _logStorageService.UploadLogAsync(stream, objectKey, "text/plain");

                // Update execution record with S3 path
                await _executionService.UpdateExecutionLogPathAsync(id, s3Key);

                _logger.LogInformation(LogMessages.LogUploadCompleted, id);
                return Ok(new { message = "Log file uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.LogUploadFailed, id);
                return StatusCode(500, "Failed to upload log file");
            }
        }

        /// <summary>
        /// Gets a secure download URL for execution logs
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <returns>Download URL</returns>
        [HttpGet("{id}/logs/download")]
        [RequirePermission(Resources.ExecutionResource, Permissions.View)]
        public async Task<ActionResult> GetExecutionLogDownloadUrl(Guid id)
        {
            try
            {
                _logger.LogInformation(LogMessages.LogDownloadRequested, id);

                // Get execution record
                var execution = await _executionService.GetExecutionByIdAsync(id);
                if (execution == null)
                    return NotFound("Execution not found");

                if (string.IsNullOrEmpty(execution.LogS3Path))
                    return NotFound("No log file available for this execution");

                // Generate pre-signed URL (valid for 1 hour)
                var downloadUrl = await _logStorageService.GetLogDownloadUrlAsync(
                    execution.LogS3Path, 
                    TimeSpan.FromHours(1));

                _logger.LogInformation(LogMessages.LogDownloadUrlGenerated, id);
                return Ok(new { downloadUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.LogDownloadFailed, id);
                return StatusCode(500, "Failed to generate download URL");
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
                try
                {
                    await _hubContext.Clients.All.SendAsync("ExecutionStatusUpdate", MapToResponseDto(execution));
                }
                catch (Exception signalREx)
                {
                    _logger.LogWarning(signalREx, "Failed to broadcast execution status update via SignalR for execution {ExecutionId}", id);
                }

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
                try
                {
                    await _hubContext.Clients.Group($"bot-{execution.BotAgentId}")
                        .SendAsync("ReceiveCommand", "CancelExecution", new { ExecutionId = id.ToString() });
                }
                catch (Exception signalREx)
                {
                    _logger.LogWarning(signalREx, "Failed to send cancel command via SignalR to bot agent {BotAgentId}", execution.BotAgentId);
                }

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
                HasLogs = !string.IsNullOrEmpty(execution.LogS3Path),
                BotAgentName = execution.BotAgent?.Name,
                PackageName = execution.Package?.Name,
                PackageVersion = execution.Package?.Versions?.FirstOrDefault()?.VersionNumber
            };
        }
    }
} 