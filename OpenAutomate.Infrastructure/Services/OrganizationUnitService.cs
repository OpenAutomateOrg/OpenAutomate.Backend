using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.Utilities;

namespace OpenAutomate.Infrastructure.Services
{
    public class OrganizationUnitService : IOrganizationUnitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrganizationUnitService> _logger;
        private readonly IEmailService _emailService;

        public OrganizationUnitService(IUnitOfWork unitOfWork, ILogger<OrganizationUnitService> logger, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }


        public async Task<OrganizationUnitResponseDto> CreateOrganizationUnitAsync(CreateOrganizationUnitDto dto, Guid userId)
        {
            try
            {
                // Generate slug if not provided
                string slug = !string.IsNullOrWhiteSpace(dto.Slug) 
                    ? dto.Slug 
                    : GenerateSlugFromName(dto.Name);

                // Ensure slug is unique
                slug = await EnsureUniqueSlugAsync(slug);

                // Create organization unit
                var organizationUnit = new OrganizationUnit
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Slug = slug,
                    IsActive = true
                };

                await _unitOfWork.OrganizationUnits.AddAsync(organizationUnit);
                await _unitOfWork.CompleteAsync();

                // Create default authorities for the organization unit
                await CreateDefaultAuthoritiesAsync(organizationUnit.Id);
                
                // Assign the OWNER authority to the user who created the organization unit
                await AssignOwnerAuthorityToUserAsync(organizationUnit.Id, userId);

                // Return response
                return new OrganizationUnitResponseDto
                {
                    Id = organizationUnit.Id,
                    Name = organizationUnit.Name,
                    Description = organizationUnit.Description,
                    Slug = organizationUnit.Slug,
                    IsActive = organizationUnit.IsActive,
                    CreatedAt = organizationUnit.CreatedAt ?? DateTime.Now,
                    UpdatedAt = organizationUnit.LastModifyAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization unit: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<OrganizationUnitResponseDto> GetOrganizationUnitByIdAsync(Guid id)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(id);
            if (organizationUnit == null)
                return null;

            return MapToResponseDto(organizationUnit);
        }

        public async Task<OrganizationUnitResponseDto> GetOrganizationUnitBySlugAsync(string slug)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetFirstOrDefaultAsync(o => o.Slug == slug);
            if (organizationUnit == null)
                return null;

            return MapToResponseDto(organizationUnit);
        }

        public async Task<IEnumerable<OrganizationUnitResponseDto>> GetAllOrganizationUnitsAsync()
        {
            var organizationUnits = await _unitOfWork.OrganizationUnits.GetAllAsync();
            return organizationUnits.Select(MapToResponseDto);
        }

        public async Task<OrganizationUnitResponseDto> UpdateOrganizationUnitAsync(Guid id, CreateOrganizationUnitDto dto)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(id);
            if (organizationUnit == null)
                throw new KeyNotFoundException($"Organization unit with ID {id} not found");

            string newSlug = dto.Slug;
            bool generateNewSlug = string.IsNullOrWhiteSpace(dto.Slug) && dto.Name != organizationUnit.Name;

            // Generate new slug if name changed and slug not explicitly provided
            if (generateNewSlug)
            {
                newSlug = GenerateSlugFromName(dto.Name);
            }

            // If slug changed, ensure it's unique
            if (newSlug != organizationUnit.Slug)
            {
                newSlug = await EnsureUniqueSlugAsync(newSlug, id);
            }

            // Update organization unit
            organizationUnit.Name = dto.Name;
            organizationUnit.Description = dto.Description;
            if (newSlug != organizationUnit.Slug)
            {
                organizationUnit.Slug = newSlug;
            }

            await _unitOfWork.CompleteAsync();

            return MapToResponseDto(organizationUnit);
        }

        public async Task<SlugChangeWarningDto> CheckNameChangeImpactAsync(Guid id, string newName)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(id);
            if (organizationUnit == null)
                throw new KeyNotFoundException($"Organization unit with ID {id} not found");

            var currentSlug = organizationUnit.Slug;
            var proposedSlug = GenerateSlugFromName(newName);

            // Check if slug would change
            if (currentSlug == proposedSlug)
            {
                return new SlugChangeWarningDto
                {
                    CurrentSlug = currentSlug,
                    ProposedSlug = proposedSlug,
                    IsChangeSignificant = false,
                    PotentialImpacts = Array.Empty<string>(),
                    RequiresConfirmation = false
                };
            }

            // For a significant change with potential impact
            return new SlugChangeWarningDto
            {
                CurrentSlug = currentSlug,
                ProposedSlug = proposedSlug,
                IsChangeSignificant = true,
                PotentialImpacts = new string[]
                {
                    "URL paths will change",
                    "Bookmarks to this organization may no longer work",
                    "API integrations using the current slug will need to be updated"
                },
                RequiresConfirmation = true
            };
        }

        public string GenerateSlugFromName(string name)
        {
            return SlugGenerator.GenerateSlug(name);
        }
        
        private async Task<string> EnsureUniqueSlugAsync(string baseSlug, Guid? excludeId = null)
        {
            bool SlugExists(string slug)
            {
                if (excludeId.HasValue)
                {
                    return _unitOfWork.OrganizationUnits
                        .GetFirstOrDefaultAsync(o => o.Slug == slug && o.Id != excludeId.Value)
                        .GetAwaiter()
                        .GetResult() != null;
                }
                else
                {
                    return _unitOfWork.OrganizationUnits
                        .GetFirstOrDefaultAsync(o => o.Slug == slug)
                        .GetAwaiter()
                        .GetResult() != null;
                }
            }
            
            return SlugGenerator.EnsureUniqueSlug(baseSlug, SlugExists);
        }

        private OrganizationUnitResponseDto MapToResponseDto(OrganizationUnit organizationUnit)
        {
            return new OrganizationUnitResponseDto
            {
                Id = organizationUnit.Id,
                Name = organizationUnit.Name,
                Description = organizationUnit.Description,
                Slug = organizationUnit.Slug,
                IsActive = organizationUnit.IsActive,
                CreatedAt = organizationUnit.CreatedAt ?? DateTime.Now,
                UpdatedAt = organizationUnit.LastModifyAt
            };
        }

        private async Task CreateDefaultAuthoritiesAsync(Guid organizationUnitId)
        {
            // Define the default authority names and their resource permissions
            var defaultAuthorities = new Dictionary<string, Dictionary<string, int>>
            {
                {
                    "OWNER", new Dictionary<string, int>
                    {
                        { Resources.AssetResource, Permissions.Delete },
                        { Resources.AgentResource, Permissions.Delete },
                        { Resources.PackageResource, Permissions.Delete },
                        { Resources.ScheduleResource, Permissions.Delete },
                        { Resources.ExecutionResource, Permissions.Delete },
                        { Resources.UserResource, Permissions.Delete },
                        { Resources.OrganizationUnitResource, Permissions.Delete }
                    }
                },
                {
                    "OPERATOR", new Dictionary<string, int>
                    {
                        { Resources.AssetResource, Permissions.Delete },
                        { Resources.AgentResource, Permissions.Delete },
                        { Resources.PackageResource, Permissions.Delete },
                        { Resources.ScheduleResource, Permissions.Delete },
                        { Resources.ExecutionResource, Permissions.Delete },
                        { Resources.UserResource, Permissions.Update },
                        { Resources.OrganizationUnitResource, Permissions.Update }
                    }
                },
                {
                    "DEVELOPER", new Dictionary<string, int>
                    {
                        { Resources.AssetResource, Permissions.Update },
                        { Resources.AgentResource, Permissions.Update },
                        { Resources.PackageResource, Permissions.Update },
                        { Resources.ScheduleResource, Permissions.Update },
                        { Resources.ExecutionResource, Permissions.Update },
                        { Resources.UserResource, Permissions.View },
                        { Resources.OrganizationUnitResource, Permissions.View }
                    }
                },
                {
                    "USER", new Dictionary<string, int>
                    {
                        { Resources.AssetResource, Permissions.View },
                        { Resources.AgentResource, Permissions.View },
                        { Resources.PackageResource, Permissions.View },
                        { Resources.ScheduleResource, Permissions.View },
                        { Resources.ExecutionResource, Permissions.View },
                        { Resources.UserResource, Permissions.View },
                        { Resources.OrganizationUnitResource, Permissions.View }
                    }
                }
            };

            foreach (var authority in defaultAuthorities)
            {
                // Create the authority
                var newAuthority = new Authority
                {
                    Name = authority.Key,
                    OrganizationUnitId = organizationUnitId
                };

                await _unitOfWork.Authorities.AddAsync(newAuthority);
                await _unitOfWork.CompleteAsync();

                // Create the resource permissions for this authority
                foreach (var resource in authority.Value)
                {
                    var authorityResource = new AuthorityResource
                    {
                        AuthorityId = newAuthority.Id,
                        OrganizationUnitId = organizationUnitId,
                        ResourceName = resource.Key,
                        Permission = resource.Value
                    };

                    await _unitOfWork.AuthorityResources.AddAsync(authorityResource);
                }
            }

            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Created default authorities for organization unit {OrganizationUnitId}", organizationUnitId);
        }

        private async Task AssignOwnerAuthorityToUserAsync(Guid organizationUnitId, Guid userId)
        {
            try
            {
                // Find the OWNER authority for this organization unit
                var ownerAuthority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                    a => a.OrganizationUnitId == organizationUnitId && a.Name == "OWNER");
                
                if (ownerAuthority == null)
                {
                    _logger.LogError("OWNER authority not found for organization unit {OrganizationUnitId}", organizationUnitId);
                    throw new InvalidOperationException($"OWNER authority not found for organization unit {organizationUnitId}");
                }
                
                // Create the user-authority association
                var userAuthority = new UserAuthority
                {
                    UserId = userId,
                    AuthorityId = ownerAuthority.Id,
                    OrganizationUnitId = organizationUnitId
                };
                
                await _unitOfWork.UserAuthorities.AddAsync(userAuthority);
                await _unitOfWork.CompleteAsync();
                
                _logger.LogInformation("User {UserId} assigned as OWNER of organization unit {OrganizationUnitId}", 
                    userId, organizationUnitId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning OWNER authority to user {UserId} for organization unit {OrganizationUnitId}", 
                    userId, organizationUnitId);
                throw;
            }
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _unitOfWork.Users.GetFirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> AddUserToOrganizationUnitAsync(Guid ouId, Guid userId)
        {
            if (await _unitOfWork.OrganizationUnitUsers.AnyAsync(ouu => ouu.OrganizationUnitId == ouId && ouu.UserId == userId))
                return false;

            var organizationUnitUser = new OrganizationUnitUser
            {
                OrganizationUnitId = ouId,
                UserId = userId,
                Role = "Member"
            };

            await _unitOfWork.OrganizationUnitUsers.AddAsync(organizationUnitUser);
            return await _unitOfWork.CompleteAsync() > 0;
        }

        public async Task<bool> CreateInvitationAsync(Guid ouId, string email)
        {
            var existingInvitation = await _unitOfWork.OrganizationUnitInvitations
                .GetFirstOrDefaultAsync(inv => inv.OrganizationUnitId == ouId && inv.Email == email && inv.Status == "Pending");

            if (existingInvitation != null)
                return false;

            var invitation = new OrganizationUnitInvitation
            {
                OrganizationUnitId = ouId,
                Email = email,
                Status = "Pending",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _unitOfWork.OrganizationUnitInvitations.AddAsync(invitation);
            return await _unitOfWork.CompleteAsync() > 0;
        }

        public async Task SendNotificationEmailAsync(string email, string message)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("Email address is required", nameof(email));

            var subject = "Notification: Added to Organization Unit";
            var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #0078d4; color: white; padding: 10px 20px; text-align: center; }}
                            .content {{ padding: 20px; border: 1px solid #ddd; border-top: none; }}
                            .footer {{ margin-top: 20px; font-size: 12px; color: #777; text-align: center; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>Notification</h2>
                            </div>
                            <div class='content'>
                                <p>{message}</p>
                            </div>
                            <div class='footer'>
                                <p>© 2023 OpenAutomate. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

            await _emailService.SendEmailAsync(email, subject, body, isHtml: true);
        }

        public async Task SendInvitationEmailAsync(Guid ouId, string email, string organizationUnitName)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("Email address is required", nameof(email));

            var registrationLink = $"https://your-app.com/register?email={email}";
            var subject = "You are invited to join an Organization Unit";
            var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #0078d4; color: white; padding: 10px 20px; text-align: center; }}
                            .content {{ padding: 20px; border: 1px solid #ddd; border-top: none; }}
                            .footer {{ margin-top: 20px; font-size: 12px; color: #777; text-align: center; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>Invitation to Join</h2>
                            </div>
                            <div class='content'>
                                <p>Hello,</p>
                                <p>You have been invited to join the organization unit: <strong>{organizationUnitName}</strong>.</p>
                                <p>Please click the link below to register:</p>
                                <p><a href='{registrationLink}' style='color: #0078d4;'>Register Now</a></p>
                                <p>This link will expire in 7 days.</p>
                            </div>
                            <div class='footer'>
                                <p>© 2023 OpenAutomate. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

            await _emailService.SendEmailAsync(email, subject, body, isHtml: true);
        }
    }
} 