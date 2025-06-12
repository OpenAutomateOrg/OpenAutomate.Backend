# OData Roles Endpoint Documentation

## Overview

The Roles OData endpoint provides powerful querying capabilities for Authority/Role data within a tenant-specific context. This endpoint follows the standard OData v4 protocol and provides automatic tenant isolation.

## Endpoint Details

- **Base URL**: `/{tenant}/odata/Roles`
- **Controller**: `AuthoritiesController`
- **Authentication**: Required (JWT Bearer token)
- **Authorization**: Requires `OrganizationUnitResource.View` permission
- **Data Type**: `AuthorityWithPermissionsDto`

## Supported OData Operations

### 1. Filtering (`$filter`)
Filter roles based on various criteria:

```
GET /{tenant}/odata/Roles?$filter=IsSystemAuthority eq false
GET /{tenant}/odata/Roles?$filter=contains(Name,'Admin')
GET /{tenant}/odata/Roles?$filter=Name eq 'OWNER'
GET /{tenant}/odata/Roles?$filter=CreatedAt gt 2024-01-01T00:00:00Z
```

### 2. Selection (`$select`)
Choose specific fields to return:

```
GET /{tenant}/odata/Roles?$select=Id,Name,Description
GET /{tenant}/odata/Roles?$select=Name,IsSystemAuthority
```

### 3. Ordering (`$orderby`)
Sort results by one or more fields:

```
GET /{tenant}/odata/Roles?$orderby=Name asc
GET /{tenant}/odata/Roles?$orderby=CreatedAt desc
GET /{tenant}/odata/Roles?$orderby=Name asc,CreatedAt desc
```

### 4. Pagination (`$top` and `$skip`)
Implement pagination for large datasets:

```
GET /{tenant}/odata/Roles?$top=10&$skip=0      # First page
GET /{tenant}/odata/Roles?$top=10&$skip=10     # Second page
GET /{tenant}/odata/Roles?$top=5&$orderby=Name asc&$skip=0
```

### 5. Expansion (`$expand`)
Include related permission data:

```
GET /{tenant}/odata/Roles?$expand=Permissions
GET /{tenant}/odata/Roles?$expand=Permissions&$select=Name,Permissions
```

### 6. Count (`$count`)
Get total count of records:

```
GET /{tenant}/odata/Roles?$count=true
GET /{tenant}/odata/Roles/$count
```

### 7. Complex Queries
Combine multiple operations:

```
GET /{tenant}/odata/Roles?$filter=IsSystemAuthority eq false&$orderby=Name asc&$top=5&$select=Id,Name,Description&$count=true
```

## Response Format

### AuthorityWithPermissionsDto Structure

```json
{
  "value": [
    {
      "id": "guid",
      "name": "string",
      "description": "string",
      "isSystemAuthority": "boolean",
      "createdAt": "datetime",
      "updatedAt": "datetime",
      "permissions": [
        {
          "authorityId": "guid",
          "authorityName": "string",
          "resourceName": "string",
          "permission": "integer",
          "permissionDescription": "string",
          "resourceDisplayName": "string"
        }
      ]
    }
  ],
  "@odata.count": "integer"
}
```

## Frontend Implementation Examples

### TypeScript/JavaScript with Fetch

```typescript
// Define the Authority interface
interface Authority {
  id: string;
  name: string;
  description: string;
  isSystemAuthority: boolean;
  createdAt: string;
  updatedAt?: string;
  permissions: Permission[];
}

interface Permission {
  authorityId: string;
  authorityName: string;
  resourceName: string;
  permission: number;
  permissionDescription: string;
  resourceDisplayName: string;
}

// Basic fetch function
async function getRoles(tenantSlug: string, filter?: string): Promise<Authority[]> {
  const baseUrl = `/${tenantSlug}/odata/Roles`;
  const queryParams = new URLSearchParams();
  
  if (filter) {
    queryParams.append('$filter', filter);
  }
  
  const response = await fetch(`${baseUrl}?${queryParams}`, {
    headers: {
      'Authorization': `Bearer ${getAccessToken()}`,
      'Content-Type': 'application/json'
    }
  });
  
  if (!response.ok) {
    throw new Error(`Failed to fetch roles: ${response.statusText}`);
  }
  
  const data = await response.json();
  return data.value;
}

// Get paginated roles
async function getRolesPaginated(
  tenantSlug: string,
  page: number = 1,
  pageSize: number = 10,
  searchTerm?: string
): Promise<{ roles: Authority[], totalCount: number }> {
  const skip = (page - 1) * pageSize;
  const queryParams = new URLSearchParams({
    '$top': pageSize.toString(),
    '$skip': skip.toString(),
    '$count': 'true',
    '$orderby': 'Name asc'
  });
  
  if (searchTerm) {
    queryParams.append('$filter', `contains(Name,'${searchTerm}')`);
  }
  
  const response = await fetch(`/${tenantSlug}/odata/Roles?${queryParams}`, {
    headers: {
      'Authorization': `Bearer ${getAccessToken()}`,
      'Content-Type': 'application/json'
    }
  });
  
  const data = await response.json();
  return {
    roles: data.value,
    totalCount: data['@odata.count']
  };
}

// Get role with permissions
async function getRoleWithPermissions(tenantSlug: string, roleId: string): Promise<Authority> {
  const response = await fetch(`/${tenantSlug}/odata/Roles(${roleId})?$expand=Permissions`, {
    headers: {
      'Authorization': `Bearer ${getAccessToken()}`,
      'Content-Type': 'application/json'
    }
  });
  
  if (!response.ok) {
    throw new Error(`Failed to fetch role: ${response.statusText}`);
  }
  
  return await response.json();
}
```

### React Hook Example

```typescript
import { useState, useEffect } from 'react';
import useSWR from 'swr';

interface UseRolesOptions {
  filter?: string;
  orderBy?: string;
  pageSize?: number;
  searchTerm?: string;
}

export function useRoles(tenantSlug: string, options: UseRolesOptions = {}) {
  const { filter, orderBy = 'Name asc', pageSize = 10, searchTerm } = options;
  
  // Build OData query parameters
  const buildQuery = () => {
    const params = new URLSearchParams({
      '$orderby': orderBy,
      '$count': 'true'
    });
    
    if (pageSize) {
      params.append('$top', pageSize.toString());
    }
    
    const filters = [];
    if (filter) filters.push(filter);
    if (searchTerm) filters.push(`contains(Name,'${searchTerm}')`);
    
    if (filters.length > 0) {
      params.append('$filter', filters.join(' and '));
    }
    
    return params.toString();
  };
  
  const { data, error, isLoading, mutate } = useSWR(
    `/${tenantSlug}/odata/Roles?${buildQuery()}`,
    async (url) => {
      const response = await fetch(url, {
        headers: {
          'Authorization': `Bearer ${getAccessToken()}`,
          'Content-Type': 'application/json'
        }
      });
      
      if (!response.ok) {
        throw new Error('Failed to fetch roles');
      }
      
      return response.json();
    }
  );
  
  return {
    roles: data?.value || [],
    totalCount: data?.['@odata.count'] || 0,
    isLoading,
    error,
    refetch: mutate
  };
}

// Usage in component
function RolesPage() {
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  
  const { roles, totalCount, isLoading, error } = useRoles('my-tenant', {
    searchTerm,
    pageSize: 10
  });
  
  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;
  
  return (
    <div>
      <input 
        value={searchTerm} 
        onChange={(e) => setSearchTerm(e.target.value)}
        placeholder="Search roles..."
      />
      
      <div className="roles-list">
        {roles.map(role => (
          <div key={role.id} className="role-card">
            <h3>{role.name}</h3>
            <p>{role.description}</p>
            <span>{role.isSystemAuthority ? 'System' : 'Custom'}</span>
          </div>
        ))}
      </div>
      
      <div className="pagination">
        Total: {totalCount} roles
        {/* Add pagination controls here */}
      </div>
    </div>
  );
}
```

## Common Query Patterns

### 1. Dashboard Overview
```
GET /{tenant}/odata/Roles?$select=Id,Name&$filter=IsSystemAuthority eq false&$orderby=Name asc
```

### 2. Role Management Grid
```
GET /{tenant}/odata/Roles?$select=Id,Name,Description,IsSystemAuthority,CreatedAt&$orderby=CreatedAt desc&$count=true&$top=20
```

### 3. Role Dropdown/Select
```
GET /{tenant}/odata/Roles?$select=Id,Name&$filter=IsSystemAuthority eq false&$orderby=Name asc
```

### 4. Advanced Search
```
GET /{tenant}/odata/Roles?$filter=contains(tolower(Name),'admin') and IsSystemAuthority eq false&$select=Id,Name,Description&$orderby=Name asc
```

### 5. Permission Analysis
```
GET /{tenant}/odata/Roles?$expand=Permissions&$filter=Permissions/any(p: p/ResourceName eq 'BotAgent')
```

## Error Handling

The endpoint returns standard HTTP status codes:

- **200**: Success
- **400**: Bad Request (invalid OData query)
- **401**: Unauthorized (missing or invalid token)
- **403**: Forbidden (insufficient permissions)
- **404**: Not Found (specific role not found)
- **500**: Internal Server Error

### Example Error Response
```json
{
  "error": {
    "code": "ValidationError",
    "message": "Invalid OData query parameter"
  }
}
```

## Security Considerations

1. **Tenant Isolation**: All queries are automatically filtered by the current tenant context
2. **Authentication**: JWT Bearer token required
3. **Authorization**: Requires appropriate permissions for the OrganizationUnitResource
4. **Query Limits**: Maximum 100 records per request (`$top=100`)
5. **Field Access**: All fields in AuthorityWithPermissionsDto are accessible based on user permissions

## Performance Tips

1. **Use $select**: Only request fields you need
2. **Implement Pagination**: Use $top and $skip for large datasets
3. **Filter Server-Side**: Use $filter instead of client-side filtering
4. **Cache Results**: Implement appropriate caching strategies in your frontend
5. **Index Usage**: Queries on Name, CreatedAt, and IsSystemAuthority are optimized

## Migration from REST API

If migrating from the existing REST API (`/api/author/authorities`), here are the equivalent OData queries:

| REST Endpoint | OData Equivalent |
|--------------|------------------|
| `GET /api/author/authorities` | `GET /{tenant}/odata/Roles` |
| `GET /api/author/authority/{id}` | `GET /{tenant}/odata/Roles({id})` |
| Search by name | `GET /{tenant}/odata/Roles?$filter=contains(Name,'search')` |
| System roles only | `GET /{tenant}/odata/Roles?$filter=IsSystemAuthority eq true` |
| Custom roles only | `GET /{tenant}/odata/Roles?$filter=IsSystemAuthority eq false` |

## Support

For additional support or questions about the OData Roles endpoint, please refer to:
- [OData v4 Documentation](https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html)
- [ASP.NET Core OData Documentation](https://docs.microsoft.com/en-us/odata/webapi/getting-started)
- Internal API documentation and team resources 