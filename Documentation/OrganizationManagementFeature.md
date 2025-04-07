# Organization Management Feature

## Overview
This document outlines the implementation of the Organization management feature in the OpenAutomate platform. Organizations represent tenant spaces in our multi-tenant architecture, allowing users to create isolated environments for their automation resources, team members, and configurations.

## Business Requirements

### Core Functionality
1. Users should be able to create new organizations with a unique name
2. The system should automatically generate URL-friendly slugs from organization names
3. Users should be able to view, update (including name changes), and deactivate organizations they have access to
4. When an organization name is changed, the system should update the slug accordingly, with clear warnings about the impact
5. Organization owners should be able to add and remove users from their organizations
6. Users should be able to see all organizations they belong to
7. The platform should enforce tenant isolation between organizations

### User Stories
- As a user, I want to create a new organization so that I can manage my automation resources in an isolated environment
- As an organization owner, I want to invite team members to my organization so that they can collaborate on automation projects
- As a user, I want to switch between organizations I belong to so that I can work on different automation projects
- As an organization owner, I want to update my organization name and description so that they reflect changes in my business, understanding that URL paths may change
- As a platform administrator, I want to ensure data isolation between organizations to maintain security and privacy

## Technical Design

### Domain Model

#### Organization Entity
```csharp
public class Organization : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Slug { get; set; } // URL-friendly identifier for routing, auto-generated from name
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<OrganizationUser> OrganizationUsers { get; set; }
    public virtual ICollection<BotAgent> BotAgents { get; set; }
    public virtual ICollection<AutomationPackage> AutomationPackages { get; set; }
    public virtual ICollection<Execution> Executions { get; set; }
    public virtual ICollection<Schedule> Schedules { get; set; }
}
```

#### OrganizationUser Entity
```csharp
public class OrganizationUser
{
    [Required]
    public Guid UserId { set; get; }
    [ForeignKey("UserId")]
    public User User { get; set; }

    [Required]
    public Guid OrganizationId { set; get; }
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; }
    
    // To be added in a future update
    public string Role { get; set; } = "Member"; // Default role
}
```

### Data Transfer Objects (DTOs)

#### CreateOrganizationDto
```csharp
public class CreateOrganizationDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    // No slug required - will be generated automatically
}
```

#### UpdateOrganizationDto
```csharp
public class UpdateOrganizationDto
{
    public string Name { get; set; } // Now included to allow name changes
    public string Description { get; set; }
    public bool ConfirmSlugChange { get; set; } // Required confirmation when name changes would alter slug
}
```

#### SlugChangeWarningDto
```csharp
public class SlugChangeWarningDto
{
    public string CurrentName { get; set; }
    public string CurrentSlug { get; set; }
    public string NewName { get; set; }
    public string NewSlug { get; set; }
    public string[] PotentialImpacts { get; set; }
    public bool RequiresConfirmation { get; set; } = true;
}
```

#### OrganizationResponseDto
```csharp
public class OrganizationResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Slug { get; set; } // Auto-generated from name
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
}
```

#### OrganizationUserDto
```csharp
public class OrganizationUserDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}
```

#### AddUserToOrganizationDto
```csharp
public class AddUserToOrganizationDto
{
    public Guid UserId { get; set; }
    // For future implementation with roles
    public string Role { get; set; } = "Member";
}
```

### Service Interface

```csharp
public interface IOrganizationService
{
    Task<OrganizationResponseDto> CreateOrganizationAsync(CreateOrganizationDto dto, Guid userId);
    Task<OrganizationResponseDto> GetOrganizationByIdAsync(Guid organizationId);
    Task<OrganizationResponseDto> GetOrganizationBySlugAsync(string slug);
    Task<IEnumerable<OrganizationResponseDto>> GetUserOrganizationsAsync(Guid userId);
    Task<SlugChangeWarningDto> CheckNameChangeImpactAsync(Guid organizationId, string newName);
    Task<OrganizationResponseDto> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationDto dto);
    Task<bool> DeactivateOrganizationAsync(Guid organizationId);
    Task<bool> AddUserToOrganizationAsync(Guid organizationId, Guid userId, string role = "Member");
    Task<bool> RemoveUserFromOrganizationAsync(Guid organizationId, Guid userId);
    Task<IEnumerable<OrganizationUserDto>> GetOrganizationUsersAsync(Guid organizationId);
    Task<bool> UserHasAccessToOrganizationAsync(Guid organizationId, Guid userId);
}
```

### API Endpoints

#### OrganizationController
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationController : CustomControllerBase
{
    // Endpoints for CRUD operations
    [HttpPost]
    public async Task<ActionResult<OrganizationResponseDto>> CreateOrganization(CreateOrganizationDto dto);
    
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<OrganizationResponseDto>>> GetMyOrganizations();
    
    [HttpGet("{id}")]
    public async Task<ActionResult<OrganizationResponseDto>> GetOrganization(Guid id);
    
    [HttpGet("by-slug/{slug}")]
    public async Task<ActionResult<OrganizationResponseDto>> GetOrganizationBySlug(string slug);
    
    // Check impact of name changes before updating
    [HttpGet("{id}/name-change-impact")]
    public async Task<ActionResult<SlugChangeWarningDto>> CheckNameChangeImpact(Guid id, [FromQuery] string newName);
    
    [HttpPut("{id}")]
    public async Task<ActionResult<OrganizationResponseDto>> UpdateOrganization(Guid id, UpdateOrganizationDto dto);
    
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeactivateOrganization(Guid id);
    
    // User management within organizations
    [HttpGet("{id}/users")]
    public async Task<ActionResult<IEnumerable<OrganizationUserDto>>> GetOrganizationUsers(Guid id);
    
    [HttpPost("{id}/users")]
    public async Task<ActionResult> AddUserToOrganization(Guid id, AddUserToOrganizationDto dto);
    
    [HttpDelete("{id}/users/{userId}")]
    public async Task<ActionResult> RemoveUserFromOrganization(Guid id, Guid userId);
}
```

## Implementation Details

### Slug Generation Logic

The service will automatically generate a URL-friendly slug from the organization name using the following algorithm:

1. Convert the name to lowercase
2. Remove all non-alphanumeric characters
3. Replace spaces with empty strings
4. Check if the generated slug already exists in the database
5. If the slug exists, append a random suffix to make it unique

Example:
- Organization Name: "FPT Corp" → Slug: "fptcorp"
- If "fptcorp" already exists → Slug: "fptcorpnwrqao" (with random suffix)

```csharp
private async Task<string> GenerateUniqueSlugAsync(string name)
{
    // Convert to lowercase and remove special characters
    var slug = Regex.Replace(name.ToLower(), @"[^a-z0-9\s]", "");
    
    // Replace spaces
    slug = slug.Replace(" ", "");
    
    // Check if slug exists
    if (!await _unitOfWork.Organizations.AnyAsync(o => o.Slug == slug))
    {
        return slug;
    }
    
    // If slug exists, add a random suffix
    var random = new Random();
    var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    var suffix = new string(Enumerable.Repeat(chars, 6)
        .Select(s => s[random.Next(s.Length)]).ToArray());
    
    return $"{slug}{suffix}";
}
```

### Name Change Impact Management

When an organization name change is requested, the system will:

1. Generate a potential new slug based on the new name
2. Determine if this would change the organization's URL path
3. Analyze potential impacts on connected services
4. Return a warning to the user with details about the impact
5. Require explicit confirmation before proceeding with the change

```csharp
public async Task<SlugChangeWarningDto> CheckNameChangeImpactAsync(Guid organizationId, string newName)
{
    var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId);
    
    if (organization == null)
        throw new EntityNotFoundException("Organization not found");
        
    var newSlug = await GenerateUniqueSlugAsync(newName);
    
    // Generate list of potential impacts
    var potentialImpacts = new List<string>
    {
        "Organization URL paths will change, breaking existing bookmarks",
        "API clients using the current slug will need to be updated",
        "Users may need to be notified about the new organization URL"
    };
    
    // Additional impacts based on organization's connected services
    if ((await _unitOfWork.BotAgents.FindAsync(a => a.OrganizationId == organizationId)).Any())
        potentialImpacts.Add("Connected bot agents may need reconfiguration");
        
    if ((await _unitOfWork.Schedules.FindAsync(s => s.OrganizationId == organizationId)).Any())
        potentialImpacts.Add("Scheduled tasks referencing the organization may be affected");
    
    return new SlugChangeWarningDto
    {
        CurrentName = organization.Name,
        CurrentSlug = organization.Slug,
        NewName = newName,
        NewSlug = newSlug,
        PotentialImpacts = potentialImpacts.ToArray(),
        RequiresConfirmation = true
    };
}
```

### OrganizationService Implementation

The `OrganizationService` class will implement the `IOrganizationService` interface and handle all business logic related to organizations:

- Organization creation with automatic slug generation
- Organization retrieval by ID and slug
- Listing organizations for a specific user
- Organization updates with name and slug changes
- Detecting and handling impacts of slug changes
- Organization deactivation (soft delete)
- Managing organization membership
- Permission checking for organization operations

Key validation logic includes:
- Ensuring organization names are unique
- Requiring confirmation for name changes that affect slugs
- Automatically generating URL-friendly slugs
- Validating user permissions for organization operations
- Preventing the removal of the last owner from an organization

### Multi-Tenant Integration

The Organization feature is a core part of our multi-tenant architecture. Key integration points include:

1. **Tenant Resolution**:
   - Organizations are identified by slug in the URL path
   - When a request is received, the appropriate organization context is loaded
   - All subsequent database operations are filtered by the current organization
   - When slugs change, proper redirects should be in place for a transition period

2. **Data Isolation**:
   - Entity Framework Core query filters ensure that only data belonging to the current organization is accessible
   - All service methods validate that the user has access to the requested organization
   - Cross-organization operations are prevented by design

3. **User Context**:
   - Users can belong to multiple organizations
   - The current organization context is determined by the URL path
   - User permissions may vary between organizations

4. **Slug Change Management**:
   - When an organization slug changes, a temporary redirect system will capture requests to the old slug
   - System will maintain a history of previous slugs to ensure backward compatibility
   - Clients will be encouraged to update to the new slug path

## Validation Rules

### CreateOrganizationDto Validation
- Name: Required, between 3-100 characters, must be unique
- Description: Optional, max 500 characters

### UpdateOrganizationDto Validation
- Name: Optional, between 3-100 characters, must be unique if provided
- Description: Optional, max 500 characters
- ConfirmSlugChange: Required to be true if name changes would result in slug changes

### Organization Operations Validation
- Only users with appropriate permissions can update or deactivate an organization
- Name changes require explicit confirmation if they would change the organization slug
- Deactivated organizations cannot be accessed except by system administrators

## Security Considerations

1. **Authorization Checks**:
   - All organization operations require appropriate permissions
   - Organization membership does not automatically grant administrative access
   - Only owners and administrators can change organization names
   - System administrators can access all organizations

2. **Data Isolation**:
   - Strict tenant isolation prevents cross-organization data access
   - All repository queries include organization filters
   - Validation is performed at multiple levels (controller, service, repository)

3. **Audit Trail**:
   - All organization changes are logged with the user who made the change
   - Organization creation, updates, and user management actions are tracked
   - Slug changes are specifically logged with old and new values

## Error Handling

The organization management feature will implement robust error handling:

- **Validation Errors**: Return 400 Bad Request with detailed validation messages
- **Not Found Errors**: Return 404 Not Found when organizations don't exist
- **Permission Errors**: Return 403 Forbidden when users lack necessary permissions
- **Conflict Errors**: Return 409 Conflict when trying to create organizations with duplicate names
- **Confirmation Errors**: Return 422 Unprocessable Entity when slug changes are attempted without confirmation
- **Server Errors**: Return 500 Internal Server Error with appropriate logging

## Frontend Integration

The frontend will implement a user-friendly flow for organization management, aligning with our established UI patterns:

1. **Organization Management UI Design:**
   - Organization management screens will follow our UiPath-inspired UI pattern with clean, consistent layouts
   - Card-based design for organization listings with clear visual indicators for active status
   - Consistent button hierarchy with primary actions using button hover effects (scale and shadow on hover)
   - Form validation with immediate feedback following the form patterns established in auth workflows

2. **Organization Creation Flow:**
   - Simple, focused form with clear field validation
   - Real-time validation of organization name uniqueness
   - Visual preview of the generated slug as the user types
   - Confirmation dialog with clear action buttons following our button hierarchy

3. **Organization Name Change Flow:**
   - When a user attempts to change an organization name, an API call to `CheckNameChangeImpact` is made
   - If the new name would change the slug, a warning dialog is displayed with:
     - The current slug and the new slug
     - A list of potential impacts on connected services
     - A checkbox for confirming understanding of the changes
   - Dialog uses consistent button styling with destructive variant for potential data-impacting changes
   - Only after explicit confirmation can the user proceed with the name change
   - After the change, a toast notification is shown with the new organization URL

4. **Organization Switching:**
   - Dropdown menu in the header for quick organization switching
   - Visual indicator of the current active organization 
   - Smooth transition between organizations with proper loading states
   - Breadcrumb navigation showing the current organization context

5. **Organization User Management:**
   - Table-based view of organization members with clear role indicators
   - Modal dialogs for adding/removing users with consistent styling
   - Immediate UI feedback when permissions change
   - Proper error handling for permission-related issues

All UIs will follow our established design system using Shadcn UI components with consistent spacing, typography, and color usage. The user experience will prioritize clarity and ease of use, with appropriate visual feedback for all actions.

## Future Enhancements

1. **Slug History and Redirects**:
   - Maintain history of previous organization slugs
   - Implement automatic redirects from old slugs to new ones
   - Add analytics for tracking how often old slugs are accessed

2. **Role-Based Access Control**:
   - Implement detailed roles within organizations (Owner, Admin, Member, ReadOnly)
   - Add permission checks based on roles

3. **Organization Settings**:
   - Allow customization of organization settings
   - Support for organization-specific configurations

4. **Invitation System**:
   - Email-based invitation for new users
   - User acceptance workflow for organization invitations

5. **Organization Hierarchies**:
   - Support for parent-child organization relationships
   - Inherited permissions and resource sharing between related organizations

6. **Organization Analytics**:
   - Usage metrics for organizations
   - Resource utilization tracking

## Testing Strategy

### Unit Tests
- Test all service methods with various inputs
- Test slug generation logic with different organization names
- Test validation rules for all DTOs
- Test slug change impact detection
- Mock repository dependencies

### Integration Tests
- Test API endpoints with real database interactions
- Test tenant isolation across organizations
- Test user permission enforcement
- Test slug uniqueness handling
- Test name change impact analysis
- Test slug change confirmation requirements

### End-to-End Tests
- Test the complete organization management flow
- Test organization name changes and slug updates
- Test organization switching and context changes
- Test with multiple concurrent users
- Test client behavior when organization slugs change

## Implementation Timeline

1. **Phase 1**: Core Organization CRUD operations
   - Create Organization entity and DTOs
   - Implement automatic slug generation
   - Implement basic OrganizationService
   - Create OrganizationController with endpoints

2. **Phase 2**: Name and Slug Change Management
   - Add name change impact analysis
   - Implement confirmation workflow for slug changes
   - Create temporary redirect system for changed slugs

3. **Phase 3**: User Management within Organizations
   - Add endpoints for managing users
   - Implement permission checking
   - Update tenant resolution with organization context

4. **Phase 4**: Frontend Integration
   - Create UI components for organization management
   - Implement organization switching
   - Add name change confirmation UI
   - Add user management interface

5. **Phase 5**: Advanced Features
   - Implement role-based permissions
   - Add organization settings
   - Create invitation workflow 