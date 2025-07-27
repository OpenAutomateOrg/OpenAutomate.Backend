using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.IRepository;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Infrastructure.Jobs
{
    /// <summary>
    /// Simple Quartz job that deletes organization units after 7 days
    /// </summary>
    public class OrganizationUnitDeletionJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrganizationUnitDeletionJob> _logger;

        public OrganizationUnitDeletionJob(IServiceProvider serviceProvider, ILogger<OrganizationUnitDeletionJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;

            try
            {
                if (!jobDataMap.TryGetValue("OrganizationUnitId", out var organizationUnitIdObj) ||
                    !Guid.TryParse(organizationUnitIdObj.ToString(), out var organizationUnitId))
                {
                    _logger.LogError("Invalid OrganizationUnitId in job data");
                    return;
                }

                _logger.LogInformation("Starting deletion for organization unit {OrganizationUnitId}", organizationUnitId);

                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Get the organization unit
                var organizationUnit = await unitOfWork.OrganizationUnits.GetByIdAsync(organizationUnitId);
                if (organizationUnit == null)
                {
                    _logger.LogWarning("Organization unit {OrganizationUnitId} not found", organizationUnitId);
                    return;
                }

                // Delete all related data first
                await DeleteRelatedDataAsync(unitOfWork, organizationUnitId);

                // Finally delete the organization unit
                unitOfWork.OrganizationUnits.Remove(organizationUnit);
                await unitOfWork.CompleteAsync();

                _logger.LogInformation("Successfully deleted organization unit {OrganizationUnitId}", organizationUnitId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting organization unit");
                throw;
            }
        }

        private async Task DeleteRelatedDataAsync(IUnitOfWork unitOfWork, Guid organizationUnitId)
        {
            _logger.LogInformation("Deleting related data for organization unit {OrganizationUnitId}", organizationUnitId);

            // Delete AssetBotAgents
            var allAssetBotAgents = await unitOfWork.AssetBotAgents.GetAllAsync();
            var assetBotAgents = allAssetBotAgents
                .Where(aba => aba.OrganizationUnitId == organizationUnitId)
                .ToList();
            unitOfWork.AssetBotAgents.RemoveRange(assetBotAgents);

            // Delete Assets
            var allAssets = await unitOfWork.Assets.GetAllAsync();
            var assets = allAssets
                .Where(a => a.OrganizationUnitId == organizationUnitId)
                .ToList();
            unitOfWork.Assets.RemoveRange(assets);

            // Delete Executions
            var allExecutions = await unitOfWork.Executions.GetAllAsync();
            var executions = allExecutions
                .Where(e => e.OrganizationUnitId == organizationUnitId)
                .ToList();
            unitOfWork.Executions.RemoveRange(executions);

            // Delete Schedules
            var scheduleRepository = unitOfWork.GetRepository<Schedule>();
            var allSchedules = await scheduleRepository.GetAllAsync();
            var schedules = allSchedules
                .Where(s => s.OrganizationUnitId == organizationUnitId)
                .ToList();
            scheduleRepository.RemoveRange(schedules);

            // Delete BotAgents
            var allBotAgents = await unitOfWork.BotAgents.GetAllAsync();
            var botAgents = allBotAgents
                .Where(ba => ba.OrganizationUnitId == organizationUnitId)
                .ToList();
            unitOfWork.BotAgents.RemoveRange(botAgents);

            // Delete AutomationPackages
            var allPackages = await unitOfWork.AutomationPackages.GetAllAsync();
            var packages = allPackages
                .Where(ap => ap.OrganizationUnitId == organizationUnitId)
                .ToList();
            unitOfWork.AutomationPackages.RemoveRange(packages);

            // Delete OrganizationUnitInvitations
            var allInvitations = await unitOfWork.OrganizationUnitInvitations.GetAllAsync();
            var invitations = allInvitations
                .Where(i => i.OrganizationUnitId == organizationUnitId)
                .ToList();
            unitOfWork.OrganizationUnitInvitations.RemoveRange(invitations);

            // Delete UserAuthorities
            var allUserAuthorities = await unitOfWork.UserAuthorities.GetAllAsync();
            var userAuthorities = allUserAuthorities
                .Where(ua => ua.OrganizationUnitId == organizationUnitId)
                .ToList();
            unitOfWork.UserAuthorities.RemoveRange(userAuthorities);

            // Delete AuthorityResources
            var allAuthorities = await unitOfWork.Authorities.GetAllAsync();
            var authorityIds = allAuthorities
                .Where(a => a.OrganizationUnitId == organizationUnitId)
                .Select(a => a.Id)
                .ToList();
            var allAuthorityResources = await unitOfWork.AuthorityResources.GetAllAsync();
            var authorityResources = allAuthorityResources
                .Where(ar => authorityIds.Contains(ar.AuthorityId))
                .ToList();
            unitOfWork.AuthorityResources.RemoveRange(authorityResources);

            // Delete Authorities
            var authorities = allAuthorities
                .Where(a => a.OrganizationUnitId == organizationUnitId)
                .ToList();
            unitOfWork.Authorities.RemoveRange(authorities);

            // Delete OrganizationUnitUsers
            var allOuUsers = await unitOfWork.OrganizationUnitUsers.GetAllAsync();
            var ouUsers = allOuUsers
                .Where(ouu => ouu.OrganizationUnitId == organizationUnitId)
                .ToList();
            unitOfWork.OrganizationUnitUsers.RemoveRange(ouUsers);

            await unitOfWork.CompleteAsync();

            _logger.LogInformation("Deleted related data for organization unit {OrganizationUnitId}", organizationUnitId);
        }
    }
}