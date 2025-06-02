using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.Execution;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service interface for managing bot executions
    /// </summary>
    public interface IExecutionService
    {
        /// <summary>
        /// Creates a new execution record
        /// </summary>
        /// <param name="dto">Execution creation data</param>
        /// <returns>Created execution</returns>
        Task<Execution> CreateExecutionAsync(CreateExecutionDto dto);

        /// <summary>
        /// Gets an execution by ID
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <returns>Execution if found, null otherwise</returns>
        Task<Execution?> GetExecutionByIdAsync(Guid id);

        /// <summary>
        /// Gets all executions for the current tenant
        /// </summary>
        /// <returns>List of executions</returns>
        Task<IEnumerable<Execution>> GetAllExecutionsAsync();

        /// <summary>
        /// Gets executions for a specific bot agent
        /// </summary>
        /// <param name="botAgentId">Bot agent ID</param>
        /// <returns>List of executions</returns>
        Task<IEnumerable<Execution>> GetExecutionsByBotAgentIdAsync(Guid botAgentId);

        /// <summary>
        /// Updates execution status
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <param name="status">New status</param>
        /// <param name="errorMessage">Optional error message</param>
        /// <param name="logOutput">Optional log output</param>
        /// <returns>Updated execution</returns>
        Task<Execution?> UpdateExecutionStatusAsync(Guid id, string status, string? errorMessage = null, string? logOutput = null);

        /// <summary>
        /// Cancels an execution
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <returns>Updated execution</returns>
        Task<Execution?> CancelExecutionAsync(Guid id);

        /// <summary>
        /// Gets active executions for a bot agent
        /// </summary>
        /// <param name="botAgentId">Bot agent ID</param>
        /// <returns>List of active executions</returns>
        Task<IEnumerable<Execution>> GetActiveExecutionsByBotAgentIdAsync(Guid botAgentId);
    }
} 