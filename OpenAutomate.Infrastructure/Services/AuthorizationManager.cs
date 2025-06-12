using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Dto.Authority;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace OpenAutomate.Infrastructure.Services
{
    public class AuthorizationManager : IAuthorizationManager
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenantContext;
        
        public AuthorizationManager(IUnitOfWork unitOfWork, ITenantContext tenantContext)
        {
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
        }
        
        #region Permission Checking
        
        public async Task<bool> HasPermissionAsync(Guid userId, string resourceName, int permission)
        {
            // Get user's authorities
            var userAuthorities = await GetUserAuthoritiesWithDetailAsync(userId);
            if (!userAuthorities.Any())
                return false;
                
            var authorityIds = userAuthorities.Select(ua => ua.Id);
            
            // Check if any of user's authorities has the required permission for the resource
            var hasPermission = await _unitOfWork.AuthorityResources.GetAllAsync(
                ar => authorityIds.Contains(ar.AuthorityId) && 
                      ar.ResourceName == resourceName && 
                      ar.Permission >= permission);
                      
            return hasPermission.Any();
        }
        
        public async Task<bool> HasAuthorityAsync(Guid userId, string authorityName)
        {
            if (!_tenantContext.HasTenant)
                return false;
                
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                a => a.Name == authorityName && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (authority == null)
                return false;
                
            var userAuthority = await _unitOfWork.UserAuthorities.GetFirstOrDefaultAsync(
                ua => ua.UserId == userId && ua.AuthorityId == authority.Id);
                
            return userAuthority != null;
        }
        
        #endregion
        
        #region Authority Management
        
        public async Task<AuthorityWithPermissionsDto> CreateAuthorityAsync(CreateAuthorityDto dto)
        {
            if (!_tenantContext.HasTenant)
                throw new InvalidOperationException("Tenant context is required for authority creation");
                
            // Validate input
            if (!Permissions.IsValid(dto.ResourcePermissions.Max(rp => rp.Permission)))
                throw new ArgumentException("Invalid permission level");
            
            // Check if authority name already exists in current tenant
            var existingAuthority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                a => a.Name == dto.Name && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (existingAuthority != null)
                throw new InvalidOperationException($"Authority with name '{dto.Name}' already exists");
            
            // Create new authority
            var authority = new Authority
            {
                Name = dto.Name,
                Description = dto.Description,
                IsSystemAuthority = false,
                OrganizationUnitId = _tenantContext.CurrentTenantId
            };
            
            await _unitOfWork.Authorities.AddAsync(authority);
            await _unitOfWork.CompleteAsync();
            
            // Add resource permissions
            foreach (var permission in dto.ResourcePermissions)
            {
                var authorityResource = new AuthorityResource
                {
                    AuthorityId = authority.Id,
                    OrganizationUnitId = authority.OrganizationUnitId,
                    ResourceName = permission.ResourceName,
                    Permission = permission.Permission
                };
                
                await _unitOfWork.AuthorityResources.AddAsync(authorityResource);
            }
            
            await _unitOfWork.CompleteAsync();
            
            return await GetAuthorityWithPermissionsAsync(authority.Id) ?? 
                   throw new InvalidOperationException("Failed to retrieve created authority");
        }
        
        public async Task<AuthorityWithPermissionsDto?> GetAuthorityWithPermissionsAsync(Guid authorityId)
        {
            if (!_tenantContext.HasTenant)
                return null;
                
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                a => a.Id == authorityId && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (authority == null)
                return null;
            
            var permissions = await _unitOfWork.AuthorityResources.GetAllAsync(ar => ar.AuthorityId == authorityId);
            
            return new AuthorityWithPermissionsDto
            {
                Id = authority.Id,
                Name = authority.Name,
                Description = authority.Description,
                IsSystemAuthority = authority.IsSystemAuthority,
                CreatedAt = authority.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = authority.LastModifyAt,
                Permissions = permissions.Select(p => new ResourcePermissionDto
                {
                    AuthorityId = p.AuthorityId,
                    AuthorityName = authority.Name,
                    ResourceName = p.ResourceName,
                    Permission = p.Permission,
                    PermissionDescription = Permissions.GetDescription(p.Permission),
                    ResourceDisplayName = Resources.GetDisplayName(p.ResourceName)
                }).ToList()
            };
        }
        
        public async Task<IEnumerable<AuthorityWithPermissionsDto>> GetAllAuthoritiesWithPermissionsAsync()
        {
            if (!_tenantContext.HasTenant)
                return new List<AuthorityWithPermissionsDto>();
                
            // CRITICAL FIX: Explicitly filter by current tenant
            var authorities = await _unitOfWork.Authorities.GetAllAsync(
                a => a.OrganizationUnitId == _tenantContext.CurrentTenantId);
            var result = new List<AuthorityWithPermissionsDto>();
            
            foreach (var authority in authorities)
            {
                var authorityDto = await GetAuthorityWithPermissionsAsync(authority.Id);
                if (authorityDto != null)
                    result.Add(authorityDto);
            }
            
            return result;
        }
        
        public async Task UpdateAuthorityAsync(Guid authorityId, UpdateAuthorityDto dto)
        {
            if (!_tenantContext.HasTenant)
                throw new InvalidOperationException("Tenant context is required for authority update");
                
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                a => a.Id == authorityId && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (authority == null)
                throw new NotFoundException($"Authority with ID {authorityId} not found");
            
            if (authority.IsSystemAuthority)
                throw new InvalidOperationException("Cannot modify system authorities");
            
            // Update basic properties
            if (!string.IsNullOrEmpty(dto.Name))
            {
                // Check name uniqueness within current tenant
                var existingAuthority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                    a => a.Name == dto.Name && a.Id != authorityId && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
                if (existingAuthority != null)
                    throw new InvalidOperationException($"Authority with name '{dto.Name}' already exists");
                
                authority.Name = dto.Name;
            }
            
            if (dto.Description != null)
                authority.Description = dto.Description;
            
            authority.LastModifyAt = DateTime.UtcNow;
            _unitOfWork.Authorities.Update(authority);
            
            // Update permissions if provided
            if (dto.ResourcePermissions != null)
            {
                // Remove existing permissions
                var existingPermissions = await _unitOfWork.AuthorityResources.GetAllAsync(ar => ar.AuthorityId == authorityId);
                foreach (var permission in existingPermissions)
                {
                    _unitOfWork.AuthorityResources.Remove(permission);
                }
                
                // Add new permissions
                foreach (var permission in dto.ResourcePermissions)
                {
                    var authorityResource = new AuthorityResource
                    {
                        AuthorityId = authorityId,
                        OrganizationUnitId = authority.OrganizationUnitId,
                        ResourceName = permission.ResourceName,
                        Permission = permission.Permission
                    };
                    
                    await _unitOfWork.AuthorityResources.AddAsync(authorityResource);
                }
            }
            
            await _unitOfWork.CompleteAsync();
        }
        
        public async Task DeleteAuthorityAsync(Guid authorityId)
        {
            if (!_tenantContext.HasTenant)
                throw new InvalidOperationException("Tenant context is required for authority deletion");
                
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                a => a.Id == authorityId && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (authority == null)
                throw new NotFoundException($"Authority with ID {authorityId} not found");
            
            if (authority.IsSystemAuthority)
                throw new InvalidOperationException("Cannot delete system authorities");
            
            // Check if authority is assigned to any users
            var userAssignments = await _unitOfWork.UserAuthorities.GetAllAsync(ua => ua.AuthorityId == authorityId);
            if (userAssignments.Any())
                throw new InvalidOperationException("Cannot delete authority that is assigned to users");
            
            // Remove associated permissions
            var permissions = await _unitOfWork.AuthorityResources.GetAllAsync(ar => ar.AuthorityId == authorityId);
            foreach (var permission in permissions)
            {
                _unitOfWork.AuthorityResources.Remove(permission);
            }
            
            // Remove authority
            _unitOfWork.Authorities.Remove(authority);
            await _unitOfWork.CompleteAsync();
        }
        
        #endregion
        
        #region User-Authority Assignments
        
        public async Task<IEnumerable<Authority>> GetUserAuthoritiesAsync(Guid userId)
        {
            return await GetUserAuthoritiesWithDetailAsync(userId);
        }
        
        public async Task AssignAuthorityToUserAsync(Guid userId, Guid authorityId)
        {
            if (!_tenantContext.HasTenant)
                throw new InvalidOperationException("Tenant context is required for authority assignment");
                
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                a => a.Id == authorityId && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (authority == null) 
                throw new NotFoundException($"Authority with ID {authorityId} not found");
            
            // Check if the authority is already assigned to the user
            var existingAssignment = await _unitOfWork.UserAuthorities.GetFirstOrDefaultAsync(
                ua => ua.UserId == userId && ua.AuthorityId == authorityId);
                
            if (existingAssignment != null)
                return; // Already assigned
                
            // Create new user authority
            var userAuthority = new UserAuthority
            {
                UserId = userId,
                AuthorityId = authorityId,
                OrganizationUnitId = authority.OrganizationUnitId
            };
            
            await _unitOfWork.UserAuthorities.AddAsync(userAuthority);
            await _unitOfWork.CompleteAsync();
        }
        
        public async Task RemoveAuthorityFromUserAsync(Guid userId, Guid authorityId)
        {
            var userAuthority = await _unitOfWork.UserAuthorities.GetFirstOrDefaultAsync(
                ua => ua.UserId == userId && ua.AuthorityId == authorityId);
                
            if (userAuthority != null)
            {
                _unitOfWork.UserAuthorities.Remove(userAuthority);
                await _unitOfWork.CompleteAsync();
            }
        }
        
        private async Task<List<Authority>> GetUserAuthoritiesWithDetailAsync(Guid userId)
        {
            if (!_tenantContext.HasTenant)
                return new List<Authority>();
                
            var userAuthorities = await _unitOfWork.UserAuthorities.GetAllAsync(
                ua => ua.UserId == userId && ua.OrganizationUnitId == _tenantContext.CurrentTenantId);
                
            if (!userAuthorities.Any())
                return new List<Authority>();
                
            var authorityIds = userAuthorities.Select(ua => ua.AuthorityId).ToList();
            
            var authorities = await _unitOfWork.Authorities.GetAllAsync(
                a => authorityIds.Contains(a.Id) && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
                
            return authorities.ToList();
        }
        
        #endregion
        
        #region Legacy Methods (Backward Compatibility)
        
        public async Task AssignAuthorityToUserAsync(Guid userId, string authorityName)
        {
            if (!_tenantContext.HasTenant)
                throw new InvalidOperationException("Tenant context is required for authority assignment");
                
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                a => a.Name == authorityName && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (authority == null) 
                throw new InvalidOperationException($"Authority {authorityName} not found");
            
            await AssignAuthorityToUserAsync(userId, authority.Id);
        }
        
        public async Task RemoveAuthorityFromUserAsync(Guid userId, string authorityName)
        {
            if (!_tenantContext.HasTenant)
                throw new InvalidOperationException("Tenant context is required for authority removal");
                
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                a => a.Name == authorityName && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (authority == null) 
                throw new InvalidOperationException($"Authority {authorityName} not found");
            
            await RemoveAuthorityFromUserAsync(userId, authority.Id);
        }
        
        #endregion
        
        #region Resource Permissions
        
        public async Task AddResourcePermissionAsync(string authorityName, string resourceName, int permission)
        {
            if (!_tenantContext.HasTenant)
                throw new InvalidOperationException("Tenant context is required for resource permission management");
                
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                a => a.Name == authorityName && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (authority == null) 
                throw new InvalidOperationException($"Authority {authorityName} not found");
            
            try
            {
                // Check if resource permission already exists
                var existingPermission = await _unitOfWork.AuthorityResources.GetFirstOrDefaultAsync(
                    ar => ar.AuthorityId == authority.Id && ar.ResourceName == resourceName);
                
                if (existingPermission != null)
                {
                    // Update permission
                    existingPermission.Permission = permission;
                    existingPermission.LastModifyAt = DateTime.UtcNow;
                    _unitOfWork.AuthorityResources.Update(existingPermission);
                }
                else
                {
                    // Create a new resource permission
                    var authorityResource = new AuthorityResource
                    {
                        AuthorityId = authority.Id,
                        OrganizationUnitId = authority.OrganizationUnitId,
                        ResourceName = resourceName,
                        Permission = permission
                    };
                    
                    await _unitOfWork.AuthorityResources.AddAsync(authorityResource);
                }
                
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add resource permission: {ex.Message}", ex);
            }
        }
        
        public async Task RemoveResourcePermissionAsync(string authorityName, string resourceName)
        {
            if (!_tenantContext.HasTenant)
                throw new InvalidOperationException("Tenant context is required for resource permission management");
                
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                a => a.Name == authorityName && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (authority == null) 
                throw new InvalidOperationException($"Authority {authorityName} not found");
            
            try
            {
                var permission = await _unitOfWork.AuthorityResources.GetFirstOrDefaultAsync(
                    ar => ar.AuthorityId == authority.Id && ar.ResourceName == resourceName);
                
                if (permission != null)
                {
                    _unitOfWork.AuthorityResources.Remove(permission);
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to remove resource permission: {ex.Message}", ex);
            }
        }
        
        #endregion
        
        #region Resource Information
        
        public async Task<IEnumerable<AvailableResourceDto>> GetAvailableResourcesAsync()
        {
            // Return static resource information
            // In a more advanced implementation, this could be dynamic from database
            return await Task.FromResult(Resources.GetAvailableResources());
        }

        #endregion

        #region Assign Multiple Roles to User
        public async Task AssignAuthoritiesToUserAsync(Guid userId, List<Guid> authorityIds, Guid organizationUnitId)
        {
            if (!_tenantContext.HasTenant)
                throw new InvalidOperationException("Tenant context is required for authority assignment");
                
            // Verify organizationUnitId matches current tenant
            if (organizationUnitId != _tenantContext.CurrentTenantId)
                throw new InvalidOperationException("Organization unit ID does not match current tenant");
                
            // Only get UserAuthorities for this user in the current OU
            var currentUserAuthorities = await _unitOfWork.UserAuthorities.GetAllAsync(
                ua => ua.UserId == userId && ua.OrganizationUnitId == organizationUnitId);
            var currentAuthorityIds = currentUserAuthorities.Select(ua => ua.AuthorityId).ToHashSet();

            // Batch fetch all authorities with the given IDs - ensure they belong to current tenant
            var authorities = await _unitOfWork.Authorities.GetAllAsync(
                a => authorityIds.Contains(a.Id) && a.OrganizationUnitId == organizationUnitId);
            var authorityDictionary = authorities.ToDictionary(a => a.Id);

            foreach (var authorityId in authorityIds)
            {
                if (!currentAuthorityIds.Contains(authorityId))
                {
                    if (!authorityDictionary.TryGetValue(authorityId, out var authority))
                        throw new NotFoundException($"Authority with ID {authorityId} not found");
                    if (authority.OrganizationUnitId != organizationUnitId)
                        throw new InvalidOperationException("Authority does not belong to this OU");

                    var userAuthority = new UserAuthority
                    {
                        UserId = userId,
                        AuthorityId = authorityId,
                        OrganizationUnitId = organizationUnitId
                    };
                    await _unitOfWork.UserAuthorities.AddAsync(userAuthority);
                }
            }

            // Remove authorities in this OU that are not in the new list
            foreach (var ua in currentUserAuthorities)
            {
                if (!authorityIds.Contains(ua.AuthorityId))
                {
                    _unitOfWork.UserAuthorities.Remove(ua);
                }
            }

            await _unitOfWork.CompleteAsync();
        }

        #endregion

    }
} 