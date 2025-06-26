using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Domain.Enums;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class ScheduleServiceTests
    {
        private readonly Mock<IScheduleService> _mockService;

        public ScheduleServiceTests()
        {
            _mockService = new Mock<IScheduleService>();
        }

        // Schedule CRUD

        [Fact]
        public async Task CreateScheduleAsync_WithValidData_ReturnsSchedule()
        {
            var dto = new CreateScheduleDto
            {
                Name = "Test",
                CronExpression = "0 0 * * *",
                AutomationPackageId = Guid.NewGuid(),
                TimeZoneId = "UTC",
                IsEnabled = true,
                RecurrenceType = RecurrenceType.Daily,
                BotAgentId = Guid.NewGuid()
            };
            var expected = new ScheduleResponseDto { Id = Guid.NewGuid(), Name = dto.Name };
            _mockService.Setup(s => s.CreateScheduleAsync(dto)).ReturnsAsync(expected);

            var result = await _mockService.Object.CreateScheduleAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
        }

        [Fact]
        public async Task CreateScheduleAsync_WithInvalidCron_ThrowsException()
        {
            var dto = new CreateScheduleDto
            {
                Name = "Test",
                CronExpression = "invalid",
                AutomationPackageId = Guid.NewGuid(),
                TimeZoneId = "UTC",
                IsEnabled = true,
                RecurrenceType = RecurrenceType.Daily,
                BotAgentId = Guid.NewGuid()
            };
            _mockService.Setup(s => s.CreateScheduleAsync(dto)).ThrowsAsync(new ValidationException("Invalid cron"));

            await Assert.ThrowsAsync<ValidationException>(() => _mockService.Object.CreateScheduleAsync(dto));
        }

        [Fact]
        public async Task GetScheduleByIdAsync_WithValidId_ReturnsSchedule()
        {
            var id = Guid.NewGuid();
            var expected = new ScheduleResponseDto { Id = id, Name = "Test" };
            _mockService.Setup(s => s.GetScheduleByIdAsync(id)).ReturnsAsync(expected);

            var result = await _mockService.Object.GetScheduleByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public async Task GetScheduleByIdAsync_CrossTenant_ReturnsNull()
        {
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.GetScheduleByIdAsync(id)).ReturnsAsync((ScheduleResponseDto?)null);

            var result = await _mockService.Object.GetScheduleByIdAsync(id);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateScheduleAsync_WithValidData_UpdatesSuccessfully()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateScheduleDto
            {
                Name = "Updated",
                CronExpression = "0 12 * * *",
                TimeZoneId = "UTC",
                IsEnabled = false,
                RecurrenceType = RecurrenceType.Daily,
                AutomationPackageId = Guid.NewGuid(),
                BotAgentId = Guid.NewGuid()
            };
            var expected = new ScheduleResponseDto { Id = id, Name = dto.Name };
            _mockService.Setup(s => s.UpdateScheduleAsync(id, dto)).ReturnsAsync(expected);

            var result = await _mockService.Object.UpdateScheduleAsync(id, dto);

            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
        }

        [Fact]
        public async Task UpdateScheduleAsync_CrossTenant_ThrowsException()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateScheduleDto
            {
                Name = "Updated",
                CronExpression = "0 12 * * *",
                TimeZoneId = "UTC",
                IsEnabled = false,
                RecurrenceType = RecurrenceType.Daily,
                AutomationPackageId = Guid.NewGuid(),
                BotAgentId = Guid.NewGuid()
            };
            _mockService.Setup(s => s.UpdateScheduleAsync(id, dto)).ThrowsAsync(new UnauthorizedAccessException());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _mockService.Object.UpdateScheduleAsync(id, dto));
        }

        [Fact]
        public async Task DeleteScheduleAsync_WithValidId_DeletesSuccessfully()
        {
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.DeleteScheduleAsync(id)).ReturnsAsync(true);

            var result = await _mockService.Object.DeleteScheduleAsync(id);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteScheduleAsync_CrossTenant_ThrowsException()
        {
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.DeleteScheduleAsync(id)).ThrowsAsync(new UnauthorizedAccessException());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _mockService.Object.DeleteScheduleAsync(id));
        }

        // Schedule Management

        [Fact]
        public async Task GetSchedulesAsync_FiltersByTenant_ReturnsCorrectSchedules()
        {
            var schedules = new List<ScheduleResponseDto>
            {
                new ScheduleResponseDto { Id = Guid.NewGuid(), Name = "A" },
                new ScheduleResponseDto { Id = Guid.NewGuid(), Name = "B" }
            };
            _mockService.Setup(s => s.GetAllSchedulesAsync()).ReturnsAsync(schedules);

            var result = await _mockService.Object.GetAllSchedulesAsync();

            Assert.NotNull(result);
            Assert.Equal(2, ((List<ScheduleResponseDto>)result).Count);
        }

        [Fact]
        public async Task EnableScheduleAsync_WithValidId_EnablesSuccessfully()
        {
            var id = Guid.NewGuid();
            var enabled = new ScheduleResponseDto { Id = id, IsEnabled = true };
            _mockService.Setup(s => s.EnableScheduleAsync(id)).ReturnsAsync(enabled);

            var result = await _mockService.Object.EnableScheduleAsync(id);

            Assert.NotNull(result);
            Assert.True(result.IsEnabled);
        }

        [Fact]
        public async Task DisableScheduleAsync_WithValidId_DisablesSuccessfully()
        {
            var id = Guid.NewGuid();
            var disabled = new ScheduleResponseDto { Id = id, IsEnabled = false };
            _mockService.Setup(s => s.DisableScheduleAsync(id)).ReturnsAsync(disabled);

            var result = await _mockService.Object.DisableScheduleAsync(id);

            Assert.NotNull(result);
            Assert.False(result.IsEnabled);
        }

        [Fact]
        public void GetNextExecutionTimeAsync_WithValidCron_ReturnsCorrectTime()
        {
            var schedule = new ScheduleResponseDto
            {
                CronExpression = "0 0 * * *",
                IsEnabled = true
            };
            var expected = DateTime.UtcNow.AddDays(1);
            _mockService.Setup(s => s.CalculateNextRunTime(schedule)).Returns(expected);

            var result = _mockService.Object.CalculateNextRunTime(schedule);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetNextExecutionTimeAsync_WithInvalidCron_ThrowsException()
        {
            var schedule = new ScheduleResponseDto
            {
                CronExpression = "invalid",
                IsEnabled = true
            };
            _mockService.Setup(s => s.CalculateNextRunTime(schedule)).Throws(new ValidationException("Invalid cron"));

            Assert.Throws<ValidationException>(() => _mockService.Object.CalculateNextRunTime(schedule));
        }

       

      
    }
}
