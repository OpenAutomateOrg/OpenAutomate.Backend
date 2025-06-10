using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.Dto.Common;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Simplified service for managing automation schedules
    /// </summary>
    public class ScheduleServiceSimplified : IScheduleService
    {
        private const string TenantNotAvailableMessage = "Current tenant ID is not available";
        
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<ScheduleServiceSimplified> _logger;

        private static class LogMessages
        {
            public const string ScheduleCreated = "Schedule created: {ScheduleId}, Name: {ScheduleName}, Type: {ScheduleType}, Tenant: {TenantId}";
            public const string ScheduleUpdated = "Schedule updated: {ScheduleId}, Name: {ScheduleName}, Tenant: {TenantId}";
            public const string ScheduleDeleted = "Schedule deleted: {ScheduleId}, Tenant: {TenantId}";
            public const string SchedulePaused = "Schedule paused: {ScheduleId}, Tenant: {TenantId}";
            public const string ScheduleResumed = "Schedule resumed: {ScheduleId}, Tenant: {TenantId}";
            public const string ScheduleNotFound = "Schedule not found: {ScheduleId}, Tenant: {TenantId}";
        }

        public ScheduleServiceSimplified(
            IUnitOfWork unitOfWork,
            ITenantContext tenantContext,
            ILogger<ScheduleServiceSimplified> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ScheduleResponseDto> CreateOneTimeScheduleAsync(CreateOneTimeScheduleDto dto)
        {
            if (!_tenantContext.HasTenant)
            {
                throw new InvalidOperationException(TenantNotAvailableMessage);
            }

            var currentTenantId = _tenantContext.CurrentTenantId;

            // Simple validation
            if (dto.ExecutionDate <= DateTime.UtcNow)
            {
                throw new ValidationException("ExecutionDate", "Execution date must be in the future");
            }

            // Create the schedule entity
            var schedule = new Schedule
            {
                Name = dto.Name,
                Description = dto.Description,
                Type = ScheduleType.OneTime,
                OneTimeExecutionDate = dto.ExecutionDate,
                PackageId = dto.PackageId,
                IsActive = true,
                OrganizationUnitId = currentTenantId,
                CreatedById = currentTenantId // TODO: Get actual user ID from context
            };

            await _unitOfWork.Schedules.AddAsync(schedule);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation(LogMessages.ScheduleCreated, 
                schedule.Id, schedule.Name, schedule.Type, currentTenantId);

            return MapToResponseDto(schedule);
        }

        public async Task<ScheduleResponseDto> CreateScheduleAsync(CreateScheduleDto dto)
        {
            if (!_tenantContext.HasTenant)
            {
                throw new InvalidOperationException(TenantNotAvailableMessage);
            }

            var currentTenantId = _tenantContext.CurrentTenantId;

            // Simple validation
            if (dto.Type == ScheduleType.OneTime && dto.OneTimeExecutionDate <= DateTime.UtcNow)
            {
                throw new ValidationException("OneTimeExecutionDate", "Execution date must be in the future");
            }

            if ((dto.Type == ScheduleType.Recurring || dto.Type == ScheduleType.Cron) && string.IsNullOrEmpty(dto.CronExpression))
            {
                throw new ValidationException("CronExpression", "Cron expression is required for recurring schedules");
            }

            // Create the schedule entity
            var schedule = new Schedule
            {
                Name = dto.Name,
                Description = dto.Description,
                Type = dto.Type,
                CronExpression = dto.CronExpression ?? string.Empty,
                OneTimeExecutionDate = dto.OneTimeExecutionDate,
                PackageId = dto.PackageId,
                IsActive = true,
                OrganizationUnitId = currentTenantId,
                CreatedById = currentTenantId // TODO: Get actual user ID from context
            };

            await _unitOfWork.Schedules.AddAsync(schedule);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation(LogMessages.ScheduleCreated, 
                schedule.Id, schedule.Name, schedule.Type, currentTenantId);

            return MapToResponseDto(schedule);
        }

        public async Task<ScheduleResponseDto?> GetScheduleByIdAsync(Guid id)
        {
            if (!_tenantContext.HasTenant)
            {
                return null;
            }

            var currentTenantId = _tenantContext.CurrentTenantId;
            var schedule = await _unitOfWork.Schedules.GetByIdAsync(id);
            
            if (schedule?.OrganizationUnitId != currentTenantId)
            {
                _logger.LogWarning(LogMessages.ScheduleNotFound, id, currentTenantId);
                return null;
            }

            return MapToResponseDto(schedule);
        }

        public async Task<PagedResult<ScheduleResponseDto>> GetTenantSchedulesAsync(ScheduleQueryParameters parameters)
        {
            if (!_tenantContext.HasTenant)
            {
                throw new InvalidOperationException(TenantNotAvailableMessage);
            }

            var currentTenantId = _tenantContext.CurrentTenantId;
            
            // Get all schedules for the tenant
            var allSchedules = await _unitOfWork.Schedules.GetAllAsync(
                s => s.OrganizationUnitId == currentTenantId);

            var schedulesList = allSchedules.ToList();

            // Apply simple filters
            if (!string.IsNullOrEmpty(parameters.Search))
            {
                schedulesList = schedulesList.Where(s => 
                    s.Name.Contains(parameters.Search, StringComparison.OrdinalIgnoreCase) || 
                    s.Description.Contains(parameters.Search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (parameters.Type.HasValue)
            {
                schedulesList = schedulesList.Where(s => (int)s.Type == parameters.Type.Value).ToList();
            }

            if (parameters.IsActive.HasValue)
            {
                schedulesList = schedulesList.Where(s => s.IsActive == parameters.IsActive.Value).ToList();
            }

            // Simple sorting
            schedulesList = parameters.SortDirection?.ToLower() == "asc" 
                ? schedulesList.OrderBy(s => s.CreatedAt).ToList()
                : schedulesList.OrderByDescending(s => s.CreatedAt).ToList();

            var totalCount = schedulesList.Count;

            // Apply pagination
            var paginatedSchedules = schedulesList
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToList();

            var scheduleDtos = paginatedSchedules.Select(MapToResponseDto).ToList();

            return new PagedResult<ScheduleResponseDto>
            {
                Items = scheduleDtos,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ScheduleResponseDto?> UpdateScheduleAsync(Guid id, UpdateScheduleDto dto)
        {
            var schedule = await GetScheduleEntityAsync(id);
            if (schedule == null)
            {
                return null;
            }

            // Update schedule properties
            schedule.Name = dto.Name;
            schedule.Description = dto.Description;
            schedule.Type = dto.Type;
            schedule.CronExpression = dto.CronExpression ?? string.Empty;
            schedule.OneTimeExecutionDate = dto.OneTimeExecutionDate;
            schedule.IsActive = dto.IsActive;
            schedule.LastModifyAt = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync();

            var currentTenantId = _tenantContext.CurrentTenantId;
            _logger.LogInformation(LogMessages.ScheduleUpdated, 
                schedule.Id, schedule.Name, currentTenantId);

            return MapToResponseDto(schedule);
        }

        public async Task<bool> DeleteScheduleAsync(Guid id)
        {
            var schedule = await GetScheduleEntityAsync(id);
            if (schedule == null)
            {
                return false;
            }

            _unitOfWork.Schedules.Remove(schedule);
            await _unitOfWork.CompleteAsync();

            var currentTenantId = _tenantContext.CurrentTenantId;
            _logger.LogInformation(LogMessages.ScheduleDeleted, id, currentTenantId);
            return true;
        }

        public async Task<bool> PauseScheduleAsync(Guid id)
        {
            var schedule = await GetScheduleEntityAsync(id);
            if (schedule == null)
            {
                return false;
            }

            schedule.IsActive = false;
            schedule.LastModifyAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();

            var currentTenantId = _tenantContext.CurrentTenantId;
            _logger.LogInformation(LogMessages.SchedulePaused, id, currentTenantId);
            return true;
        }

        public async Task<bool> ResumeScheduleAsync(Guid id)
        {
            var schedule = await GetScheduleEntityAsync(id);
            if (schedule == null)
            {
                return false;
            }

            schedule.IsActive = true;
            schedule.LastModifyAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();

            var currentTenantId = _tenantContext.CurrentTenantId;
            _logger.LogInformation(LogMessages.ScheduleResumed, id, currentTenantId);
            return true;
        }

        public async Task<List<ScheduleResponseDto>> GetActiveSchedulesForTenantAsync()
        {
            if (!_tenantContext.HasTenant)
            {
                throw new InvalidOperationException(TenantNotAvailableMessage);
            }

            var currentTenantId = _tenantContext.CurrentTenantId;
            
            var schedules = await _unitOfWork.Schedules.GetAllAsync(
                s => s.OrganizationUnitId == currentTenantId && s.IsActive);

            return schedules.Select(MapToResponseDto).ToList();
        }

        private async Task<Schedule?> GetScheduleEntityAsync(Guid id)
        {
            if (!_tenantContext.HasTenant)
            {
                return null;
            }

            var currentTenantId = _tenantContext.CurrentTenantId;
            var schedule = await _unitOfWork.Schedules.GetByIdAsync(id);
            
            if (schedule?.OrganizationUnitId != currentTenantId)
            {
                _logger.LogWarning(LogMessages.ScheduleNotFound, id, currentTenantId);
                return null;
            }

            return schedule;
        }

        private static ScheduleResponseDto MapToResponseDto(Schedule schedule)
        {
            return new ScheduleResponseDto
            {
                Id = schedule.Id,
                Name = schedule.Name,
                Description = schedule.Description,
                CronExpression = schedule.CronExpression,
                IsActive = schedule.IsActive,
                Type = schedule.Type,
                OneTimeExecutionDate = schedule.OneTimeExecutionDate,
                PackageId = schedule.PackageId,
                CreatedById = schedule.CreatedById,
                CreatedAt = schedule.CreatedAt,
                LastModifyAt = schedule.LastModifyAt,
                PackageName = null, // TODO: Load package name
                CreatedByName = null, // TODO: Load user name
                NextExecution = null,
                LastExecution = null,
                LastExecutionStatus = null
            };
        }
    }
} 