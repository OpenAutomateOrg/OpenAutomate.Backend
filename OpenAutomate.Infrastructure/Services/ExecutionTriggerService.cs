using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Dto.Execution;
using OpenAutomate.Core.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service for triggering executions with full workflow including SignalR communication
    /// </summary>
    public class ExecutionTriggerService : IExecutionTriggerService
    {
        private readonly IExecutionService _executionService;
        private readonly IBotAgentService _botAgentService;
        private readonly IAutomationPackageService _packageService;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<ExecutionTriggerService> _logger;

        // Delegate for SignalR communication - will be injected from API layer
        private readonly Func<Guid, string, object, Task>? _signalRSender;

        public ExecutionTriggerService(
            IExecutionService executionService,
            IBotAgentService botAgentService,
            IAutomationPackageService packageService,
            ITenantContext tenantContext,
            ILogger<ExecutionTriggerService> logger,
            Func<Guid, string, object, Task>? signalRSender = null)
        {
            _executionService = executionService;
            _botAgentService = botAgentService;
            _packageService = packageService;
            _tenantContext = tenantContext;
            _logger = logger;
            _signalRSender = signalRSender;
        }

        public async Task<ExecutionResponseDto> TriggerExecutionAsync(TriggerExecutionDto dto)
        {
            // Validate bot agent exists and is not disconnected
            var botAgent = await _botAgentService.GetBotAgentByIdAsync(dto.BotAgentId);
            if (botAgent == null)
                throw new ArgumentException("Bot agent not found");

            if (botAgent.Status == "Disconnected")
                throw new InvalidOperationException($"Bot agent is disconnected (Status: {botAgent.Status})");

            // Validate package exists
            var package = await _packageService.GetPackageByIdAsync(dto.PackageId);
            if (package == null)
                throw new ArgumentException("Package not found");

            // Create execution record
            var execution = await _executionService.CreateExecutionAsync(new CreateExecutionDto
            {
                BotAgentId = dto.BotAgentId,
                PackageId = dto.PackageId
            });

            // Send command to bot agent via SignalR if available
            if (_signalRSender != null)
            {
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
                    await _signalRSender(dto.BotAgentId, "ExecutePackage", commandPayload);
                    _logger.LogInformation("SignalR command sent to bot agent {BotAgentId} for execution {ExecutionId}", 
                        dto.BotAgentId, execution.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send SignalR command to bot agent {BotAgentId} for execution {ExecutionId}", 
                        dto.BotAgentId, execution.Id);
                }
            }
            else
            {
                _logger.LogWarning("SignalR sender not available - execution {ExecutionId} created but command not sent to bot agent", 
                    execution.Id);
            }

            _logger.LogInformation("Execution {ExecutionId} triggered for bot agent {BotAgentId}", 
                execution.Id, dto.BotAgentId);

            // Map to response DTO
            var responseDto = new ExecutionResponseDto
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
                ScheduleId = execution.ScheduleId,
                BotAgentName = botAgent.Name,
                PackageName = package.Name,
                PackageVersion = dto.Version
            };

            return responseDto;
        }

        public async Task<ExecutionResponseDto> TriggerScheduledExecutionAsync(
            Guid scheduleId,
            Guid botAgentId,
            Guid packageId,
            string packageName,
            string version)
        {
            // Validate bot agent exists and is not disconnected
            var botAgent = await _botAgentService.GetBotAgentByIdAsync(botAgentId);
            if (botAgent == null)
                throw new ArgumentException("Bot agent not found");

            if (botAgent.Status == "Disconnected")
                throw new InvalidOperationException($"Bot agent is disconnected (Status: {botAgent.Status})");

            // Validate package exists
            var package = await _packageService.GetPackageByIdAsync(packageId);
            if (package == null)
                throw new ArgumentException("Package not found");

            // Use latest version if version is not specified or is "latest"
            var actualVersion = version;
            if (string.IsNullOrEmpty(version) || version == "latest")
            {
                actualVersion = package.Versions?.FirstOrDefault()?.VersionNumber ?? "1.0.0";
            }

            // Create execution record with ScheduleId
            var execution = await _executionService.CreateExecutionAsync(new CreateExecutionDto
            {
                BotAgentId = botAgentId,
                PackageId = packageId,
                ScheduleId = scheduleId
            });

            // Send command to bot agent via SignalR if available
            if (_signalRSender != null)
            {
                var commandPayload = new
                {
                    ExecutionId = execution.Id.ToString(),
                    PackageId = packageId.ToString(),
                    PackageName = packageName,
                    Version = actualVersion,
                    TenantSlug = _tenantContext.CurrentTenantSlug
                };

                try
                {
                    await _signalRSender(botAgentId, "ExecutePackage", commandPayload);
                    _logger.LogInformation("SignalR command sent to bot agent {BotAgentId} for scheduled execution {ExecutionId} from schedule {ScheduleId}",
                        botAgentId, execution.Id, scheduleId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send SignalR command to bot agent {BotAgentId} for scheduled execution {ExecutionId}",
                        botAgentId, execution.Id);
                }
            }
            else
            {
                _logger.LogWarning("SignalR sender not available - scheduled execution {ExecutionId} created but command not sent to bot agent",
                    execution.Id);
            }

            _logger.LogInformation("Scheduled execution {ExecutionId} triggered for bot agent {BotAgentId} from schedule {ScheduleId}",
                execution.Id, botAgentId, scheduleId);

            // Map to response DTO
            var responseDto = new ExecutionResponseDto
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
                ScheduleId = execution.ScheduleId,
                BotAgentName = botAgent.Name,
                PackageName = package.Name,
                PackageVersion = actualVersion
            };

            return responseDto;
        }
    }
} 