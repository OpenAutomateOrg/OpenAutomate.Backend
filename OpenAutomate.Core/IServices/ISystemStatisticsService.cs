using OpenAutomate.Core.Dto.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for system-wide statistics
    /// </summary>
    public interface ISystemStatisticsService
    {
        /// <summary>
        /// Gets system-wide resource summary
        /// </summary>
        /// <returns>Total counts of all resources across the system</returns>
        Task<SystemResourceSummaryDto> GetSystemResourceSummaryAsync();
    }
}
