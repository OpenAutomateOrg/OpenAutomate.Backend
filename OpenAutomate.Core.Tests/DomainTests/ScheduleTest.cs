using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class ScheduleTest
    {
        [Fact]
        public void Schedule_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var schedule = new Schedule();

            // Assert
            Assert.NotNull(schedule);
            Assert.Equal(string.Empty, schedule.CronExpression);
            Assert.False(schedule.IsActive);
            Assert.Equal(Guid.Empty, schedule.PackageId);
            Assert.Equal(Guid.Empty, schedule.CreatedById);
            Assert.Null(schedule.Package);
            Assert.Null(schedule.User);
            Assert.Null(schedule.Executions);
        }
        [Fact]
        public void Schedule_SetCronExpression_CronExpressionIsSet()
        {
            // Arrange
            var schedule = new Schedule();
            var cronExpression = "0 0 * * *"; // Example cron expression for daily execution

            // Act
            schedule.CronExpression = cronExpression;

            // Assert
            Assert.Equal(cronExpression, schedule.CronExpression);
        }
        [Fact]
        public void Schedule_LinkAutomationPackage_PackageIsLinked()
        {
            // Arrange
            var package = new AutomationPackage { Name = "Test Package", Description = "Test Description" };
            var schedule = new Schedule { Package = package };

            // Act
            var linkedPackage = schedule.Package;

            // Assert
            Assert.NotNull(linkedPackage);
            Assert.Equal("Test Package", linkedPackage.Name);
            Assert.Equal("Test Description", linkedPackage.Description);
        }
      
        [Fact]
        public void Schedule_LinkUser_UserIsLinked()
        {
            // Arrange
            var user = new User { FirstName = "John", LastName = "Doe" };
            var schedule = new Schedule { User = user };

            // Act
            var linkedUser = schedule.User;

            // Assert
            Assert.NotNull(linkedUser);
            Assert.Equal("John", linkedUser.FirstName);
            Assert.Equal("Doe", linkedUser.LastName);
        }
        [Fact]
        public void Schedule_AddExecutions_ExecutionsAreAdded()
        {
            // Arrange
            var execution1 = new Execution { Status = "Completed" };
            var execution2 = new Execution { Status = "Failed" };
            var schedule = new Schedule { Executions = new List<Execution>() };

            // Act
            schedule.Executions.Add(execution1);
            schedule.Executions.Add(execution2);

            // Assert
            Assert.NotNull(schedule.Executions);
            Assert.Contains(execution1, schedule.Executions);
            Assert.Contains(execution2, schedule.Executions);
            Assert.Equal(2, schedule.Executions.Count);
        }

    }
}