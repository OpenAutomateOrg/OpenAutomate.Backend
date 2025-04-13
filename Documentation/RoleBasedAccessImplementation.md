# Role-Based Access Control (RBAC) Implementation

## Overview

This document outlines the technical implementation of Role-Based Access Control (RBAC) within the OpenAutomate platform, focusing on authorization within a single OrganizationUnit (tenant). The implementation will follow a resource-permission model where authorities (roles) are granted specific permissions to resources.

## Core Components

The RBAC system will consist of four primary tables:

1. **User** - Already exists in the system
2. **Authority** - Represents roles in the system (ADMIN, USER, OPERATOR, DEVELOPER)
3. **UserAuthority** - Maps users to authorities (roles)
4. **AuthorityResource** - Defines permissions for each authority on specific resources

## Data Model

### Existing Entities

```csharp
// User.cs - Already exists
public class User : BaseUser
{
    public string? FirstName { set; get; }
    public string? LastName { set; get; }
    public string? ImageUrl { set; get; }
    public List<RefreshToken>? RefreshTokens { get; set; }
    // Existing methods
}

// Authority.cs - Already exists but needs enhancement
public class Authority : BaseEntity
{
    public string? Name { set; get; }
}

// UserAuthority.cs - Already exists but needs enhancement
public class UserAuthority
{
    [Required]
    public Guid UserId { set; get; }
    [ForeignKey("UserId")]
    public User User { get; set; }

    [Required]
    public Guid AuthorityID { set; get; }
    [ForeignKey("AuthorityID")]
    public Authority Authority { get; set; }
}
```

### New Entity

```csharp
// AuthorityResource.cs - New entity to implement
public class AuthorityResource : BaseEntity
{
    [Required]
    public string ResourceName { get; set; }
    
    [Required]
    public int Permission { get; set; }
    
    [Required]
    public Guid AuthorityId { get; set; }
    
    [ForeignKey("AuthorityId")]
    public Authority Authority { get; set; }
    
    // OrganizationUnitId is inherited from BaseEntity
}
```

### Permission Constants

```csharp
public static class Permissions
{
    public const int View = 1;
    public const int Create = 2;
    public const int Update = 3;
    public const int Delete = 4;
}

public static class Resources
{
    public const string AdminResource = "admin";
    public const string EnvironmentResource = "environment";
    public const string AgentResource = "agent";
    public const string PackageResource = "package";
    public const string ScheduleResource = "schedule";
    // Add more resources as needed
}
```

## Implementation Steps

### 1. Entity Model Enhancement

1. Update the `Authority` entity to support predefined roles:

```csharp
public class Authority : BaseEntity
{
    public string Name { get; set; }
    
    // Navigation property
    public ICollection<AuthorityResource> AuthorityResources { get; set; }
    public ICollection<UserAuthority> UserAuthorities { get; set; }
}
```

2. Create the `AuthorityResource` entity:

```csharp
public class AuthorityResource : BaseEntity
{
    [Required]
    public string ResourceName { get; set; }
    
    [Required]
    public int Permission { get; set; }
    
    [Required]
    public Guid AuthorityId { get; set; }
    
    [ForeignKey("AuthorityId")]
    public Authority Authority { get; set; }
}
```

3. Enhance the `UserAuthority` entity for proper management:

```csharp
public class UserAuthority : BaseEntity
{
    [Required]
    public Guid UserId { set; get; }
    
    [ForeignKey("UserId")]
    public User User { get; set; }

    [Required]
    public Guid AuthorityId { set; get; }
    
    [ForeignKey("AuthorityId")]
    public Authority Authority { get; set; }
}
```

### 2. Database Configuration

1. Add Entity Framework configuration for the new and modified entities:

```csharp
// AuthorityConfiguration.cs
public class AuthorityConfiguration : IEntityTypeConfiguration<Authority>
{
    public void Configure(EntityTypeBuilder<Authority> builder)
    {
        builder.ToTable("authorities");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(50);
        
        // Add initial seed data
        builder.HasData(
            new Authority { Id = Guid.NewGuid(), Name = "ADMIN", CreatedAt = DateTime.UtcNow },
            new Authority { Id = Guid.NewGuid(), Name = "USER", CreatedAt = DateTime.UtcNow },
            new Authority { Id = Guid.NewGuid(), Name = "OPERATOR", CreatedAt = DateTime.UtcNow },
            new Authority { Id = Guid.NewGuid(), Name = "DEVELOPER", CreatedAt = DateTime.UtcNow }
        );
    }
}

// AuthorityResourceConfiguration.cs
public class AuthorityResourceConfiguration : IEntityTypeConfiguration<AuthorityResource>
{
    public void Configure(EntityTypeBuilder<AuthorityResource> builder)
    {
        builder.ToTable("authority_resources");
        builder.HasKey(ar => ar.Id);
        builder.Property(ar => ar.ResourceName).IsRequired().HasMaxLength(50);
        builder.Property(ar => ar.Permission).IsRequired();
        
        // Create index for faster lookups
        builder.HasIndex(ar => new { ar.AuthorityId, ar.ResourceName });
    }
}

// UserAuthorityConfiguration.cs
public class UserAuthorityConfiguration : IEntityTypeConfiguration<UserAuthority>
{
    public void Configure(EntityTypeBuilder<UserAuthority> builder)
    {
        builder.ToTable("user_authorities");
        builder.HasKey(ua => ua.Id);
        
        // Create unique constraint
        builder.HasIndex(ua => new { ua.UserId, ua.AuthorityId }).IsUnique();
    }
}
```

2. Update the DbContext to include the new configurations:

```csharp
// AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Existing configurations
    // ...
    
    // New configurations
    modelBuilder.ApplyConfiguration(new AuthorityConfiguration());
    modelBuilder.ApplyConfiguration(new AuthorityResourceConfiguration());
    modelBuilder.ApplyConfiguration(new UserAuthorityConfiguration());
}
```

### 3. Create Repository Interfaces and Implementations

1. Define repository interfaces:

```csharp
// IAuthorityRepository.cs
public interface IAuthorityRepository : IBaseRepository<Authority>
{
    Task<Authority> GetByNameAsync(string name);
    Task<IEnumerable<Authority>> GetUserAuthoritiesAsync(Guid userId);
}

// IAuthorityResourceRepository.cs
public interface IAuthorityResourceRepository : IBaseRepository<AuthorityResource>
{
    Task<IEnumerable<AuthorityResource>> GetByAuthorityIdAsync(Guid authorityId);
    Task<bool> HasPermissionAsync(Guid userId, string resourceName, int permission);
}

// IUserAuthorityRepository.cs
public interface IUserAuthorityRepository : IBaseRepository<UserAuthority>
{
    Task<IEnumerable<UserAuthority>> GetByUserIdAsync(Guid userId);
    Task<bool> HasAuthorityAsync(Guid userId, string authorityName);
    Task AssignAuthorityToUserAsync(Guid userId, Guid authorityId);
    Task RemoveAuthorityFromUserAsync(Guid userId, Guid authorityId);
}
```

2. Implement the repositories:

```csharp
// AuthorityRepository.cs
public class AuthorityRepository : BaseRepository<Authority>, IAuthorityRepository
{
    public AuthorityRepository(AppDbContext context) : base(context) { }
    
    public async Task<Authority> GetByNameAsync(string name)
    {
        return await _context.Authorities
            .FirstOrDefaultAsync(a => a.Name == name);
    }
    
    public async Task<IEnumerable<Authority>> GetUserAuthoritiesAsync(Guid userId)
    {
        return await _context.UserAuthorities
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.Authority)
            .ToListAsync();
    }
}

// AuthorityResourceRepository.cs
public class AuthorityResourceRepository : BaseRepository<AuthorityResource>, IAuthorityResourceRepository
{
    private readonly IUserAuthorityRepository _userAuthorityRepository;
    
    public AuthorityResourceRepository(
        AppDbContext context,
        IUserAuthorityRepository userAuthorityRepository) : base(context)
    {
        _userAuthorityRepository = userAuthorityRepository;
    }
    
    public async Task<IEnumerable<AuthorityResource>> GetByAuthorityIdAsync(Guid authorityId)
    {
        return await _context.AuthorityResources
            .Where(ar => ar.AuthorityId == authorityId)
            .ToListAsync();
    }
    
    public async Task<bool> HasPermissionAsync(Guid userId, string resourceName, int permission)
    {
        // Get user's authorities
        var userAuthorities = await _userAuthorityRepository.GetByUserIdAsync(userId);
        var authorityIds = userAuthorities.Select(ua => ua.AuthorityId);
        
        // Check if any of user's authorities has the required permission for the resource
        return await _context.AuthorityResources
            .AnyAsync(ar => 
                authorityIds.Contains(ar.AuthorityId) && 
                ar.ResourceName == resourceName && 
                ar.Permission >= permission);
    }
}

// UserAuthorityRepository.cs
public class UserAuthorityRepository : BaseRepository<UserAuthority>, IUserAuthorityRepository
{
    private readonly IAuthorityRepository _authorityRepository;
    
    public UserAuthorityRepository(
        AppDbContext context,
        IAuthorityRepository authorityRepository) : base(context)
    {
        _authorityRepository = authorityRepository;
    }
    
    public async Task<IEnumerable<UserAuthority>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserAuthorities
            .Where(ua => ua.UserId == userId)
            .Include(ua => ua.Authority)
            .ToListAsync();
    }
    
    public async Task<bool> HasAuthorityAsync(Guid userId, string authorityName)
    {
        var authority = await _authorityRepository.GetByNameAsync(authorityName);
        if (authority == null) return false;
        
        return await _context.UserAuthorities
            .AnyAsync(ua => ua.UserId == userId && ua.AuthorityId == authority.Id);
    }
    
    public async Task AssignAuthorityToUserAsync(Guid userId, Guid authorityId)
    {
        // Check if assignment already exists
        var exists = await _context.UserAuthorities
            .AnyAsync(ua => ua.UserId == userId && ua.AuthorityId == authorityId);
            
        if (!exists)
        {
            var userAuthority = new UserAuthority
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AuthorityId = authorityId,
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.UserAuthorities.AddAsync(userAuthority);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task RemoveAuthorityFromUserAsync(Guid userId, Guid authorityId)
    {
        var userAuthority = await _context.UserAuthorities
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AuthorityId == authorityId);
            
        if (userAuthority != null)
        {
            _context.UserAuthorities.Remove(userAuthority);
            await _context.SaveChangesAsync();
        }
    }
}
```

### 4. Authorization Service Implementation

1. Define the Authorization Service interface:

```csharp
// IAuthorizationManager.cs
public interface IAuthorizationManager
{
    Task<bool> HasPermissionAsync(Guid userId, string resourceName, int permission);
    Task<bool> HasAuthorityAsync(Guid userId, string authorityName);
    Task<IEnumerable<Authority>> GetUserAuthoritiesAsync(Guid userId);
    Task AssignAuthorityToUserAsync(Guid userId, string authorityName);
    Task RemoveAuthorityFromUserAsync(Guid userId, string authorityName);
    Task AddResourcePermissionAsync(string authorityName, string resourceName, int permission);
    Task RemoveResourcePermissionAsync(string authorityName, string resourceName);
}
```

2. Implement the Authorization Service:

```csharp
// AuthorizationManager.cs
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
        if (authority == null) throw new NotFoundException($"Authority {authorityName} not found");
        
        await _userAuthorityRepository.AssignAuthorityToUserAsync(userId, authority.Id);
    }
    
    public async Task RemoveAuthorityFromUserAsync(Guid userId, string authorityName)
    {
        var authority = await _authorityRepository.GetByNameAsync(authorityName);
        if (authority == null) throw new NotFoundException($"Authority {authorityName} not found");
        
        await _userAuthorityRepository.RemoveAuthorityFromUserAsync(userId, authority.Id);
    }
    
    public async Task AddResourcePermissionAsync(string authorityName, string resourceName, int permission)
    {
        var authority = await _authorityRepository.GetByNameAsync(authorityName);
        if (authority == null) throw new NotFoundException($"Authority {authorityName} not found");
        
        // Check if resource permission already exists
        var existingPermission = await _authorityResourceRepository.Query()
            .FirstOrDefaultAsync(ar => ar.AuthorityId == authority.Id && ar.ResourceName == resourceName);
            
        if (existingPermission != null)
        {
            // Update permission
            existingPermission.Permission = permission;
            await _authorityResourceRepository.UpdateAsync(existingPermission);
        }
        else
        {
            // Create new permission
            var authorityResource = new AuthorityResource
            {
                Id = Guid.NewGuid(),
                AuthorityId = authority.Id,
                ResourceName = resourceName,
                Permission = permission,
                CreatedAt = DateTime.UtcNow
            };
            
            await _authorityResourceRepository.AddAsync(authorityResource);
        }
    }
    
    public async Task RemoveResourcePermissionAsync(string authorityName, string resourceName)
    {
        var authority = await _authorityRepository.GetByNameAsync(authorityName);
        if (authority == null) throw new NotFoundException($"Authority {authorityName} not found");
        
        var permission = await _authorityResourceRepository.Query()
            .FirstOrDefaultAsync(ar => ar.AuthorityId == authority.Id && ar.ResourceName == resourceName);
            
        if (permission != null)
        {
            await _authorityResourceRepository.DeleteAsync(permission.Id);
        }
    }
}
```

### 5. Create Custom Authorization Attribute

```csharp
// RequirePermissionAttribute.cs
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _resourceName;
    private readonly int _permission;
    
    public RequirePermissionAttribute(string resourceName, int permission)
    {
        _resourceName = resourceName;
        _permission = permission;
    }
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Get the user ID from the claims
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        // Get the authorization service from DI
        var authorizationManager = context.HttpContext.RequestServices
            .GetRequiredService<IAuthorizationManager>();
            
        // Check if user has permission
        var hasPermission = await authorizationManager.HasPermissionAsync(
            Guid.Parse(userId), 
            _resourceName, 
            _permission
        );
        
        if (!hasPermission)
        {
            context.Result = new ForbidResult();
            return;
        }
        
        await next();
    }
}
```

### 6. API Controller Implementation

```csharp
// AuthorityController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthorityController : ControllerBase
{
    private readonly IAuthorizationManager _authorizationManager;
    
    public AuthorityController(IAuthorizationManager authorizationManager)
    {
        _authorizationManager = authorizationManager;
    }
    
    [HttpGet("user/{userId}")]
    [RequirePermission(Resources.AdminResource, Permissions.View)]
    public async Task<ActionResult<IEnumerable<AuthorityDto>>> GetUserAuthorities(Guid userId)
    {
        var authorities = await _authorizationManager.GetUserAuthoritiesAsync(userId);
        var result = authorities.Select(a => new AuthorityDto { Id = a.Id, Name = a.Name });
        return Ok(result);
    }
    
    [HttpPost("user/{userId}")]
    [RequirePermission(Resources.AdminResource, Permissions.Update)]
    public async Task<ActionResult> AssignAuthorityToUser(Guid userId, [FromBody] AssignAuthorityDto dto)
    {
        await _authorizationManager.AssignAuthorityToUserAsync(userId, dto.AuthorityName);
        return Ok();
    }
    
    [HttpDelete("user/{userId}/{authorityName}")]
    [RequirePermission(Resources.AdminResource, Permissions.Delete)]
    public async Task<ActionResult> RemoveAuthorityFromUser(Guid userId, string authorityName)
    {
        await _authorizationManager.RemoveAuthorityFromUserAsync(userId, authorityName);
        return Ok();
    }
    
    [HttpPost("permission")]
    [RequirePermission(Resources.AdminResource, Permissions.Create)]
    public async Task<ActionResult> AddResourcePermission([FromBody] ResourcePermissionDto dto)
    {
        await _authorizationManager.AddResourcePermissionAsync(
            dto.AuthorityName,
            dto.ResourceName,
            dto.Permission
        );
        return Ok();
    }
    
    [HttpDelete("permission/{authorityName}/{resourceName}")]
    [RequirePermission(Resources.AdminResource, Permissions.Delete)]
    public async Task<ActionResult> RemoveResourcePermission(string authorityName, string resourceName)
    {
        await _authorizationManager.RemoveResourcePermissionAsync(authorityName, resourceName);
        return Ok();
    }
}
```

### 7. DTOs for the API

```csharp
// AuthorityDto.cs
public class AuthorityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

// AssignAuthorityDto.cs
public class AssignAuthorityDto
{
    [Required]
    public string AuthorityName { get; set; }
}

// ResourcePermissionDto.cs
public class ResourcePermissionDto
{
    [Required]
    public string AuthorityName { get; set; }
    
    [Required]
    public string ResourceName { get; set; }
    
    [Required]
    [Range(1, 4)]
    public int Permission { get; set; }
}
```

### 8. Register Services in Dependency Injection

```csharp
// Program.cs or Startup.cs
services.AddScoped<IAuthorityRepository, AuthorityRepository>();
services.AddScoped<IAuthorityResourceRepository, AuthorityResourceRepository>();
services.AddScoped<IUserAuthorityRepository, UserAuthorityRepository>();
services.AddScoped<IAuthorizationManager, AuthorizationManager>();
```

### 9. Seed Initial Data

Create a database migration and add this data seeding:

```csharp
// Seed initial authorities
var adminAuthority = new Authority { Id = Guid.NewGuid(), Name = "ADMIN", CreatedAt = DateTime.UtcNow };
var userAuthority = new Authority { Id = Guid.NewGuid(), Name = "USER", CreatedAt = DateTime.UtcNow };
var operatorAuthority = new Authority { Id = Guid.NewGuid(), Name = "OPERATOR", CreatedAt = DateTime.UtcNow };
var developerAuthority = new Authority { Id = Guid.NewGuid(), Name = "DEVELOPER", CreatedAt = DateTime.UtcNow };

// Add authorities
modelBuilder.Entity<Authority>().HasData(
    adminAuthority,
    userAuthority,
    operatorAuthority,
    developerAuthority
);

// Seed initial permissions for ADMIN
modelBuilder.Entity<AuthorityResource>().HasData(
    new AuthorityResource 
    { 
        Id = Guid.NewGuid(), 
        AuthorityId = adminAuthority.Id, 
        ResourceName = Resources.AdminResource, 
        Permission = Permissions.Delete,
        CreatedAt = DateTime.UtcNow
    },
    new AuthorityResource 
    { 
        Id = Guid.NewGuid(), 
        AuthorityId = adminAuthority.Id, 
        ResourceName = Resources.AgentResource, 
        Permission = Permissions.Delete,
        CreatedAt = DateTime.UtcNow
    },
    // Add more resources with full permissions for ADMIN
);

// Seed basic permissions for USER
modelBuilder.Entity<AuthorityResource>().HasData(
    new AuthorityResource 
    { 
        Id = Guid.NewGuid(), 
        AuthorityId = userAuthority.Id, 
        ResourceName = Resources.AgentResource, 
        Permission = Permissions.View,
        CreatedAt = DateTime.UtcNow
    },
    new AuthorityResource 
    { 
        Id = Guid.NewGuid(), 
        AuthorityId = userAuthority.Id, 
        ResourceName = Resources.PackageResource, 
        Permission = Permissions.View,
        CreatedAt = DateTime.UtcNow
    },
    // Add more resources with VIEW permission for USER
);

// Add specific permissions for OPERATOR and DEVELOPER roles
```

## System Admin Implementation (Future Enhancement)

For system administrators that need access across all tenants, we will implement a separate mechanism:

1. Add a `IsSystemAdmin` property to the `User` entity
2. Modify the `HasPermissionAsync` method in the `AuthorizationManager` to bypass tenant checks for system admins
3. Create a custom attribute for system admin access

```csharp
// To be implemented in the future
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class RequireSystemAdminAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Implementation details for system admin check
    }
}
```

## Implementation Plan

1. **Phase 1: Core RBAC Infrastructure (Week 1)**
   - Update existing entities (Authority, UserAuthority)
   - Create new entity (AuthorityResource)
   - Create database migrations
   - Implement repositories

2. **Phase 2: Authorization Service (Week 1)**
   - Implement IAuthorizationManager
   - Create RequirePermission attribute
   - Update dependency injection

3. **Phase 3: Admin Management API (Week 2)**
   - Create AuthorityController
   - Create DTOs
   - Implement API endpoints

4. **Phase 4: Frontend Integration (Week 2)**
   - Create authority management UI
   - Implement user role assignment interface
   - Add permission UI for administrators

5. **Phase 5: System Admin Access (Week 3)**
   - Implement cross-tenant access for system admins
   - Create RequireSystemAdmin attribute
   - Add system admin UI

## Testing Plan

1. **Unit Tests:**
   - Test authority repository methods
   - Test permission evaluation logic
   - Test attribute functionality

2. **Integration Tests:**
   - Test API endpoints with various permission scenarios
   - Test cross-tenant access controls

3. **End-to-End Tests:**
   - Test role assignment through UI
   - Test permission effects on resource access

## Conclusion

This implementation provides a comprehensive RBAC system for OpenAutomate that:

1. Supports the four predefined roles (ADMIN, USER, OPERATOR, DEVELOPER)
2. Implements granular resource-level permissions
3. Maintains proper tenant isolation
4. Provides a framework for future system-admin cross-tenant access

The implementation leverages existing infrastructure while extending it to support more sophisticated access control needs. It follows ASP.NET Core best practices for authorization and remains compatible with the multi-tenant architecture of the OpenAutomate platform. 