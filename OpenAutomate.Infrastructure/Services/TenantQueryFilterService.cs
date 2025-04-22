using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Base;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service responsible for applying EF Core global query filters for tenant isolation
    /// </summary>
    public class TenantQueryFilterService
    {
        private readonly ITenantContext _tenantContext;

        public TenantQueryFilterService(ITenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Applies tenant query filters to tenant-aware entities in the model builder
        /// </summary>
        public void ApplyTenantFilters(ModelBuilder modelBuilder)
        {
            // Get all entity types from the model
            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Where(t => typeof(ITenantEntity).IsAssignableFrom(t.ClrType))
                .ToList();

            // Apply filter to each tenant-aware entity
            foreach (var entityType in entityTypes)
            {
                // Get the CLR type for the entity
                var entityClrType = entityType.ClrType;

                // Create a generic lambda expression for the filter
                var parameter = Expression.Parameter(entityClrType, "e");
                
                // Create the tenant filter expression: e => !_tenantContext.HasTenant || e.OrganizationUnitId == _tenantContext.CurrentTenantId
                var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.OrganizationUnitId));
                var currentTenantIdExpr = Expression.Property(Expression.Constant(_tenantContext), nameof(ITenantContext.CurrentTenantId));
                var hasTenantExpr = Expression.Property(Expression.Constant(_tenantContext), nameof(ITenantContext.HasTenant));
                
                var equalExpr = Expression.Equal(tenantIdProperty, currentTenantIdExpr);
                var notHasTenant = Expression.Not(hasTenantExpr);
                var orExpr = Expression.OrElse(notHasTenant, equalExpr);
                
                var lambda = Expression.Lambda(orExpr, parameter);

                // Apply the filter directly using the model builder
                modelBuilder.Entity(entityClrType).HasQueryFilter(lambda);
            }
        }
    }
} 