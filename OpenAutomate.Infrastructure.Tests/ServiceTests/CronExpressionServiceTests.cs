using System;
using System.Collections.Generic;
using Xunit;
using Moq;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class CronExpressionServiceTests
    {
        private readonly Mock<ICronExpressionService> _mockService;

        public CronExpressionServiceTests()
        {
            _mockService = new Mock<ICronExpressionService>();
        }

        // Cron Validation
        [Theory]
        [InlineData("0 0 * * *", true)]
        [InlineData("invalid-cron", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        public void ValidateCronExpression_ReturnsExpected(string cron, bool expected)
        {
            _mockService.Setup(s => s.IsValid(cron)).Returns(expected);
            var result = _mockService.Object.IsValid(cron);
            Assert.Equal(expected, result);
        }

        // Next Execution Calculation
        [Fact]
        public void GetNextExecutionTime_WithValidCron_ReturnsCorrectTime()
        {
            var cron = "0 0 * * *";
            var now = DateTime.UtcNow;
            var expected = now.Date.AddDays(1);
            _mockService.Setup(s => s.GetNextExecution(cron, now)).Returns(expected);
            var result = _mockService.Object.GetNextExecution(cron, now);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetNextExecutionTime_WithInvalidCron_ThrowsException()
        {
            var cron = "invalid-cron";
            _mockService.Setup(s => s.GetNextExecution(cron, null)).Throws(new FormatException("Invalid cron expression"));
            Assert.Throws<FormatException>(() => _mockService.Object.GetNextExecution(cron, null));
        }

        [Fact]
        public void GetNextExecutionTimes_WithValidCron_ReturnsMultipleTimes()
        {
            var cron = "0 0 * * *";
            var now = DateTime.UtcNow;
            var expected = new List<DateTime> { now.Date.AddDays(1), now.Date.AddDays(2) };
            _mockService.Setup(s => s.GetNextExecutions(cron, 2, now)).Returns(expected);
            var result = _mockService.Object.GetNextExecutions(cron, 2, now);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetNextExecutionTimes_WithCount_ReturnsCorrectCount()
        {
            var cron = "0 0 * * *";
            var now = DateTime.UtcNow;
            var expected = new List<DateTime> { now.Date.AddDays(1), now.Date.AddDays(2), now.Date.AddDays(3) };
            _mockService.Setup(s => s.GetNextExecutions(cron, 3, now)).Returns(expected);
            var result = _mockService.Object.GetNextExecutions(cron, 3, now);
            Assert.Equal(3, result.Count);
        }

        // Cron Description
        [Fact]
        public void GetCronDescription_WithValidExpression_ReturnsDescription()
        {
            var cron = "0 0 * * *";
            var expected = "At 12:00 AM every day";
            _mockService.Setup(s => s.GetDescription(cron)).Returns(expected);
            var result = _mockService.Object.GetDescription(cron);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetCronDescription_WithInvalidExpression_ThrowsException()
        {
            var cron = "invalid-cron";
            _mockService.Setup(s => s.GetDescription(cron)).Throws(new FormatException("Invalid cron expression"));
            Assert.Throws<FormatException>(() => _mockService.Object.GetDescription(cron));
        }
    }
}
