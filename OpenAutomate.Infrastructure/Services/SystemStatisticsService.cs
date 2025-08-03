using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.Statistics;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for system statistics
    /// </summary>
    public class SystemStatisticsService : ISystemStatisticsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SystemStatisticsService> _logger;

        public SystemStatisticsService(IUnitOfWork unitOfWork, ILogger<SystemStatisticsService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<SystemResourceSummaryDto> GetSystemResourceSummaryAsync()
        {
            try
            {
                _logger.LogInformation("Getting system-wide resource summary");

                // Initialize summary with default values
                var summary = new SystemResourceSummaryDto();

                // Count Organization Units
                try
                {
                    var organizationUnits = await _unitOfWork.OrganizationUnits.GetAllAsync();
                    summary.TotalOrganizationUnits = organizationUnits.Count();
                    _logger.LogDebug("Organization Units count: {Count}", summary.TotalOrganizationUnits);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting organization units");
                    summary.TotalOrganizationUnits = 0;
                }

                // Count Bot Agents
                try
                {
                    var botAgents = await _unitOfWork.BotAgents.GetAllAsync();
                    summary.TotalBotAgents = botAgents.Count();
                    _logger.LogDebug("Bot Agents count: {Count}", summary.TotalBotAgents);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting bot agents");
                    summary.TotalBotAgents = 0;
                }

                // Count Assets
                try
                {
                    var assets = await _unitOfWork.Assets.GetAllAsync();
                    summary.TotalAssets = assets.Count();
                    _logger.LogDebug("Assets count: {Count}", summary.TotalAssets);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting assets");
                    summary.TotalAssets = 0;
                }

                // Count Automation Packages
                try
                {
                    var automationPackages = await _unitOfWork.AutomationPackages.GetAllAsync();
                    summary.TotalAutomationPackages = automationPackages.Count();
                    _logger.LogDebug("Automation Packages count: {Count}", summary.TotalAutomationPackages);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting automation packages");
                    summary.TotalAutomationPackages = 0;
                }

                // Count Executions
                try
                {
                    var executions = await _unitOfWork.Executions.GetAllAsync();
                    summary.TotalExecutions = executions.Count();
                    _logger.LogDebug("Executions count: {Count}", summary.TotalExecutions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting executions");
                    summary.TotalExecutions = 0;
                }

                // Count Schedules
                try
                {
                    var scheduleRepository = _unitOfWork.GetRepository<Schedule>();
                    var schedules = await scheduleRepository.GetAllAsync();
                    summary.TotalSchedules = schedules.Count();
                    _logger.LogDebug("Schedules count: {Count}", summary.TotalSchedules);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting schedules");
                    summary.TotalSchedules = 0;
                }

                // Count Users
                try
                {
                    var users = await _unitOfWork.Users.GetAllAsync();
                    summary.TotalUsers = users.Count();
                    _logger.LogDebug("Users count: {Count}", summary.TotalUsers);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting users");
                    summary.TotalUsers = 0;
                }

                _logger.LogInformation("Generated system resource summary - OUs: {OUs}, BotAgents: {BotAgents}, Assets: {Assets}, Packages: {Packages}, Executions: {Executions}, Schedules: {Schedules}, Users: {Users}",
                    summary.TotalOrganizationUnits,
                    summary.TotalBotAgents,
                    summary.TotalAssets,
                    summary.TotalAutomationPackages,
                    summary.TotalExecutions,
                    summary.TotalSchedules,
                    summary.TotalUsers);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system resource summary");
                throw;
            }
        }
    }
}
