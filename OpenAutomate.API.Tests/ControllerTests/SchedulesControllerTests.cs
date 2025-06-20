using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.Dto.Common;
using Microsoft.AspNetCore.Http;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class SchedulesControllerTests
    {
        private readonly Mock<IScheduleService> _mockService;
        private readonly Mock<ILogger<SchedulesController>> _mockLogger;
        private readonly SchedulesController _controller;
        private readonly Guid _testUserId;

        public SchedulesControllerTests()
        {
            _mockService = new Mock<IScheduleService>();
            _mockLogger = new Mock<ILogger<SchedulesController>>();
            _controller = new SchedulesController(_mockService.Object, _mockLogger.Object);
            _testUserId = Guid.NewGuid();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.HttpContext.Items["UserId"] = _testUserId;
        }

        #region CreateSchedule

        [Fact]
        public async Task CreateSchedule_WithValidData_ReturnsCreated()
        {
            // Arrange
            var dto = new CreateScheduleDto { Name = "Test", Type = ScheduleType.Recurring, CronExpression = "0 0 * * *", PackageId = Guid.NewGuid() };
            var response = new ScheduleResponseDto { Id = Guid.NewGuid(), Name = dto.Name };
            _mockService.Setup(s => s.CreateScheduleAsync(dto)).ReturnsAsync(response);

            // Act
            var result = await _controller.CreateSchedule(dto);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var value = Assert.IsType<ScheduleResponseDto>(created.Value);
            Assert.Equal(dto.Name, value.Name);
        }

        [Fact]
        public async Task CreateSchedule_WithInvalidCron_ReturnsBadRequest()
        {
            // Arrange
            var dto = new CreateScheduleDto { Name = "Test", Type = ScheduleType.Recurring, CronExpression = "invalid", PackageId = Guid.NewGuid() };
            var errors = new Dictionary<string, List<string>>
                {
                    { "CronExpression", new List<string> { "Invalid" } }
                };
            _mockService.Setup(s => s.CreateScheduleAsync(dto)).ThrowsAsync(new ValidationException(errors));

            // Act
            var result = await _controller.CreateSchedule(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
            var errorDict = Assert.IsAssignableFrom<IDictionary<string, List<string>>>(badRequest.Value);
            Assert.True(errorDict.ContainsKey("CronExpression"));
            Assert.Contains("Invalid", errorDict["CronExpression"]);
        }

        #endregion

        #region GetSchedule

        [Fact]
        public async Task GetSchedule_WithValidId_ReturnsSchedule()
        {
            // Arrange
            var id = Guid.NewGuid();
            var schedule = new ScheduleResponseDto { Id = id, Name = "Test" };
            _mockService.Setup(s => s.GetScheduleByIdAsync(id)).ReturnsAsync(schedule);

            // Act
            var result = await _controller.GetScheduleById(id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<ScheduleResponseDto>(ok.Value);
            Assert.Equal(id, value.Id);
        }

        [Theory]
        [InlineData("InvalidId")]
        [InlineData("CrossTenant")]
        public async Task GetSchedule_ReturnsNotFound(string testCase)
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.GetScheduleByIdAsync(id)).ReturnsAsync((ScheduleResponseDto?)null);

            // Act
            var result = await _controller.GetScheduleById(id);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region GetAllSchedules

        [Fact]
        public async Task GetAllSchedules_FiltersByTenant_ReturnsCorrectSchedules()
        {
            // Arrange
            var parameters = new ScheduleQueryParameters { PageNumber = 1, PageSize = 10 };
            var paged = new PagedResult<ScheduleResponseDto>
            {
                Items = new List<ScheduleResponseDto> { new ScheduleResponseDto { Id = Guid.NewGuid(), Name = "Test" } },
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 1
            };
            _mockService.Setup(s => s.GetTenantSchedulesAsync(parameters)).ReturnsAsync(paged);

            // Act
            var result = await _controller.GetSchedules(parameters);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<PagedResult<ScheduleResponseDto>>(ok.Value);
            Assert.Single(value.Items);
        }

        #endregion

        #region UpdateSchedule

        [Fact]
        public async Task UpdateSchedule_WithValidData_ReturnsUpdated()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateScheduleDto { Name = "Updated", Type = ScheduleType.Recurring };
            var updated = new ScheduleResponseDto { Id = id, Name = "Updated" };
            _mockService.Setup(s => s.UpdateScheduleAsync(id, dto)).ReturnsAsync(updated);

            // Act
            var result = await _controller.UpdateSchedule(id, dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<ScheduleResponseDto>(ok.Value);
            Assert.Equal("Updated", value.Name);
        }

        [Fact]
        public async Task UpdateSchedule_CrossTenant_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateScheduleDto { Name = "Updated", Type = ScheduleType.Recurring };
            _mockService.Setup(s => s.UpdateScheduleAsync(id, dto)).ReturnsAsync((ScheduleResponseDto?)null);

            // Act
            var result = await _controller.UpdateSchedule(id, dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region DeleteSchedule

        [Fact]
        public async Task DeleteSchedule_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.DeleteScheduleAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteSchedule(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteSchedule_CrossTenant_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.DeleteScheduleAsync(id)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteSchedule(id);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region ScheduleManagement

        [Fact]
        public async Task EnableSchedule_WithValidId_ReturnsOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.ResumeScheduleAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _controller.ResumeSchedule(id);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DisableSchedule_WithValidId_ReturnsOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.PauseScheduleAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _controller.PauseSchedule(id);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        #endregion
    }
}