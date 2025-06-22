namespace OpenAutomate.Core.Domain.Enums
{
    /// <summary>
    /// Defines the type of recurrence for a schedule
    /// </summary>
    public enum RecurrenceType
    {
        /// <summary>
        /// Execute once at a specific date and time
        /// </summary>
        Once,

        /// <summary>
        /// Execute every specified number of minutes
        /// </summary>
        Minutes,

        /// <summary>
        /// Execute every hour
        /// </summary>
        Hourly,

        /// <summary>
        /// Execute daily at a specific time
        /// </summary>
        Daily,

        /// <summary>
        /// Execute weekly on specific days
        /// </summary>
        Weekly,

        /// <summary>
        /// Execute monthly on specific days
        /// </summary>
        Monthly,

        /// <summary>
        /// Use advanced cron expression for complex scheduling
        /// </summary>
        Advanced
    }
} 