using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Domain.Enums;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Text.Json;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class ScheduleControllerTests
    {
        private readonly Mock<IScheduleService> _mockScheduleService;
        private readonly Mock<ILogger<ScheduleController>> _mockLogger;
        private readonly Mock<IQuartzScheduleManager> _mockQuartzManager;
        private readonly ScheduleController _controller;

        public ScheduleControllerTests()
        {
            _mockScheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<ScheduleController>>();
            _mockQuartzManager = new Mock<IQuartzScheduleManager>();
            _controller = new ScheduleController(_mockScheduleService.Object, _mockLogger.Object, _mockQuartzManager.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues["tenant"] = "test-tenant";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

       

        [Fact]
        public async Task CreateSchedule_WithInvalidCron_ReturnsBadRequest()
        {
            var createDto = new CreateScheduleDto
            {
                Name = "Test Schedule",
                CronExpression = "invalid-cron",
                AutomationPackageId = Guid.NewGuid(),
                TimeZoneId = "UTC",
                IsEnabled = true,
                RecurrenceType = RecurrenceType.Daily,
                BotAgentId = Guid.NewGuid()
            };

            _mockScheduleService.Setup(x => x.CreateScheduleAsync(It.Is<CreateScheduleDto>(dto => 
                dto.Name == createDto.Name && 
                dto.CronExpression == createDto.CronExpression &&
                dto.AutomationPackageId == createDto.AutomationPackageId &&
                dto.TimeZoneId == createDto.TimeZoneId &&
                dto.IsEnabled == createDto.IsEnabled &&
                dto.RecurrenceType == createDto.RecurrenceType &&
                dto.BotAgentId == createDto.BotAgentId)))
                .ThrowsAsync(new ArgumentException("Invalid cron expression"));

            var result = await _controller.CreateSchedule(createDto);

            Assert.NotNull(result.Result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task CreateSchedule_WithDuplicateName_ReturnsConflict()
        {
            var createDto = new CreateScheduleDto
            {
                Name = "Existing Schedule",
                CronExpression = "0 0 * * *",
                AutomationPackageId = Guid.NewGuid(),
                TimeZoneId = "UTC",
                IsEnabled = true,
                RecurrenceType = RecurrenceType.Daily,
                BotAgentId = Guid.NewGuid()
            };

            _mockScheduleService.Setup(x => x.CreateScheduleAsync(createDto))
                .ThrowsAsync(new InvalidOperationException("Schedule with this name already exists"));

            var result = await _controller.CreateSchedule(createDto);

            Assert.NotNull(result.Result);
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        }

        [Fact]
        public async Task GetScheduleById_WithValidId_ReturnsSchedule()
        {
            var scheduleId = Guid.NewGuid();
            var schedule = new ScheduleResponseDto
            {
                Id = scheduleId,
                Name = "Test Schedule",
                CronExpression = "0 0 * * *",
                AutomationPackageId = Guid.NewGuid(),
                IsEnabled = true,
                TimeZoneId = "UTC",
                RecurrenceType = RecurrenceType.Daily,
                BotAgentId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            _mockScheduleService.Setup(x => x.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync(schedule);

            var result = await _controller.GetScheduleById(scheduleId);

            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var returnedSchedule = Assert.IsType<ScheduleResponseDto>(okResult.Value);
            Assert.Equal(scheduleId, returnedSchedule.Id);
            Assert.Equal(schedule.Name, returnedSchedule.Name);
        }

        [Fact]
        public async Task GetScheduleById_WithInvalidId_ReturnsNotFound()
        {
            var scheduleId = Guid.NewGuid();
            _mockScheduleService.Setup(x => x.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync((ScheduleResponseDto?)null);

            var result = await _controller.GetScheduleById(scheduleId);

            Assert.NotNull(result.Result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetAllSchedules_ReturnsAllSchedules()
        {
            var schedules = new List<ScheduleResponseDto>
            {
                new ScheduleResponseDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Schedule 1",
                    CronExpression = "0 0 * * *",
                    AutomationPackageId = Guid.NewGuid(),
                    IsEnabled = true,
                    RecurrenceType = RecurrenceType.Daily,
                    BotAgentId = Guid.NewGuid()
                },
                new ScheduleResponseDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Schedule 2",
                    CronExpression = "0 12 * * *",
                    AutomationPackageId = Guid.NewGuid(),
                    IsEnabled = false,
                    RecurrenceType = RecurrenceType.Daily,
                    BotAgentId = Guid.NewGuid()
                }
            };

            _mockScheduleService.Setup(x => x.GetAllSchedulesAsync())
                .ReturnsAsync(schedules);

            var result = await _controller.GetAllSchedules();

            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var returnedSchedules = Assert.IsAssignableFrom<IEnumerable<ScheduleResponseDto>>(okResult.Value);
            Assert.Equal(2, returnedSchedules.Count());
        }

        [Fact]
        public async Task UpdateSchedule_WithValidData_ReturnsOk()
        {
            var scheduleId = Guid.NewGuid();
            var updateDto = new UpdateScheduleDto
            {
                Name = "Updated Schedule",
                CronExpression = "0 12 * * *",
                TimeZoneId = "America/New_York",
                IsEnabled = false,
                RecurrenceType = RecurrenceType.Daily,
                AutomationPackageId = Guid.NewGuid(),
                BotAgentId = Guid.NewGuid()
            };

            var updatedSchedule = new ScheduleResponseDto
            {
                Id = scheduleId,
                Name = updateDto.Name,
                CronExpression = updateDto.CronExpression,
                TimeZoneId = updateDto.TimeZoneId,
                IsEnabled = updateDto.IsEnabled,
                RecurrenceType = updateDto.RecurrenceType,
                AutomationPackageId = updateDto.AutomationPackageId,
                BotAgentId = updateDto.BotAgentId
            };

            _mockScheduleService.Setup(x => x.UpdateScheduleAsync(scheduleId, updateDto))
                .ReturnsAsync(updatedSchedule);

            var result = await _controller.UpdateSchedule(scheduleId, updateDto);

            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var returnedSchedule = Assert.IsType<ScheduleResponseDto>(okResult.Value);
            Assert.Equal(scheduleId, returnedSchedule.Id);
            Assert.Equal(updateDto.Name, returnedSchedule.Name);
            Assert.Equal(updateDto.CronExpression, returnedSchedule.CronExpression);
            Assert.Equal(updateDto.IsEnabled, returnedSchedule.IsEnabled);
        }

        [Fact]
        public async Task UpdateSchedule_WithInvalidId_ReturnsNotFound()
        {
            var scheduleId = Guid.NewGuid();
            var updateDto = new UpdateScheduleDto
            {
                Name = "Updated Schedule",
                CronExpression = "0 12 * * *",
                TimeZoneId = "America/New_York",
                IsEnabled = true,
                RecurrenceType = RecurrenceType.Daily,
                AutomationPackageId = Guid.NewGuid(),
                BotAgentId = Guid.NewGuid()
            };

            _mockScheduleService.Setup(x => x.UpdateScheduleAsync(scheduleId, updateDto))
                .ReturnsAsync((ScheduleResponseDto?)null);

            var result = await _controller.UpdateSchedule(scheduleId, updateDto);

            Assert.NotNull(result.Result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task UpdateSchedule_WithInvalidCron_ReturnsBadRequest()
        {
            var scheduleId = Guid.NewGuid();
            var updateDto = new UpdateScheduleDto
            {
                Name = "Updated Schedule",
                CronExpression = "invalid-cron",
                TimeZoneId = "America/New_York",
                IsEnabled = true,
                RecurrenceType = RecurrenceType.Daily,
                AutomationPackageId = Guid.NewGuid(),
                BotAgentId = Guid.NewGuid()
            };

            _mockScheduleService.Setup(x => x.UpdateScheduleAsync(scheduleId, updateDto))
                .ThrowsAsync(new ArgumentException("Invalid cron expression"));

            var result = await _controller.UpdateSchedule(scheduleId, updateDto);

            Assert.NotNull(result.Result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task DeleteSchedule_WithValidId_ReturnsNoContent()
        {
            var scheduleId = Guid.NewGuid();
            _mockScheduleService.Setup(x => x.DeleteScheduleAsync(scheduleId))
                .ReturnsAsync(true);

            var result = await _controller.DeleteSchedule(scheduleId);

            Assert.NotNull(result);
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
        }

        [Fact]
        public async Task DeleteSchedule_WithInvalidId_ReturnsNotFound()
        {
            var scheduleId = Guid.NewGuid();
            _mockScheduleService.Setup(x => x.DeleteScheduleAsync(scheduleId))
                .ReturnsAsync(false);

            var result = await _controller.DeleteSchedule(scheduleId);

            Assert.NotNull(result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task EnableSchedule_WithValidId_ReturnsOk()
        {
            var scheduleId = Guid.NewGuid();
            var enabledSchedule = new ScheduleResponseDto
            {
                Id = scheduleId,
                Name = "Test Schedule",
                IsEnabled = true,
                CronExpression = "0 0 * * *",
                AutomationPackageId = Guid.NewGuid(),
                RecurrenceType = RecurrenceType.Daily,
                BotAgentId = Guid.NewGuid()
            };

            _mockScheduleService.Setup(x => x.EnableScheduleAsync(scheduleId))
                .ReturnsAsync(enabledSchedule);

            var result = await _controller.EnableSchedule(scheduleId);

            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var returnedSchedule = Assert.IsType<ScheduleResponseDto>(okResult.Value);
            Assert.Equal(scheduleId, returnedSchedule.Id);
            Assert.True(returnedSchedule.IsEnabled);
        }

        [Fact]
        public async Task EnableSchedule_WithInvalidId_ReturnsNotFound()
        {
            var scheduleId = Guid.NewGuid();
            _mockScheduleService.Setup(x => x.EnableScheduleAsync(scheduleId))
                .ReturnsAsync((ScheduleResponseDto?)null);

            var result = await _controller.EnableSchedule(scheduleId);

            Assert.NotNull(result.Result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task DisableSchedule_WithValidId_ReturnsOk()
        {
            var scheduleId = Guid.NewGuid();
            var disabledSchedule = new ScheduleResponseDto
            {
                Id = scheduleId,
                Name = "Test Schedule",
                IsEnabled = false,
                CronExpression = "0 0 * * *",
                AutomationPackageId = Guid.NewGuid(),
                RecurrenceType = RecurrenceType.Daily,
                BotAgentId = Guid.NewGuid()
            };

            _mockScheduleService.Setup(x => x.DisableScheduleAsync(scheduleId))
                .ReturnsAsync(disabledSchedule);

            var result = await _controller.DisableSchedule(scheduleId);

            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var returnedSchedule = Assert.IsType<ScheduleResponseDto>(okResult.Value);
            Assert.Equal(scheduleId, returnedSchedule.Id);
            Assert.False(returnedSchedule.IsEnabled);
        }

        [Fact]
        public async Task DisableSchedule_WithInvalidId_ReturnsNotFound()
        {
            var scheduleId = Guid.NewGuid();
            _mockScheduleService.Setup(x => x.DisableScheduleAsync(scheduleId))
                .ReturnsAsync((ScheduleResponseDto?)null);

            var result = await _controller.DisableSchedule(scheduleId);

            Assert.NotNull(result.Result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetUpcomingRunTimes_WithValidId_ReturnsUpcomingRuns()
        {
            var scheduleId = Guid.NewGuid();
            var schedule = new ScheduleResponseDto
            {
                Id = scheduleId,
                Name = "Test Schedule",
                CronExpression = "0 0 * * *",
                IsEnabled = true,
                TimeZoneId = "UTC",
                AutomationPackageId = Guid.NewGuid(),
                RecurrenceType = RecurrenceType.Daily,
                BotAgentId = Guid.NewGuid()
            };

            var upcomingTimes = new List<DateTime>
            {
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(3)
            };

            _mockScheduleService.Setup(x => x.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync(schedule);
            _mockScheduleService.Setup(x => x.CalculateUpcomingRunTimes(schedule, It.IsAny<int>()))
                .Returns(upcomingTimes);

            var result = await _controller.GetUpcomingRunTimes(scheduleId);

            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal(scheduleId.ToString(), root.GetProperty("scheduleId").GetString());
            Assert.Equal(schedule.Name, root.GetProperty("scheduleName").GetString());
            Assert.Equal(schedule.IsEnabled, root.GetProperty("isEnabled").GetBoolean());
            Assert.Equal(3, root.GetProperty("upcomingRuns").GetArrayLength());
        }

        [Fact]
        public async Task GetUpcomingRunTimes_WithInvalidId_ReturnsNotFound()
        {
            var scheduleId = Guid.NewGuid();
            _mockScheduleService.Setup(x => x.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync((ScheduleResponseDto?)null);

            var result = await _controller.GetUpcomingRunTimes(scheduleId);

            Assert.NotNull(result.Result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetUpcomingRunTimes_LimitsResultsToMaximum()
        {
            var scheduleId = Guid.NewGuid();
            var schedule = new ScheduleResponseDto
            {
                Id = scheduleId,
                Name = "Test Schedule",
                CronExpression = "0 0 * * *",
                IsEnabled = true,
                TimeZoneId = "UTC",
                AutomationPackageId = Guid.NewGuid(),
                RecurrenceType = RecurrenceType.Daily,
                BotAgentId = Guid.NewGuid()
            };

            var upcomingTimes = Enumerable.Range(1, 5)
                .Select(i => DateTime.UtcNow.AddDays(i))
                .ToList();

            _mockScheduleService.Setup(x => x.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync(schedule);
            _mockScheduleService.Setup(x => x.CalculateUpcomingRunTimes(schedule, It.IsAny<int>()))
                .Returns(upcomingTimes);

            var result = await _controller.GetUpcomingRunTimes(scheduleId, 5);

            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);

            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal(5, root.GetProperty("upcomingRuns").GetArrayLength());
            _mockScheduleService.Verify(x => x.CalculateUpcomingRunTimes(schedule, 5), Times.Once);
        }
    }
}
