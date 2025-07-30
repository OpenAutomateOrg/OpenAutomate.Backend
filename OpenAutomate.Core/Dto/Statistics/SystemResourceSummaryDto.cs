using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Dto.Statistics
{
    /// <summary>
    /// DTO for system-wide resource summary
    /// </summary>
    public class SystemResourceSummaryDto
    {
        /// <summary>
        /// Total number of organization units
        /// </summary>
        public int TotalOrganizationUnits { get; set; }

        /// <summary>
        /// Total Bot Agents across all OUs
        /// </summary>
        public int TotalBotAgents { get; set; }

        /// <summary>
        /// Total Assets across all OUs
        /// </summary>
        public int TotalAssets { get; set; }

        /// <summary>
        /// Total Automation Packages across all OUs
        /// </summary>
        public int TotalAutomationPackages { get; set; }

        /// <summary>
        /// Total Executions across all OUs
        /// </summary>
        public int TotalExecutions { get; set; }

        /// <summary>
        /// Total Schedules across all OUs
        /// </summary>
        public int TotalSchedules { get; set; }

        /// <summary>
        /// Total Users across all OUs
        /// </summary>
        public int TotalUsers { get; set; }
    }
}
