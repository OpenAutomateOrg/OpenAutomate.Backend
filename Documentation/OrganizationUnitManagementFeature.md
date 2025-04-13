# Organization Unit Management Feature

## Overview
This document outlines the implementation of the Organization Unit management feature in the OpenAutomate platform. Organization Units represent tenant spaces in our multi-tenant architecture, allowing users to create isolated environments for their automation resources, team members, and configurations.

## Business Requirements

### Core Functionality
1. Users should be able to create new organization units with a unique name
2. The system should automatically generate URL-friendly slugs from organization unit names
3. Users should be able to view, update (including name changes), and deactivate organization units they have access to
4. When an organization unit name is changed, the system should update the slug accordingly, with clear warnings about the impact
5. Organization unit owners should be able to add and remove users from their organization units
6. Users should be able to see all organization units they belong to
7. The platform should enforce tenant isolation between organization units

### User Stories
- As a user, I want to create a new organization unit so that I can manage my automation resources in an isolated environment
- As an organization unit owner, I want to invite team members to my organization unit so that they can collaborate on automation projects
- As a user, I want to switch between organization units I belong to so that I can work on different automation projects
- As an organization unit owner, I want to update my organization unit name and description so that they reflect changes in my business, understanding that URL paths may change
- As a platform administrator, I want to ensure data isolation between organization units to maintain security and privacy

## Technical Design

### Domain Model

#### OrganizationUnit Entity
```csharp
public class OrganizationUnit : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Slug { get; set; } // URL-friendly identifier for routing, auto-generated from name
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<OrganizationUnitUser> OrganizationUnitUsers { get; set; }
    public virtual ICollection<BotAgent> BotAgents { get; set; }
    public virtual ICollection<AutomationPackage> AutomationPackages { get; set; }
    public virtual ICollection<Execution> Executions { get; set; }
    public virtual ICollection<Schedule> Schedules { get; set; }
}
```

#### OrganizationUnitUser Entity
```csharp
public class OrganizationUnitUser
{
    [Required]
    public Guid UserId { set; get; }
    [ForeignKey("UserId")]
    public User User { get; set; }

    [Required]
    public Guid OrganizationUnitId { set; get; }
    [ForeignKey("OrganizationUnitId")]
    public OrganizationUnit OrganizationUnit { get; set; }
    
    // To be added in a future update
    public string Role { get; set; } = "Member"; // Default role
}
```

### Data Transfer Objects (DTOs)

#### CreateOrganizationUnitDto
```csharp
public class CreateOrganizationUnitDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    // No slug required - will be generated automatically
}
```

#### UpdateOrganizationUnitDto
```csharp
public class UpdateOrganizationUnitDto
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

#### OrganizationUnitResponseDto
```csharp
public class OrganizationUnitResponseDto
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

#### OrganizationUnitUserDto
```csharp
public class OrganizationUnitUserDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}
```

#### AddUserToOrganizationUnitDto
```csharp
public class AddUserToOrganizationUnitDto
{
    public Guid UserId { get; set; }
    // For future implementation with roles
    public string Role { get; set; } = "Member";
}
```

### Service Interface

```csharp
public interface IOrganizationUnitService
{
    Task<OrganizationUnitResponseDto> CreateOrganizationUnitAsync(CreateOrganizationUnitDto dto, Guid userId);
    Task<OrganizationUnitResponseDto> GetOrganizationUnitByIdAsync(Guid organizationUnitId);
    Task<OrganizationUnitResponseDto> GetOrganizationUnitBySlugAsync(string slug);
    Task<IEnumerable<OrganizationUnitResponseDto>> GetUserOrganizationUnitsAsync(Guid userId);
    Task<SlugChangeWarningDto> CheckNameChangeImpactAsync(Guid organizationUnitId, string newName);
    Task<OrganizationUnitResponseDto> UpdateOrganizationUnitAsync(Guid organizationUnitId, UpdateOrganizationUnitDto dto);
    Task<bool> DeactivateOrganizationUnitAsync(Guid organizationUnitId);
    Task<bool> AddUserToOrganizationUnitAsync(Guid organizationUnitId, Guid userId, string role = "Member");
    Task<bool> RemoveUserFromOrganizationUnitAsync(Guid organizationUnitId, Guid userId);
    Task<IEnumerable<OrganizationUnitUserDto>> GetOrganizationUnitUsersAsync(Guid organizationUnitId);
    Task<bool> UserHasAccessToOrganizationUnitAsync(Guid organizationUnitId, Guid userId);
}
```

### API Endpoints

#### OrganizationUnitController
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationUnitController : CustomControllerBase
{
    // Endpoints for CRUD operations
    [HttpPost]
    public async Task<ActionResult<OrganizationUnitResponseDto>> CreateOrganizationUnit(CreateOrganizationUnitDto dto);
    
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<OrganizationUnitResponseDto>>> GetMyOrganizationUnits();
    
    [HttpGet("{id}")]
    public async Task<ActionResult<OrganizationUnitResponseDto>> GetOrganizationUnit(Guid id);
    
    [HttpGet("by-slug/{slug}")]
    public async Task<ActionResult<OrganizationUnitResponseDto>> GetOrganizationUnitBySlug(string slug);
    
    // Check impact of name changes before updating
    [HttpGet("{id}/name-change-impact")]
    public async Task<ActionResult<SlugChangeWarningDto>> CheckNameChangeImpact(Guid id, [FromQuery] string newName);
    
    [HttpPut("{id}")]
    public async Task<ActionResult<OrganizationUnitResponseDto>> UpdateOrganizationUnit(Guid id, UpdateOrganizationUnitDto dto);
    
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeactivateOrganizationUnit(Guid id);
    
    // User management within organization units
    [HttpGet("{id}/users")]
    public async Task<ActionResult<IEnumerable<OrganizationUnitUserDto>>> GetOrganizationUnitUsers(Guid id);
    
    [HttpPost("{id}/users")]
    public async Task<ActionResult> AddUserToOrganizationUnit(Guid id, AddUserToOrganizationUnitDto dto);
    
    [HttpDelete("{id}/users/{userId}")]
    public async Task<ActionResult> RemoveUserFromOrganizationUnit(Guid id, Guid userId);
}
```

## Implementation Details

### Slug Generation Logic

The service will automatically generate a URL-friendly slug from the organization unit name using the following algorithm:

1. Convert the name to lowercase
2. Remove all non-alphanumeric characters
3. Replace spaces with empty strings
4. Check if the generated slug already exists in the database
5. If the slug exists, append a random suffix to make it unique

Example:
- Organization Unit Name: "FPT Corp" → Slug: "fptcorp"
- If "fptcorp" already exists → Slug: "fptcorpnwrqao" (with random suffix)

```csharp
private async Task<string> GenerateUniqueSlugAsync(string name)
{
    // Convert to lowercase and remove special characters
    var slug = Regex.Replace(name.ToLower(), @"[^a-z0-9\s]", "");
    
    // Replace spaces
    slug = slug.Replace(" ", "");
    
    // Check if slug exists
    if (!await _unitOfWork.OrganizationUnits.AnyAsync(o => o.Slug == slug))
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

When an organization unit name change is requested, the system will:

1. Generate a potential new slug based on the new name
2. Determine if this would change the organization unit's URL path
3. Analyze potential impacts on connected services
4. Return a warning to the user with details about the impact
5. Require explicit confirmation before proceeding with the change

```csharp
public async Task<SlugChangeWarningDto> CheckNameChangeImpactAsync(Guid organizationUnitId, string newName)
{
    var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(organizationUnitId);
    
    if (organizationUnit == null)
        throw new EntityNotFoundException("Organization unit not found");
        
    var newSlug = await GenerateUniqueSlugAsync(newName);
    
    // Generate list of potential impacts
    var potentialImpacts = new List<string>
    {
        "Organization unit URL paths will change, breaking existing bookmarks",
        "API clients using the current slug will need to be updated",
        "Users may need to be notified about the new organization unit URL"
    };
    
    // Additional impacts based on organization unit's connected services
    if ((await _unitOfWork.BotAgents.FindAsync(a => a.OrganizationUnitId == organizationUnitId)).Any())
        potentialImpacts.Add("Connected bot agents may need reconfiguration");
        
    if ((await _unitOfWork.Schedules.FindAsync(s => s.OrganizationUnitId == organizationUnitId)).Any())
        potentialImpacts.Add("Scheduled tasks referencing the organization unit may be affected");
    
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
```

### OrganizationUnitService Implementation

The `OrganizationUnitService` class will implement the `IOrganizationUnitService` interface and handle all business logic related to organization units:

- Organization unit creation with automatic slug generation
- Organization unit retrieval by ID and slug
- Listing organization units for a specific user
- Organization unit updates with name and slug changes
- Detecting and handling impacts of slug changes
- Organization unit deactivation (soft delete)
- Managing organization unit membership
- Permission checking for organization unit operations

Key validation logic includes:
- Ensuring organization unit names are unique
- Requiring confirmation for name changes that affect slugs
- Automatically generating URL-friendly slugs
- Validating user permissions for organization unit operations
- Preventing the removal of the last owner from an organization unit

### Multi-Tenant Integration

The Organization Unit feature is a core part of our multi-tenant architecture. Key integration points include:

1. **Tenant Resolution**:
   - Organization units are identified by slug in the URL path
   - When a request is received, the appropriate organization unit context is loaded
   - All subsequent database operations are filtered by the current organization unit
   - When slugs change, proper redirects should be in place for a transition period

2. **Data Isolation**:
   - Entity Framework Core query filters ensure that only data belonging to the current organization unit is accessible
   - All service methods validate that the user has access to the requested organization unit
   - Cross-organization unit operations are prevented by design

3. **User Context**:
   - Users can belong to multiple organization units
   - The current organization unit context is determined by the URL path
   - User permissions may vary between organization units

4. **Slug Change Management**:
   - When an organization unit slug changes, a temporary redirect system will capture requests to the old slug
   - System will maintain a history of previous slugs to ensure backward compatibility
   - Clients will be encouraged to update to the new slug path

## Validation Rules

### CreateOrganizationUnitDto Validation
- Name: Required, between 3-100 characters, must be unique
- Description: Optional, max 500 characters

### UpdateOrganizationUnitDto Validation
- Name: Optional, between 3-100 characters, must be unique if provided
- Description: Optional, max 500 characters
- ConfirmSlugChange: Required to be true if name changes would result in slug changes

### Organization Unit Operations Validation
- Only users with appropriate permissions can update or deactivate an organization unit
- Name changes require explicit confirmation if they would change the organization unit slug
- Deactivated organization units cannot be accessed except by system administrators

## Security Considerations

1. **Authorization Checks**:
   - All organization unit operations require appropriate permissions
   - Organization unit membership does not automatically grant administrative access
   - Only owners and administrators can change organization unit names
   - System administrators can access all organization units

2. **Data Isolation**:
   - Strict tenant isolation prevents cross-organization unit data access
   - All repository queries include organization unit filters
   - Validation is performed at multiple levels (controller, service, repository)

3. **Audit Trail**:
   - All organization unit changes are logged with the user who made the change
   - Organization unit creation, updates, and user management actions are tracked
   - Slug changes are specifically logged with old and new values

## Error Handling

The organization unit management feature will implement robust error handling:

- **Validation Errors**: Return 400 Bad Request with detailed validation messages
- **Not Found Errors**: Return 404 Not Found when organization units don't exist
- **Permission Errors**: Return 403 Forbidden when users lack necessary permissions
- **Conflict Errors**: Return 409 Conflict when trying to create organization units with duplicate names
- **Confirmation Errors**: Return 422 Unprocessable Entity when slug changes are attempted without confirmation
- **Server Errors**: Return 500 Internal Server Error with appropriate logging

## Frontend Integration

The frontend will implement a user-friendly flow for organization unit management, aligning with our established UI patterns:

1. **Organization Unit Management UI Design:**
   - Organization unit management screens will follow our UiPath-inspired UI pattern with clean, consistent layouts
   - Card-based design for organization unit listings with clear visual indicators for active status
   - Consistent button hierarchy with primary actions using button hover effects (scale and shadow on hover)
   - Form validation with immediate feedback following the form patterns established in auth workflows

2. **Organization Unit Creation Flow:**
   - Simple, focused form with clear field validation
   - Real-time validation of organization unit name uniqueness
   - Visual preview of the generated slug as the user types
   - Confirmation dialog with clear action buttons following our button hierarchy

3. **Organization Unit Name Change Flow:**
   - When a user attempts to change an organization unit name, an API call to `CheckNameChangeImpact` is made
   - If the new name would change the slug, a warning dialog is displayed with:
     - The current slug and the new slug
     - A list of potential impacts on connected services
     - A checkbox for confirming understanding of the changes
   - Dialog uses consistent button styling with destructive variant for potential data-impacting changes
   - Only after explicit confirmation can the user proceed with the name change
   - After the change, a toast notification is shown with the new organization unit URL

4. **Organization Unit Switching:**
   - Dropdown menu in the header for quick organization unit switching
   - Visual indicator of the current active organization unit 
   - Smooth transition between organization units with proper loading states
   - Breadcrumb navigation showing the current organization unit context

5. **Organization Unit User Management:**
   - Table-based view of organization unit members with clear role indicators
   - Modal dialogs for adding/removing users with consistent styling
   - Immediate UI feedback when permissions change
   - Proper error handling for permission-related issues

All UIs will follow our established design system using Shadcn UI components with consistent spacing, typography, and color usage. The user experience will prioritize clarity and ease of use, with appropriate visual feedback for all actions.

## Future Enhancements

1. **Slug History and Redirects**:
   - Maintain history of previous organization unit slugs
   - Implement automatic redirects from old slugs to new ones
   - Add analytics for tracking how often old slugs are accessed

2. **Role-Based Access Control**:
   - Implement detailed roles within organization units (Owner, Admin, Member, ReadOnly)
   - Add permission checks based on roles

3. **Organization Unit Settings**:
   - Allow customization of organization unit settings
   - Support for organization unit-specific configurations

4. **Invitation System**:
   - Email-based invitation for new users
   - User acceptance workflow for organization unit invitations

5. **Organization Unit Hierarchies**:
   - Support for parent-child organization unit relationships
   - Inherited permissions and resource sharing between related organization units

6. **Organization Unit Analytics**:
   - Usage metrics for organization units
   - Resource utilization tracking

## Testing Strategy

### Unit Tests
- Test all service methods with various inputs
- Test slug generation logic with different organization unit names
- Test validation rules for all DTOs
- Test slug change impact detection
- Mock repository dependencies

### Integration Tests
- Test API endpoints with real database interactions
- Test tenant isolation across organization units
- Test user permission enforcement
- Test slug uniqueness handling
- Test name change impact analysis
- Test slug change confirmation requirements

### End-to-End Tests
- Test the complete organization unit management flow
- Test organization unit name changes and slug updates
- Test organization unit switching and context changes
- Test with multiple concurrent users
- Test client behavior when organization unit slugs change

## Implementation Timeline

1. **Phase 1**: Core Organization Unit CRUD operations
   - Create Organization unit entity and DTOs
   - Implement automatic slug generation
   - Implement basic OrganizationUnitService
   - Create OrganizationUnitController with endpoints

2. **Phase 2**: Name and Slug Change Management
   - Add name change impact analysis
   - Implement confirmation workflow for slug changes
   - Create temporary redirect system for changed slugs

3. **Phase 3**: User Management within Organization Units
   - Add endpoints for managing users
   - Implement permission checking
   - Update tenant resolution with organization unit context

4. **Phase 4**: Frontend Integration
   - Create UI components for organization unit management
   - Implement organization unit switching
   - Add name change confirmation UI
   - Add user management interface

5. **Phase 5**: Advanced Features
   - Implement role-based permissions
   - Add organization unit settings
   - Create invitation workflow 