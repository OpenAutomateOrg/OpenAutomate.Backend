namespace OpenAutomate.Core.Constants
{
    /// <summary>
    /// Standardized status values for bot agents across the system
    /// </summary>
    public static class AgentStatus
    {
        /// <summary>
        /// Agent is connected and ready to accept work
        /// </summary>
        public const string Available = "Available";
        
        /// <summary>
        /// Agent is connected but currently executing a task
        /// </summary>
        public const string Busy = "Busy";
        
        /// <summary>
        /// Agent is not connected to the server
        /// </summary>
        public const string Disconnected = "Disconnected";
    }
} 