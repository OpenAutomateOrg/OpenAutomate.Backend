using System;

namespace OpenAutomate.Core.Dto.Execution
{
    /// <summary>
    /// DTO for execution responses to the frontend
    /// </summary>
    public class ExecutionResponseDto
    {
        /// <summary>
        /// Execution ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Bot agent ID that is executing or executed the package
        /// </summary>
        public Guid BotAgentId { get; set; }

        /// <summary>
        /// Package ID being executed (null if package was deleted)
        /// </summary>
        public Guid? PackageId { get; set; }

        /// <summary>
        /// Current execution status (Pending, Running, Completed, Failed, Cancelled)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Execution start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Execution end time (null if still running)
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Error message if execution failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Execution log output
        /// </summary>
        public string? LogOutput { get; set; }

        /// <summary>
        /// Indicates whether log files are available for download
        /// </summary>
        public bool HasLogs { get; set; }

        /// <summary>
        /// Schedule ID if this execution was triggered by a schedule
        /// </summary>
        public Guid? ScheduleId { get; set; }

        /// <summary>
        /// Source of the execution (Manual or Scheduled)
        /// </summary>
        public string Source => ScheduleId.HasValue ? "Scheduled" : "Manual";

        // Navigation properties for display purposes

        /// <summary>
        /// Name of the bot agent
        /// </summary>
        public string? BotAgentName { get; set; }

        /// <summary>
        /// Name of the package
        /// </summary>
        public string? PackageName { get; set; }

        /// <summary>
        /// Version of the package executed
        /// </summary>
        public string? PackageVersion { get; set; }

        /// <summary>
        /// Name of the schedule if this execution was triggered by a schedule
        /// </summary>
        public string? ScheduleName { get; set; }
    }
} 