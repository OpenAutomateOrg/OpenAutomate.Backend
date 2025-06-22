using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for managing Quartz.NET database schema
    /// </summary>
    public interface IQuartzSchemaService
    {
        /// <summary>
        /// Ensures that the Quartz.NET database schema exists and is up to date
        /// </summary>
        /// <returns>True if schema was created or already exists, false if there was an error</returns>
        Task<bool> EnsureSchemaExistsAsync();
        
        /// <summary>
        /// Checks if the Quartz.NET schema exists in the database
        /// </summary>
        /// <returns>True if all required tables exist, false otherwise</returns>
        Task<bool> SchemaExistsAsync();
    }
} 