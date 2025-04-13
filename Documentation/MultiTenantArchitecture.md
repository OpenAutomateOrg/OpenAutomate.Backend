# Multi-Tenant Architecture Design for OpenAutomate

## Overview

This document outlines the multi-tenant architecture design for the OpenAutomate platform. In this design, each organization unit represents a tenant, and all data is isolated between tenants while using a shared database infrastructure.

## Architecture Approach

OpenAutomate implements multi-tenancy using the **shared database with tenant filtering** approach:

- A single database instance hosts data for all tenants
- Each tenant-specific entity includes a reference to its tenant (OrganizationUnit)
- Queries are automatically filtered by the current tenant
- Special system-wide entities remain accessible across tenants

This approach provides a good balance between resource efficiency and data isolation.

## Key Components

### 1. Tenant Entity

The OrganizationUnit entity serves as the tenant identifier:

```csharp
public class OrganizationUnit : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Slug { get; set; } // URL-friendly identifier
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<OrganizationUnitUser> OrganizationUnitUsers { get; set; }
    public virtual ICollection<BotAgent> BotAgents { get; set; }
    public virtual ICollection<AutomationPackage> AutomationPackages { get; set; }
    public virtual ICollection<Execution> Executions { get; set; }
    public virtual ICollection<Schedule> Schedules { get; set; }
}
```

### 2. Tenant Resolution Middleware

Middleware that extracts tenant information from the URL path:

```csharp
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
    {
        // URL format: https://domain.com/{tenantSlug}/api/...
        var path = context.Request.Path.Value;
        
        if (path != null && path.Length > 1)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                var potentialTenantSlug = segments[0];
                var tenant = await unitOfWork.OrganizationUnits
                    .GetFirstOrDefaultAsync(o => o.Slug == potentialTenantSlug && o.IsActive);
                
                if (tenant != null)
                {
                    // Store the tenant in HttpContext.Items for later use
                    context.Items["CurrentTenant"] = tenant;
                    
                    // Rewrite path to remove tenant segment
                    var newPath = "/" + string.Join('/', segments.Skip(1));
                    context.Request.Path = new PathString(newPath);
                }
            }
        }
        
        await _next(context);
    }
}
```

### 3. Tenant Context Service

Service that provides access to the current tenant throughout the application:

```csharp
public interface ITenantContext
{
    OrganizationUnit CurrentTenant { get; }
    Guid CurrentTenantId { get; }
    bool HasTenant { get; }
}

public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public OrganizationUnit CurrentTenant => 
        _httpContextAccessor.HttpContext?.Items["CurrentTenant"] as OrganizationUnit;
    
    public Guid CurrentTenantId => CurrentTenant?.Id ?? Guid.Empty;
    
    public bool HasTenant => CurrentTenant != null;
}
```

### 4. Tenant-Aware Database Context

Database context that automatically applies tenant filtering:

```csharp
public class ApplicationDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply global query filters for tenant isolation
        modelBuilder.Entity<BotAgent>()
            .HasQueryFilter(b => b.OrganizationUnitId == _tenantContext.CurrentTenantId);
            
        modelBuilder.Entity<AutomationPackage>()
            .HasQueryFilter(a => a.OrganizationUnitId == _tenantContext.CurrentTenantId);
            
        modelBuilder.Entity<Execution>()
            .HasQueryFilter(e => e.OrganizationUnitId == _tenantContext.CurrentTenantId);
            
        modelBuilder.Entity<Schedule>()
            .HasQueryFilter(s => s.OrganizationUnitId == _tenantContext.CurrentTenantId);
            
        // Configure relationships
        // ...
    }
    
    // Override SaveChanges to set tenant ID on new entities
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        if (_tenantContext.HasTenant)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is ITenantEntity tenantEntity &&
                    entry.State == EntityState.Added)
                {
                    tenantEntity.OrganizationUnitId = _tenantContext.CurrentTenantId;
                }
            }
        }
        
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
```

### 5. Tenant Entity Interface

Interface to mark entities that belong to a tenant:

```csharp
public interface ITenantEntity
{
    Guid OrganizationUnitId { get; set; }
}
```

### 6. Tenant-Aware Repositories

Repositories that respect tenant isolation:

```csharp
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    // Existing implementation
    
    // Override add methods to set tenant ID for tenant-specific entities
    public async Task AddAsync(TEntity entity)
    {
        // If entity implements ITenantEntity, set the tenant ID
        if (entity is ITenantEntity tenantEntity && _tenantContext.HasTenant)
        {
            tenantEntity.OrganizationUnitId = _tenantContext.CurrentTenantId;
        }
        
        await _dbSet.AddAsync(entity);
    }
}
```

## URL Structure

- Tenant-specific endpoints: `https://domain.com/{tenant-slug}/api/resources`
- System-wide endpoints: `https://domain.com/api/resources` (accessible only to system administrators)
- Admin management endpoints: `https://domain.com/admin/tenants` (tenant management)

## User to Tenant Mapping

Users can belong to multiple organization units (tenants) through the OrganizationUnitUser join entity:

```csharp
public class OrganizationUnitUser
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; }
    
    public Guid OrganizationUnitId { get; set; }
    public virtual OrganizationUnit OrganizationUnit { get; set; }
    
    public string Role { get; set; } // Role within this organization unit
}
```

## Security Considerations

1. **Tenant Isolation**: Ensures data from one tenant is never exposed to another tenant

2. **Authentication and Authorization**: JWT tokens include tenant information

3. **API Security**: All API endpoints validate tenant access

4. **Cross-Tenant Operations**: Special permissions required for system administrators

5. **Audit Logging**: All operations log the tenant context for compliance

## Implementation Steps

1. Enhance the `OrganizationUnit` entity with tenant-specific fields.

2. Modify all tenant-specific entities to include `OrganizationUnitId`.

3. Implement the TenantResolutionMiddleware.

4. Create the ITenantContext interface and implementation.

5. Update the ApplicationDbContext to apply tenant filtering.

6. Modify repositories to respect tenant context.

7. Update authentication to include tenant information in tokens.

8. Add tenant-aware authorization policies.

## Performance Considerations

- **Indexing**: Ensure all OrganizationUnitId columns are indexed.

- **Query Optimization**: Monitor the performance impact of global query filters.

- **Caching**: Implement tenant-aware caching strategies.

- **Database Scaling**: Consider database sharding for extremely large deployments.

## Testing Approach

1. **Unit Testing**: Test tenant filtering in isolation.

2. **Integration Testing**: Verify tenant isolation across the application.

3. **Security Testing**: Validate that cross-tenant data access is prevented.

## Conclusion

The multi-tenant architecture provides a flexible, secure approach to hosting multiple organizations within the OpenAutomate platform. By implementing tenant filtering at the data access layer, we ensure complete data isolation while maintaining the efficiency of a shared infrastructure. 