using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.API.Tests.Controllers
{
    public class OrganizationUnitControllerTests
    {
        private readonly Mock<IOrganizationUnitService> _mockOrgUnitService;
        private readonly OrganizationUnitController _controller;

        public OrganizationUnitControllerTests()
        {
            _mockOrgUnitService = new Mock<IOrganizationUnitService>();
            _controller = new OrganizationUnitController(_mockOrgUnitService.Object);
        }

        [Fact]
        public async Task Create_WithValidRequest_ReturnsCreatedWithOrganizationUnit()
        {
            // Arrange
            var createDto = new CreateOrganizationUnitDto
            {
                Name = "Test Organization",
                Description = "Test Description"
            };
            var expectedResponse = new OrganizationUnitResponseDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Organization",
                Description = "Test Description",
                Slug = "test-organization",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var userId = Guid.NewGuid();
            var currentUser = new User { Id = userId };

            _mockOrgUnitService.Setup(s => s.CreateOrganizationUnitAsync(createDto, userId))
                .ReturnsAsync(expectedResponse);

            // Mock current user
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.HttpContext.Items["User"] = currentUser;

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnValue = Assert.IsType<OrganizationUnitResponseDto>(createdResult.Value);
            Assert.Equal(expectedResponse.Id, returnValue.Id);
            Assert.Equal(expectedResponse.Name, returnValue.Name);
            Assert.Equal(expectedResponse.Description, returnValue.Description);
            Assert.Equal(expectedResponse.Slug, returnValue.Slug);
            Assert.Equal(expectedResponse.IsActive, returnValue.IsActive);
            _mockOrgUnitService.Verify(s => s.CreateOrganizationUnitAsync(createDto, userId), Times.Once);
        }

        [Fact]
        public async Task Create_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateOrganizationUnitDto
            {
                Name = "", // Invalid name
                Description = "Test Description"
            };

            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _mockOrgUnitService.Verify(s => s.CreateOrganizationUnitAsync(It.IsAny<CreateOrganizationUnitDto>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Create_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var createDto = new CreateOrganizationUnitDto
            {
                Name = "Test Organization",
                Description = "Test Description"
            };

            var userId = Guid.NewGuid();
            var currentUser = new User { Id = userId };

            _mockOrgUnitService.Setup(s => s.CreateOrganizationUnitAsync(createDto, userId))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Mock current user
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.HttpContext.Items["User"] = currentUser;

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Contains("An error occurred while creating the organization unit", objectResult.Value.ToString());
            _mockOrgUnitService.Verify(s => s.CreateOrganizationUnitAsync(createDto, userId), Times.Once);
        }

        [Fact]
        public async Task GetMyOrganizationUnits_WithValidUser_ReturnsOrganizationUnits()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUser = new User { Id = userId };
            var expectedResponse = new UserOrganizationUnitsResponseDto
            {
                Count = 2,
                OrganizationUnits = new List<OrganizationUnitResponseDto>
                {
                    new OrganizationUnitResponseDto
                    {
                        Id = Guid.NewGuid(),
                        Name = "Org 1",
                        Description = "Description 1",
                        Slug = "org-1",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new OrganizationUnitResponseDto
                    {
                        Id = Guid.NewGuid(),
                        Name = "Org 2",
                        Description = "Description 2",
                        Slug = "org-2",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            _mockOrgUnitService.Setup(s => s.GetUserOrganizationUnitsAsync(userId))
                .ReturnsAsync(expectedResponse);

            // Mock current user
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.HttpContext.Items["User"] = currentUser;

            // Act
            var result = await _controller.GetMyOrganizationUnits();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<UserOrganizationUnitsResponseDto>(okResult.Value);
            Assert.Equal(expectedResponse.Count, returnValue.Count);
            Assert.Equal(2, returnValue.OrganizationUnits.Count());
            _mockOrgUnitService.Verify(s => s.GetUserOrganizationUnitsAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetMyOrganizationUnits_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUser = new User { Id = userId };

            _mockOrgUnitService.Setup(s => s.GetUserOrganizationUnitsAsync(userId))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Mock current user
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.HttpContext.Items["User"] = currentUser;

            // Act
            var result = await _controller.GetMyOrganizationUnits();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Contains("An error occurred while retrieving your organization units", objectResult.Value.ToString());
            _mockOrgUnitService.Verify(s => s.GetUserOrganizationUnitsAsync(userId), Times.Once);
        }
    }
}
