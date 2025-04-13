using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    public class AuthorizationManager : IAuthorizationManager
    {
        private readonly IAuthorityRepository _authorityRepository;
        private readonly IAuthorityResourceRepository _authorityResourceRepository;
        private readonly IUserAuthorityRepository _userAuthorityRepository;
        
        public AuthorizationManager(
            IAuthorityRepository authorityRepository,
            IAuthorityResourceRepository authorityResourceRepository,
            IUserAuthorityRepository userAuthorityRepository)
        {
            _authorityRepository = authorityRepository;
            _authorityResourceRepository = authorityResourceRepository;
            _userAuthorityRepository = userAuthorityRepository;
        }
        
        public async Task<bool> HasPermissionAsync(Guid userId, string resourceName, int permission)
        {
            return await _authorityResourceRepository.HasPermissionAsync(userId, resourceName, permission);
        }
        
        public async Task<bool> HasAuthorityAsync(Guid userId, string authorityName)
        {
            return await _userAuthorityRepository.HasAuthorityAsync(userId, authorityName);
        }
        
        public async Task<IEnumerable<Authority>> GetUserAuthoritiesAsync(Guid userId)
        {
            return await _authorityRepository.GetUserAuthoritiesAsync(userId);
        }
        
        public async Task AssignAuthorityToUserAsync(Guid userId, string authorityName)
        {
            var authority = await _authorityRepository.GetByNameAsync(authorityName);
            if (authority == null) 
                throw new InvalidOperationException($"Authority {authorityName} not found");
            
            // Get the authority's ID using reflection if needed
            var authorityId = (Guid)authority.GetType().GetProperty("Id").GetValue(authority);
            await _userAuthorityRepository.AssignAuthorityToUserAsync(userId, authorityId);
        }
        
        public async Task RemoveAuthorityFromUserAsync(Guid userId, string authorityName)
        {
            var authority = await _authorityRepository.GetByNameAsync(authorityName);
            if (authority == null) 
                throw new InvalidOperationException($"Authority {authorityName} not found");
            
            // Get the authority's ID using reflection if needed
            var authorityId = (Guid)authority.GetType().GetProperty("Id").GetValue(authority);
            await _userAuthorityRepository.RemoveAuthorityFromUserAsync(userId, authorityId);
        }
        
        public async Task AddResourcePermissionAsync(string authorityName, string resourceName, int permission)
        {
            var authority = await _authorityRepository.GetByNameAsync(authorityName);
            if (authority == null) 
                throw new InvalidOperationException($"Authority {authorityName} not found");
            
            try
            {
                // Get the authority's ID using reflection
                var authorityId = (Guid)authority.GetType().GetProperty("Id").GetValue(authority);
                
                // Check if resource permission already exists
                var existingPermissions = await _authorityResourceRepository.GetByAuthorityIdAsync(authorityId);
                var existingPermission = existingPermissions.FirstOrDefault(ar => ar.ResourceName == resourceName);
                
                if (existingPermission != null)
                {
                    // Update permission
                    existingPermission.Permission = permission;
                    _authorityResourceRepository.Update(existingPermission);
                    await _authorityResourceRepository.SaveChangesAsync();
                }
                else
                {
                    // Find an organization unit ID for this authority
                    var userAuthority = await _userAuthorityRepository.GetAllAsync(
                        filter: ua => ua.AuthorityId == authorityId,
                        orderBy: null);
                    
                    if (!userAuthority.Any())
                        throw new InvalidOperationException($"Authority {authorityName} is not assigned to any user or organization unit");
                    
                    var organizationUnitId = userAuthority.First().OrganizationUnitId;
                    
                    // Create a new resource permission
                    var authorityResource = new AuthorityResource
                    {
                        AuthorityId = authorityId,
                        ResourceName = resourceName,
                        Permission = permission,
                        OrganizationUnitId = organizationUnitId
                    };
                    
                    await _authorityResourceRepository.AddAsync(authorityResource);
                    await _authorityResourceRepository.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add resource permission: {ex.Message}", ex);
            }
        }
        
        public async Task RemoveResourcePermissionAsync(string authorityName, string resourceName)
        {
            var authority = await _authorityRepository.GetByNameAsync(authorityName);
            if (authority == null) 
                throw new InvalidOperationException($"Authority {authorityName} not found");
            
            try
            {
                // Get the authority's ID using reflection
                var authorityId = (Guid)authority.GetType().GetProperty("Id").GetValue(authority);
                
                var permissions = await _authorityResourceRepository.GetByAuthorityIdAsync(authorityId);
                var permission = permissions.FirstOrDefault(ar => ar.ResourceName == resourceName);
                
                if (permission != null)
                {
                    _authorityResourceRepository.Remove(permission);
                    await _authorityResourceRepository.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to remove resource permission: {ex.Message}", ex);
            }
        }
    }
} 