using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class OrganizationUnitTest
    {
        [Fact]
        public void OrganizationUnit_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var orgUnit = new OrganizationUnit();

            // Assert
            Assert.NotNull(orgUnit);
            Assert.Equal(string.Empty, orgUnit.Name);
            Assert.Equal(string.Empty, orgUnit.Description);
            Assert.Equal(string.Empty, orgUnit.Slug);
            Assert.True(orgUnit.IsActive);
            Assert.NotNull(orgUnit.OrganizationUnitUsers);
            Assert.Empty(orgUnit.OrganizationUnitUsers);
            Assert.NotNull(orgUnit.BotAgents);
            Assert.Empty(orgUnit.BotAgents);
            Assert.NotNull(orgUnit.AutomationPackages);
            Assert.Empty(orgUnit.AutomationPackages);
            Assert.NotNull(orgUnit.Executions);
            Assert.Empty(orgUnit.Executions);
            Assert.NotNull(orgUnit.Schedules);
            Assert.Empty(orgUnit.Schedules);
        }
        [Fact]
        public void OrganizationUnit_SetName_NameIsSet()
        {
            // Arrange
            var orgUnit = new OrganizationUnit();
            var name = "Test Organization";

            // Act
            orgUnit.Name = name;

            // Assert
            Assert.Equal(name, orgUnit.Name);
        }
        [Fact]
        public void OrganizationUnit_AddOrganizationUnitUser_UserIsAdded()
        {
            // Arrange
            var orgUnit = new OrganizationUnit();
            var user = new OrganizationUnitUser { UserId = Guid.NewGuid() };

            // Act
            orgUnit.OrganizationUnitUsers.Add(user);

            // Assert
            Assert.NotNull(orgUnit.OrganizationUnitUsers);
            Assert.Contains(user, orgUnit.OrganizationUnitUsers);
            Assert.Single(orgUnit.OrganizationUnitUsers);
        }
        [Fact]
        public void OrganizationUnit_AddBotAgent_BotAgentIsAdded()
        {
            // Arrange
            var orgUnit = new OrganizationUnit();
            var botAgent = new BotAgent { Name = "Test Bot" };

            // Act
            orgUnit.BotAgents.Add(botAgent);

            // Assert
            Assert.NotNull(orgUnit.BotAgents);
            Assert.Contains(botAgent, orgUnit.BotAgents);
            Assert.Single(orgUnit.BotAgents);
        }
        [Fact]
        public void OrganizationUnit_AddAutomationPackage_PackageIsAdded()
        {
            // Arrange
            var orgUnit = new OrganizationUnit();
            var package = new AutomationPackage { Name = "Test Package" };

            // Act
            orgUnit.AutomationPackages.Add(package);

            // Assert
            Assert.NotNull(orgUnit.AutomationPackages);
            Assert.Contains(package, orgUnit.AutomationPackages);
            Assert.Single(orgUnit.AutomationPackages);
        }
        [Fact]
        public void OrganizationUnit_AddExecution_ExecutionIsAdded()
        {
            // Arrange
            var orgUnit = new OrganizationUnit();
            var execution = new Execution { Status = "Completed" };

            // Act
            orgUnit.Executions.Add(execution);

            // Assert
            Assert.NotNull(orgUnit.Executions);
            Assert.Contains(execution, orgUnit.Executions);
            Assert.Single(orgUnit.Executions);
        }
        [Fact]
        public void OrganizationUnit_AddSchedule_ScheduleIsAdded()
        {
            // Arrange
            var orgUnit = new OrganizationUnit();
            var schedule = new Schedule { CronExpression = "0 0 * * *" };

            // Act
            orgUnit.Schedules.Add(schedule);

            // Assert
            Assert.NotNull(orgUnit.Schedules);
            Assert.Contains(schedule, orgUnit.Schedules);
            Assert.Single(orgUnit.Schedules);
        }

    }
}
