using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.Execution;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service for managing bot executions
    /// </summary>
    public class ExecutionService : IExecutionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<ExecutionService> _logger;

        /// <summary>
        /// Initializes a new instance of the ExecutionService
        /// </summary>
        public ExecutionService(
            IUnitOfWork unitOfWork,
            ITenantContext tenantContext,
            ILogger<ExecutionService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new execution record
        /// </summary>
        public async Task<Execution> CreateExecutionAsync(CreateExecutionDto dto)
        {
            var execution = new Execution
            {
                BotAgentId = dto.BotAgentId,
                PackageId = dto.PackageId,
                ScheduleId = dto.ScheduleId,
                Status = "Pending",
                StartTime = DateTime.UtcNow,
                OrganizationUnitId = _tenantContext.CurrentTenantId
            };

            await _unitOfWork.Executions.AddAsync(execution);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Execution created: {ExecutionId}, BotAgent: {BotAgentId}, Package: {PackageId}",
                execution.Id, dto.BotAgentId, dto.PackageId);

            return execution;
        }

        /// <summary>
        /// Gets an execution by ID
        /// </summary>
        public async Task<Execution?> GetExecutionByIdAsync(Guid id)
        {
            var execution = await _unitOfWork.Executions.GetByIdAsync(id);
            
            // Ensure the execution belongs to the current tenant
            if (execution?.OrganizationUnitId != _tenantContext.CurrentTenantId)
            {
                return null;
            }

            return execution;
        }

        /// <summary>
        /// Gets all executions for the current tenant
        /// </summary>
        public async Task<IEnumerable<Execution>> GetAllExecutionsAsync()
        {
            var executions = await _unitOfWork.Executions.GetAllAsync(
                e => e.OrganizationUnitId == _tenantContext.CurrentTenantId,
                null,
                e => e.BotAgent,
                e => e.Package,
                e => e.Package.Versions);

            return executions;
        }

        /// <summary>
        /// Gets executions for a specific bot agent
        /// </summary>
        public async Task<IEnumerable<Execution>> GetExecutionsByBotAgentIdAsync(Guid botAgentId)
        {
            var executions = await _unitOfWork.Executions.GetAllAsync(
                e => e.BotAgentId == botAgentId && e.OrganizationUnitId == _tenantContext.CurrentTenantId,
                null,
                e => e.BotAgent,
                e => e.Package,
                e => e.Package.Versions);

            return executions;
        }

        /// <summary>
        /// Updates execution status
        /// </summary>
        public async Task<Execution?> UpdateExecutionStatusAsync(Guid id, string status, string? errorMessage = null, string? logOutput = null)
        {
            var execution = await GetExecutionByIdAsync(id);
            if (execution == null)
            {
                return null;
            }

            execution.Status = status;
            execution.ErrorMessage = errorMessage;
            execution.LogOutput = logOutput;

            // Set end time if execution is completed, failed, or cancelled
            if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Failed", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                execution.EndTime = DateTime.UtcNow;
            }

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Execution status updated: {ExecutionId}, Status: {Status}",
                execution.Id, status);

            return execution;
        }

        /// <summary>
        /// Cancels an execution
        /// </summary>
        public async Task<Execution?> CancelExecutionAsync(Guid id)
        {
            var execution = await GetExecutionByIdAsync(id);
            if (execution == null)
            {
                return null;
            }

            // Only allow cancelling pending or running executions
            if (!execution.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) &&
                !execution.Status.Equals("Running", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Cannot cancel execution {ExecutionId} with status {Status}", 
                    execution.Id, execution.Status);
                return execution;
            }

            execution.Status = "Cancelled";
            execution.EndTime = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Execution cancelled: {ExecutionId}", execution.Id);

            return execution;
        }

        /// <summary>
        /// Gets active executions for a bot agent
        /// </summary>
        public async Task<IEnumerable<Execution>> GetActiveExecutionsByBotAgentIdAsync(Guid botAgentId)
        {
            var executions = await _unitOfWork.Executions.GetAllAsync(
                e => e.BotAgentId == botAgentId && 
                     e.OrganizationUnitId == _tenantContext.CurrentTenantId &&
                     (e.Status == "Pending" || e.Status == "Running"),
                null,
                e => e.BotAgent,
                e => e.Package,
                e => e.Package.Versions);

            return executions;
        }
    }
} 