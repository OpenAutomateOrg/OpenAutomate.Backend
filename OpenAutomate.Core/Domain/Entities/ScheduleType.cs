namespace OpenAutomate.Core.Domain.Entities
{
    /// <summary>
    /// Defines the types of schedules available
    /// </summary>
    public enum ScheduleType
    {
        /// <summary>
        /// One-time execution schedule
        /// </summary>
        OneTime = 1,
        
        /// <summary>
        /// Recurring schedule with patterns
        /// </summary>
        Recurring = 2,
        
        /// <summary>
        /// Custom cron expression schedule
        /// </summary>
        Cron = 3
    }
} 