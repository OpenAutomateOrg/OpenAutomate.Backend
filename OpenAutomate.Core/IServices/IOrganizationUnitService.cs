using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.OrganizationUnit;

namespace OpenAutomate.Core.IServices
{
    public interface IOrganizationUnitService
    {
        /// <summary>
        /// Creates a new organization unit with default authorities and assigns the creator as OWNER
        /// </summary>
        Task<OrganizationUnitResponseDto> CreateOrganizationUnitAsync(CreateOrganizationUnitDto dto, Guid userId);
        
        /// <summary>
        /// Gets an organization unit by its ID
        /// </summary>
        Task<OrganizationUnitResponseDto> GetOrganizationUnitByIdAsync(Guid id);
        
        /// <summary>
        /// Gets an organization unit by its slug
        /// </summary>
        Task<OrganizationUnitResponseDto> GetOrganizationUnitBySlugAsync(string slug);
        
        /// <summary>
        /// Gets all organization units
        /// </summary>
        Task<IEnumerable<OrganizationUnitResponseDto>> GetAllOrganizationUnitsAsync();
        
        /// <summary>
        /// Updates an organization unit
        /// </summary>
        Task<OrganizationUnitResponseDto> UpdateOrganizationUnitAsync(Guid id, CreateOrganizationUnitDto dto);
        
        /// <summary>
        /// Checks the impact of changing an organization unit's name
        /// </summary>
        Task<SlugChangeWarningDto> CheckNameChangeImpactAsync(Guid id, string newName);
        
        /// <summary>
        /// Generates a slug from the organization unit name
        /// </summary>
        string GenerateSlugFromName(string name);

        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>The user entity if found, otherwise null.</returns>
        Task<User> GetUserByEmailAsync(string email);

        /// <summary>
        /// Adds a user to a specific organization unit.
        /// </summary>
        /// <param name="ouId">The ID of the organization unit.</param>
        /// <param name="userId">The ID of the user to add.</param>
        /// <returns>True if the user was added successfully, otherwise false.</returns>
        Task<bool> AddUserToOrganizationUnitAsync(Guid ouId, Guid userId);

        /// <summary>
        /// Creates an invitation for a user to join an organization unit.
        /// </summary>
        /// <param name="ouId">The ID of the organization unit.</param>
        /// <param name="email">The email address of the user to invite.</param>
        /// <returns>True if the invitation was created successfully, otherwise false.</returns>
        Task<bool> CreateInvitationAsync(Guid ouId, string email);

        /// <summary>
        /// Sends a notification email to a user.
        /// </summary>
        /// <param name="email">The recipient's email address.</param>
        /// <param name="message">The message content of the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendNotificationEmailAsync(string email, string message);

        /// <summary>
        /// Sends an invitation email to a user to join an organization unit.
        /// </summary>
        /// <param name="ouId">The ID of the organization unit.</param>
        /// <param name="email">The recipient's email address.</param>
        /// <param name="organizationUnitName">The name of the organization unit.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendInvitationEmailAsync(Guid ouId, string email, string organizationUnitName);
    }
}