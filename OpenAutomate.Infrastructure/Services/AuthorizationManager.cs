using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
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
        
        public AuthorizationManager(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        
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
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(a => a.Name == authorityName);
            if (authority == null)
                return false;
                
            var userAuthority = await _unitOfWork.UserAuthorities.GetFirstOrDefaultAsync(
                ua => ua.UserId == userId && ua.AuthorityId == authority.Id);
                
            return userAuthority != null;
        }
        
        public async Task<IEnumerable<Authority>> GetUserAuthoritiesAsync(Guid userId)
        {
            return await GetUserAuthoritiesWithDetailAsync(userId);
        }
        
        private async Task<List<Authority>> GetUserAuthoritiesWithDetailAsync(Guid userId)
        {
            var userAuthorities = await _unitOfWork.UserAuthorities.GetAllAsync(
                ua => ua.UserId == userId);
                
            if (!userAuthorities.Any())
                return new List<Authority>();
                
            var authorityIds = userAuthorities.Select(ua => ua.AuthorityId).ToList();
            
            var authorities = await _unitOfWork.Authorities.GetAllAsync(
                a => authorityIds.Contains(a.Id));
                
            return authorities.ToList();
        }
        
        public async Task AssignAuthorityToUserAsync(Guid userId, string authorityName)
        {
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(a => a.Name == authorityName);
            if (authority == null) 
                throw new InvalidOperationException($"Authority {authorityName} not found");
            
            // Check if the authority is already assigned to the user
            var existingAssignment = await _unitOfWork.UserAuthorities.GetFirstOrDefaultAsync(
                ua => ua.UserId == userId && ua.AuthorityId == authority.Id);
                
            if (existingAssignment != null)
                return; // Already assigned
                
            // Create new user authority
            var userAuthority = new UserAuthority
            {
                UserId = userId,
                AuthorityId = authority.Id,
                OrganizationUnitId = authority.OrganizationUnitId
            };
            
            await _unitOfWork.UserAuthorities.AddAsync(userAuthority);
            await _unitOfWork.CompleteAsync();
        }
        
        public async Task RemoveAuthorityFromUserAsync(Guid userId, string authorityName)
        {
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(a => a.Name == authorityName);
            if (authority == null) 
                throw new InvalidOperationException($"Authority {authorityName} not found");
            
            var userAuthority = await _unitOfWork.UserAuthorities.GetFirstOrDefaultAsync(
                ua => ua.UserId == userId && ua.AuthorityId == authority.Id);
                
            if (userAuthority != null)
            {
                _unitOfWork.UserAuthorities.Remove(userAuthority);
                await _unitOfWork.CompleteAsync();
            }
        }
        
        public async Task AddResourcePermissionAsync(string authorityName, string resourceName, int permission)
        {
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(a => a.Name == authorityName);
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
            var authority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(a => a.Name == authorityName);
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
    }
} 