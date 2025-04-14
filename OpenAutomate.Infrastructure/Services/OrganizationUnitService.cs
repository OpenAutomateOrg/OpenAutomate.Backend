using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Services
{
    public class OrganizationUnitService : IOrganizationUnitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Random _random;

        public OrganizationUnitService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _random = new Random();
        }

        public async Task<OrganizationUnitResponseDto> CreateOrganizationUnitAsync(CreateOrganizationUnitDto dto, Guid userId)
        {
            // Check if name is already taken
            var existingOrgUnit = await _unitOfWork.OrganizationUnits.GetFirstOrDefaultAsync(o => o.Name == dto.Name);
            if (existingOrgUnit != null)
            {
                throw new InvalidOperationException($"Organization unit with name '{dto.Name}' already exists");
            }

            // Generate unique slug
            var slug = await GenerateUniqueSlugAsync(dto.Name);

            // Create new organization unit
            var organizationUnit = new OrganizationUnit
            {
                Name = dto.Name,
                Description = dto.Description,
                Slug = slug,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId.ToString()
            };

            await _unitOfWork.OrganizationUnits.AddAsync(organizationUnit);

            // Add the creating user as a member of the organization
            var organizationUnitUser = new OrganizationUnitUser
            {
                UserId = userId,
                OrganizationUnitId = organizationUnit.Id
            };

            await _unitOfWork.OrganizationUnitUsers.AddAsync(organizationUnitUser);
            await _unitOfWork.CompleteAsync();

            // Return the created organization unit
            return new OrganizationUnitResponseDto
            {
                Id = organizationUnit.Id,
                Name = organizationUnit.Name,
                Description = organizationUnit.Description,
                Slug = organizationUnit.Slug,
                IsActive = organizationUnit.IsActive,
                CreatedAt = organizationUnit.CreatedAt,
                UserCount = 1 // Initial user count is 1 (the creator)
            };
        }

        public async Task<OrganizationUnitResponseDto> GetOrganizationUnitByIdAsync(Guid organizationUnitId)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(organizationUnitId);
            if (organizationUnit == null)
            {
                throw new KeyNotFoundException($"Organization unit with ID {organizationUnitId} not found");
            }

            // Count users by getting all and counting in memory
            var users = await _unitOfWork.OrganizationUnitUsers.GetAllAsync();
            var userCount = users.Count(ou => ou.OrganizationUnitId == organizationUnitId);

            return new OrganizationUnitResponseDto
            {
                Id = organizationUnit.Id,
                Name = organizationUnit.Name,
                Description = organizationUnit.Description,
                Slug = organizationUnit.Slug,
                IsActive = organizationUnit.IsActive,
                CreatedAt = organizationUnit.CreatedAt,
                UserCount = userCount
            };
        }

        public async Task<OrganizationUnitResponseDto> GetOrganizationUnitBySlugAsync(string slug)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetFirstOrDefaultAsync(o => o.Slug == slug);
            if (organizationUnit == null)
            {
                throw new KeyNotFoundException($"Organization unit with slug '{slug}' not found");
            }

            // Count users by getting all and counting in memory
            var users = await _unitOfWork.OrganizationUnitUsers.GetAllAsync();
            var userCount = users.Count(ou => ou.OrganizationUnitId == organizationUnit.Id);

            return new OrganizationUnitResponseDto
            {
                Id = organizationUnit.Id,
                Name = organizationUnit.Name,
                Description = organizationUnit.Description,
                Slug = organizationUnit.Slug,
                IsActive = organizationUnit.IsActive,
                CreatedAt = organizationUnit.CreatedAt,
                UserCount = userCount
            };
        }

        public async Task<IEnumerable<OrganizationUnitResponseDto>> GetUserOrganizationUnitsAsync(Guid userId)
        {
            // Get all organization unit users and filter in memory
            var allOrgUnitUsers = await _unitOfWork.OrganizationUnitUsers.GetAllAsync();
            var orgUnitUsers = allOrgUnitUsers.Where(ou => ou.UserId == userId).ToList();

            var result = new List<OrganizationUnitResponseDto>();
            var allUsers = await _unitOfWork.OrganizationUnitUsers.GetAllAsync();

            foreach (var orgUnitUser in orgUnitUsers)
            {
                var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(orgUnitUser.OrganizationUnitId);
                if (organizationUnit != null && organizationUnit.IsActive)
                {
                    var userCount = allUsers.Count(ou => ou.OrganizationUnitId == organizationUnit.Id);
                    
                    result.Add(new OrganizationUnitResponseDto
                    {
                        Id = organizationUnit.Id,
                        Name = organizationUnit.Name,
                        Description = organizationUnit.Description,
                        Slug = organizationUnit.Slug,
                        IsActive = organizationUnit.IsActive,
                        CreatedAt = organizationUnit.CreatedAt,
                        UserCount = userCount
                    });
                }
            }

            return result;
        }

        public async Task<SlugChangeWarningDto> CheckNameChangeImpactAsync(Guid organizationUnitId, string newName)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(organizationUnitId);
            if (organizationUnit == null)
            {
                throw new KeyNotFoundException($"Organization unit with ID {organizationUnitId} not found");
            }

            var newSlug = await GenerateUniqueSlugAsync(newName);
            
            // If the slugs are the same, no impact
            if (newSlug == organizationUnit.Slug)
            {
                return new SlugChangeWarningDto
                {
                    CurrentName = organizationUnit.Name,
                    CurrentSlug = organizationUnit.Slug,
                    NewName = newName,
                    NewSlug = newSlug,
                    PotentialImpacts = Array.Empty<string>(),
                    RequiresConfirmation = false
                };
            }

            // Generate list of potential impacts
            var potentialImpacts = new List<string>
            {
                "Organization unit URL paths will change, breaking existing bookmarks",
                "API clients using the current slug will need to be updated",
                "Users may need to be notified about the new organization unit URL"
            };

            // Additional impacts based on organization unit's connected services
            var botAgents = await _unitOfWork.BotAgents.GetAllAsync();
            if (botAgents.Any(a => a.OrganizationUnitId == organizationUnitId))
            {
                potentialImpacts.Add("Connected bot agents may need reconfiguration");
            }
                
            var schedules = await _unitOfWork.Schedules.GetAllAsync();
            if (schedules.Any(s => s.OrganizationUnitId == organizationUnitId))
            {
                potentialImpacts.Add("Scheduled tasks referencing the organization unit may be affected");
            }

            return new SlugChangeWarningDto
            {
                CurrentName = organizationUnit.Name,
                CurrentSlug = organizationUnit.Slug,
                NewName = newName,
                NewSlug = newSlug,
                PotentialImpacts = potentialImpacts.ToArray(),
                RequiresConfirmation = true
            };
        }

        public async Task<OrganizationUnitResponseDto> UpdateOrganizationUnitAsync(Guid organizationUnitId, UpdateOrganizationUnitDto dto)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(organizationUnitId);
            if (organizationUnit == null)
            {
                throw new KeyNotFoundException($"Organization unit with ID {organizationUnitId} not found");
            }

            // If name is changing, check if we need to update the slug
            if (!string.IsNullOrEmpty(dto.Name) && dto.Name != organizationUnit.Name)
            {
                var slugChangeWarning = await CheckNameChangeImpactAsync(organizationUnitId, dto.Name);
                
                // If slug will change and confirmation is not provided
                if (slugChangeWarning.RequiresConfirmation && !dto.ConfirmSlugChange)
                {
                    throw new InvalidOperationException("Name change will affect the organization unit's URL. Confirmation is required.");
                }
                
                organizationUnit.Name = dto.Name;
                organizationUnit.Slug = slugChangeWarning.NewSlug;
            }

            // Update description if provided
            if (dto.Description != null)
            {
                organizationUnit.Description = dto.Description;
            }

            organizationUnit.LastModifyAt = DateTime.UtcNow;
            
            await _unitOfWork.CompleteAsync();

            // Count users
            var users = await _unitOfWork.OrganizationUnitUsers.GetAllAsync();
            var userCount = users.Count(ou => ou.OrganizationUnitId == organizationUnitId);

            return new OrganizationUnitResponseDto
            {
                Id = organizationUnit.Id,
                Name = organizationUnit.Name,
                Description = organizationUnit.Description,
                Slug = organizationUnit.Slug,
                IsActive = organizationUnit.IsActive,
                CreatedAt = organizationUnit.CreatedAt,
                UserCount = userCount
            };
        }

        public async Task<bool> DeactivateOrganizationUnitAsync(Guid organizationUnitId)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(organizationUnitId);
            if (organizationUnit == null)
            {
                throw new KeyNotFoundException($"Organization unit with ID {organizationUnitId} not found");
            }

            organizationUnit.IsActive = false;
            organizationUnit.LastModifyAt = DateTime.UtcNow;
            
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> AddUserToOrganizationUnitAsync(Guid organizationUnitId, Guid userId, string role = "Member")
        {
            // Check if organization unit exists
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(organizationUnitId);
            if (organizationUnit == null)
            {
                throw new KeyNotFoundException($"Organization unit with ID {organizationUnitId} not found");
            }

            // Check if user exists
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found");
            }

            // Check if user is already a member
            var allOrgUnitUsers = await _unitOfWork.OrganizationUnitUsers.GetAllAsync();
            var existingMembership = allOrgUnitUsers.FirstOrDefault(ou => 
                ou.OrganizationUnitId == organizationUnitId && ou.UserId == userId);
            
            if (existingMembership != null)
            {
                return true; // User is already a member
            }

            // Add user to organization unit
            var organizationUnitUser = new OrganizationUnitUser
            {
                UserId = userId,
                OrganizationUnitId = organizationUnitId
            };

            await _unitOfWork.OrganizationUnitUsers.AddAsync(organizationUnitUser);
            await _unitOfWork.CompleteAsync();
            
            return true;
        }

        public async Task<bool> RemoveUserFromOrganizationUnitAsync(Guid organizationUnitId, Guid userId)
        {
            // Get all organization unit users
            var allOrgUnitUsers = await _unitOfWork.OrganizationUnitUsers.GetAllAsync();
            
            // Check if the user is a member of the organization unit
            var membership = allOrgUnitUsers.FirstOrDefault(ou => 
                ou.OrganizationUnitId == organizationUnitId && ou.UserId == userId);
            
            if (membership == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} is not a member of organization unit with ID {organizationUnitId}");
            }

            // Check if this is the last user in the organization unit
            var memberCount = allOrgUnitUsers.Count(ou => ou.OrganizationUnitId == organizationUnitId);
            if (memberCount == 1)
            {
                throw new InvalidOperationException("Cannot remove the last user from an organization unit");
            }

            _unitOfWork.OrganizationUnitUsers.Remove(membership);
            await _unitOfWork.CompleteAsync();
            
            return true;
        }

        public async Task<IEnumerable<OrganizationUnitUserDto>> GetOrganizationUnitUsersAsync(Guid organizationUnitId)
        {
            // Check if organization unit exists
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(organizationUnitId);
            if (organizationUnit == null)
            {
                throw new KeyNotFoundException($"Organization unit with ID {organizationUnitId} not found");
            }

            // Get all users in the organization unit
            var allOrgUnitUsers = await _unitOfWork.OrganizationUnitUsers.GetAllAsync();
            var memberships = allOrgUnitUsers.Where(ou => ou.OrganizationUnitId == organizationUnitId).ToList();

            var result = new List<OrganizationUnitUserDto>();

            foreach (var membership in memberships)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(membership.UserId);
                if (user != null)
                {
                    result.Add(new OrganizationUnitUserDto
                    {
                        UserId = user.Id,
                        UserName = user.FirstName +" "+ user.LastName,
                        Email = user.Email,
                        Role = "Member" // Default role for now
                    });
                }
            }

            return result;
        }

        public async Task<bool> UserHasAccessToOrganizationUnitAsync(Guid organizationUnitId, Guid userId)
        {
            // Get all organization unit users
            var allOrgUnitUsers = await _unitOfWork.OrganizationUnitUsers.GetAllAsync();
            
            // Check if the user is a member of the organization unit
            var membership = allOrgUnitUsers.FirstOrDefault(ou => 
                ou.OrganizationUnitId == organizationUnitId && ou.UserId == userId);
            
            return membership != null;
        }

        private async Task<string> GenerateUniqueSlugAsync(string name)
        {
            // Generate base slug
            var slug = GenerateSlug(name);
            
            // Check if slug already exists
            var existingOrgUnit = await _unitOfWork.OrganizationUnits.GetFirstOrDefaultAsync(o => o.Slug == slug);
            if (existingOrgUnit == null)
            {
                return slug; // Slug is unique
            }
            
            // Add random suffix to ensure uniqueness
            var randomSuffix = GenerateRandomString(6);
            return $"{slug}{randomSuffix}";
        }

        private string GenerateSlug(string text)
        {
            // Convert to lowercase
            text = text.ToLowerInvariant();
            
            // Remove diacritics (accents)
            text = Regex.Replace(text, @"[^a-z0-9\s]", "");
            
            // Replace spaces with empty string
            text = Regex.Replace(text, @"\s+", "");
            
            return text;
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }
    }
} 