using OpenAutomate.Core.Dto.Execution;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for triggering executions with full workflow including SignalR communication
    /// </summary>
    public interface IExecutionTriggerService
    {
        /// <summary>
        /// Triggers a new execution with full workflow including SignalR communication to bot agent
        /// </summary>
        /// <param name="dto">Execution trigger data</param>
        /// <returns>Created execution response</returns>
        Task<ExecutionResponseDto> TriggerExecutionAsync(TriggerExecutionDto dto);

        /// <summary>
        /// Triggers a scheduled execution for a specific schedule
        /// </summary>
        /// <param name="scheduleId">Schedule ID that triggered this execution</param>
        /// <param name="botAgentId">Bot agent to execute on</param>
        /// <param name="packageId">Package to execute</param>
        /// <param name="packageName">Package name for SignalR payload</param>
        /// <param name="version">Package version for SignalR payload</param>
        /// <returns>Created execution response</returns>
        Task<ExecutionResponseDto> TriggerScheduledExecutionAsync(
            Guid scheduleId, 
            Guid botAgentId, 
            Guid packageId, 
            string packageName, 
            string version);
    }
} 