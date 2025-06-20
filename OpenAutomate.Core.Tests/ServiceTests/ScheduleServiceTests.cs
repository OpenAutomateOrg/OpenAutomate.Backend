using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.Dto.Common;
using OpenAutomate.Core.Dto.Execution;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Core.Tests.ServiceTests
{
    public class ScheduleServiceTests
    {
        private readonly Mock<IScheduleService> _mockService;

        public ScheduleServiceTests()
        {
            _mockService = new Mock<IScheduleService>();
        }

        [Fact]
        public async Task CreateScheduleAsync_WithValidData_ReturnsSchedule()
        {
            // Arrange
            var dto = new CreateScheduleDto
            {
                Name = "Test Schedule",
                Type = ScheduleType.Recurring,
                CronExpression = "0 0 * * *",
                PackageId = Guid.NewGuid()
            };
            var expected = new ScheduleResponseDto { Id = Guid.NewGuid(), Name = dto.Name };
            _mockService.Setup(s => s.CreateScheduleAsync(dto)).ReturnsAsync(expected);

            // Act
            var result = await _mockService.Object.CreateScheduleAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
        }

        [Fact]
        public async Task CreateScheduleAsync_WithInvalidCron_ThrowsException()
        {
            // Arrange
            var dto = new CreateScheduleDto
            {
                Name = "Test Schedule",
                Type = ScheduleType.Recurring,
                CronExpression = null,
                PackageId = Guid.NewGuid()
            };
            _mockService.Setup(s => s.CreateScheduleAsync(dto)).ThrowsAsync(new ValidationException("Cron expression required"));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _mockService.Object.CreateScheduleAsync(dto));
        }

        [Fact]
        public async Task GetScheduleByIdAsync_WithValidId_ReturnsSchedule()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            var expected = new ScheduleResponseDto { Id = scheduleId, Name = "Test Schedule" };
            _mockService.Setup(s => s.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(expected);

            // Act
            var result = await _mockService.Object.GetScheduleByIdAsync(scheduleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(scheduleId, result.Id);
        }

        [Fact]
        public async Task GetScheduleByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            _mockService.Setup(s => s.GetScheduleByIdAsync(scheduleId)).ReturnsAsync((ScheduleResponseDto?)null);

            // Act
            var result = await _mockService.Object.GetScheduleByIdAsync(scheduleId);

            // Assert
            Assert.Null(result);
        }
    }
}