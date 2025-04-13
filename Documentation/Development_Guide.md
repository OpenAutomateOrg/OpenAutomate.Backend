# OpenAutomate Development Guide

This guide provides essential information for developers working on the OpenAutomate project. It covers both backend and frontend development, including how to add new features, follow project conventions, and leverage existing components.

## Table of Contents

1. [Project Architecture Overview](#project-architecture-overview)
2. [Backend Development](#backend-development)
   - [Project Structure](#backend-project-structure)
   - [Multi-Tenant Development](#multi-tenant-development)
   - [Adding New Entities](#adding-new-entities)
   - [Creating API Endpoints](#creating-api-endpoints)
   - [Authentication & Authorization](#authentication--authorization)
   - [Error Handling](#error-handling)
   - [EF Core Best Practices](#ef-core-best-practices)
3. [Frontend Development](#frontend-development)
   - [Project Structure](#frontend-project-structure)
   - [Authentication & Tenant Context](#authentication--tenant-context)
   - [Creating New Pages](#creating-new-pages)
   - [Creating New Components](#creating-new-components)
   - [State Management](#state-management)
   - [API Integration](#api-integration)
   - [Server vs. Client Components](#server-vs-client-components)
4. [Development Workflow](#development-workflow)
   - [Setting Up the Development Environment](#setting-up-the-development-environment)
   - [Running the Application](#running-the-application)
   - [Testing](#testing)
   - [Pull Request Process](#pull-request-process)

## Project Architecture Overview

OpenAutomate is an open-source business process automation management platform built with:

- **Backend**: ASP.NET Core 8.0 with clean architecture
- **Frontend**: Next.js 14 with App Router and TypeScript
- **Authentication**: JWT with refresh tokens (HTTP-only cookies)
- **Multi-Tenant Architecture**: Shared database with tenant filtering
- **Database**: Entity Framework Core with global query filters for tenant isolation

The architecture follows these core principles:

1. **Clean Architecture**: Separation of concerns with Core, Infrastructure, and API layers
2. **Multi-Tenancy**: All tenant-specific data is isolated through global query filters
3. **Provider-Based Frontend**: Authentication and tenant state managed through context providers

## Backend Development

### Backend Project Structure

The backend solution consists of three main projects:

1. **OpenAutomate.Core**: Contains domain models, interfaces, and business logic
   - Domain/Entities: Domain models
   - Domain/Interfaces: Interfaces for repositories and services
   - Domain/Dto: Data Transfer Objects

2. **OpenAutomate.Infrastructure**: Contains implementations of interfaces
   - DbContext: Database context and configuration
   - Repositories: Repository implementations
   - Services: Service implementations
   - Middleware: Custom middleware components

3. **OpenAutomate.API**: Contains API controllers and configuration
   - Controllers: API endpoints grouped by resource
   - Middleware: API-specific middleware

### Multi-Tenant Development

When developing for multi-tenant scenarios, follow these guidelines:

1. **Tenant-Aware Entities**: All tenant-specific entities must include:
   ```csharp
   public Guid OrganizationId { get; set; }
   public virtual Organization Organization { get; set; }
   ```

2. **Repository Usage**: Always use the repository pattern to access data. The repositories automatically apply tenant filtering.
   ```csharp
   // Example: Get filtered data for current tenant
   var items = await _repository.GetAllAsync(filter: null);
   ```

3. **Tenant Context**: Use the tenant context to get the current tenant ID:
   ```csharp
   public class MyService
   {
       private readonly ITenantContext _tenantContext;
       
       public MyService(ITenantContext tenantContext)
       {
           _tenantContext = tenantContext;
       }
       
       public void DoSomething()
       {
           var tenantId = _tenantContext.CurrentTenantId;
           // Use tenant ID as needed
       }
   }
   ```

4. **Cross-Tenant Operations**: For admin functions that need to work across tenants:
   ```csharp
   // Use IgnoreTenantFilter to bypass tenant filtering
   using (_tenantContext.IgnoreTenantFilter())
   {
       var allItems = await _repository.GetAllAsync(filter: null);
   }
   ```

### Adding New Entities

Follow these steps to add a new entity:

1. **Create the Entity Class** in `OpenAutomate.Core/Domain/Entities`:
   ```csharp
   public class MyEntity : BaseEntity
   {
       public string Name { get; set; }
       public string Description { get; set; }
       
       // Tenant relationship - required for tenant isolation
       public Guid OrganizationId { get; set; }
       public virtual Organization Organization { get; set; }
       
       // Any other relationships
       public Guid CreatedById { get; set; }
       public virtual User CreatedBy { get; set; }
   }
   ```

2. **Create DTOs** in `OpenAutomate.Core/Domain/Dto`:
   ```csharp
   // Request DTO
   public class CreateMyEntityRequest
   {
       public string Name { get; set; }
       public string Description { get; set; }
   }
   
   // Response DTO
   public class MyEntityResponse
   {
       public Guid Id { get; set; }
       public string Name { get; set; }
       public string Description { get; set; }
       public DateTime CreatedAt { get; set; }
       public Guid CreatedById { get; set; }
   }
   ```

3. **Add DbSet to ApplicationDbContext** in `OpenAutomate.Infrastructure/DbContext/ApplicationDbContext.cs`:
   ```csharp
   public DbSet<MyEntity> MyEntities { get; set; }
   ```

4. **Configure Entity** in `OnModelCreating` if needed:
   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       // Existing code...
       
       modelBuilder.Entity<MyEntity>()
           .HasOne(e => e.CreatedBy)
           .WithMany()
           .HasForeignKey(e => e.CreatedById)
           .OnDelete(DeleteBehavior.Restrict);
   }
   ```

5. **Create Migration**:
   ```
   dotnet ef migrations add AddMyEntity -p OpenAutomate.Infrastructure -s OpenAutomate.API
   ```

6. **Create Interface** in `OpenAutomate.Core/Domain/Interfaces/IServices`:
   ```csharp
   public interface IMyEntityService
   {
       Task<MyEntityResponse> CreateAsync(CreateMyEntityRequest request);
       Task<List<MyEntityResponse>> GetAllAsync();
       Task<MyEntityResponse> GetByIdAsync(Guid id);
       Task<MyEntityResponse> UpdateAsync(Guid id, UpdateMyEntityRequest request);
       Task<bool> DeleteAsync(Guid id);
   }
   ```

7. **Implement Service** in `OpenAutomate.Infrastructure/Services`:
   ```csharp
   public class MyEntityService : IMyEntityService
   {
       private readonly IUnitOfWork _unitOfWork;
       private readonly ITenantContext _tenantContext;
       
       public MyEntityService(IUnitOfWork unitOfWork, ITenantContext tenantContext)
       {
           _unitOfWork = unitOfWork;
           _tenantContext = tenantContext;
       }
       
       // Implement methods from interface
   }
   ```

8. **Register Service** in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<IMyEntityService, MyEntityService>();
   ```

### Creating API Endpoints

Follow these steps to add a new controller:

1. **Create Controller** in `OpenAutomate.API/Controllers`:
   ```csharp
   [Route("{tenant}/api/[controller]")]
   [ApiController]
   [Authorize]
   public class MyEntitiesController : ControllerBase
   {
       private readonly IMyEntityService _myEntityService;
       
       public MyEntitiesController(IMyEntityService myEntityService)
       {
           _myEntityService = myEntityService;
       }
       
       [HttpGet]
       public async Task<IActionResult> GetAll()
       {
           try
           {
               var entities = await _myEntityService.GetAllAsync();
               return Ok(entities);
           }
           catch (Exception ex)
           {
               return StatusCode(500, new { message = "An error occurred while processing your request." });
           }
       }
       
       [HttpGet("{id}")]
       public async Task<IActionResult> GetById(Guid id)
       {
           try
           {
               var entity = await _myEntityService.GetByIdAsync(id);
               if (entity == null)
                   return NotFound();
               
               return Ok(entity);
           }
           catch (Exception ex)
           {
               return StatusCode(500, new { message = "An error occurred while processing your request." });
           }
       }
       
       [HttpPost]
       public async Task<IActionResult> Create([FromBody] CreateMyEntityRequest request)
       {
           try
           {
               var entity = await _myEntityService.CreateAsync(request);
               return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
           }
           catch (Exception ex)
           {
               return StatusCode(500, new { message = "An error occurred while processing your request." });
           }
       }
       
       [HttpPut("{id}")]
       public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMyEntityRequest request)
       {
           try
           {
               var entity = await _myEntityService.UpdateAsync(id, request);
               if (entity == null)
                   return NotFound();
               
               return Ok(entity);
           }
           catch (Exception ex)
           {
               return StatusCode(500, new { message = "An error occurred while processing your request." });
           }
       }
       
       [HttpDelete("{id}")]
       public async Task<IActionResult> Delete(Guid id)
       {
           try
           {
               var result = await _myEntityService.DeleteAsync(id);
               if (!result)
                   return NotFound();
               
               return NoContent();
           }
           catch (Exception ex)
           {
               return StatusCode(500, new { message = "An error occurred while processing your request." });
           }
       }
   }
   ```

### Authentication & Authorization

The OpenAutomate backend uses JWT with refresh tokens for authentication:

1. **Securing Endpoints**:
   ```csharp
   // Require authentication
   [Authorize]
   public class MyController : ControllerBase
   {
       // Controller code
   }
   
   // Require specific role
   [Authorize(Roles = "Admin")]
   public IActionResult AdminAction()
   {
       // Action code
   }
   ```

2. **Getting Current User**:
   ```csharp
   public class MyService
   {
       private readonly IHttpContextAccessor _httpContextAccessor;
       
       public MyService(IHttpContextAccessor httpContextAccessor)
       {
           _httpContextAccessor = httpContextAccessor;
       }
       
       public void DoSomething()
       {
           var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
           // Use user ID as needed
       }
   }
   ```

3. **Refresh Token Considerations**:
   - Refresh tokens are stored in HTTP-only cookies
   - Backend checks and validates tokens automatically
   - Use the EF Core query optimization technique described in the documentation

### Error Handling

Follow these guidelines for error handling:

1. **Service Layer**:
   ```csharp
   public async Task<MyEntityResponse> GetByIdAsync(Guid id)
   {
       try
       {
           var entity = await _unitOfWork.MyEntities.GetByIdAsync(id);
           if (entity == null)
               throw new NotFoundException($"Entity with ID {id} not found");
           
           return _mapper.Map<MyEntityResponse>(entity);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Error getting entity by ID {Id}", id);
           throw;
       }
   }
   ```

2. **Controller Layer**:
   ```csharp
   [HttpGet("{id}")]
   public async Task<IActionResult> GetById(Guid id)
   {
       try
       {
           var entity = await _myEntityService.GetByIdAsync(id);
           return Ok(entity);
       }
       catch (NotFoundException ex)
       {
           return NotFound(new { message = ex.Message });
       }
       catch (Exception ex)
       {
           return StatusCode(500, new { message = "An error occurred while processing your request." });
       }
   }
   ```

### EF Core Best Practices

When working with Entity Framework Core:

1. **Avoid Using Computed Properties in LINQ Queries**:
   ```csharp
   // Incorrect - will cause translation error
   var token = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(
       t => t.Token == refreshToken && !t.IsRevoked && !t.IsExpired);
   
   // Correct - query database properties, then check computed properties
   var token = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(
       t => t.Token == refreshToken);
   
   if (token == null || token.IsRevoked || token.IsExpired)
   {
       // Handle invalid token
   }
   ```

2. **Use Include for Loading Related Entities**:
   ```csharp
   var entity = await _unitOfWork.MyEntities.GetFirstOrDefaultAsync(
       e => e.Id == id,
       includes: e => e.CreatedBy);
   ```

3. **Use Repository Methods for Standard Operations**:
   ```csharp
   // Add entity
   await _unitOfWork.MyEntities.AddAsync(entity);
   
   // Update entity
   await _unitOfWork.MyEntities.UpdateAsync(entity);
   
   // Delete entity
   await _unitOfWork.MyEntities.DeleteAsync(entity);
   
   // Save changes
   await _unitOfWork.CompleteAsync();
   ```

## Frontend Development

### Frontend Project Structure

The frontend project follows the Next.js App Router structure:

1. **app**: Page routes and layouts
   - `layout.tsx`: Root layout with providers
   - `(auth)`: Authentication routes (login, register)
   - `(tenant)`: Tenant-specific routes

2. **components**: Reusable components
   - `auth`: Authentication components
   - `layouts`: Layout components including app-layout
   - `ui`: UI components like buttons, forms, etc.
   - `providers`: Context providers

3. **lib**: Utilities and services
   - `api`: API client and utilities
   - `hooks`: Custom React hooks
   - `config.ts`: Application configuration

4. **types**: TypeScript type definitions

### Authentication & Tenant Context

The frontend uses provider components for authentication and tenant context:

1. **Using Authentication**:
   ```tsx
   'use client';
   
   import { useAuth } from '@/lib/hooks/use-auth';
   
   export default function MyComponent() {
     const { user, isAuthenticated, isLoading, logout } = useAuth();
     
     if (isLoading) {
       return <div>Loading...</div>;
     }
     
     if (!isAuthenticated) {
       return <div>You need to login</div>;
     }
     
     return (
       <div>
         <h1>Welcome, {user?.name}</h1>
         <button onClick={logout}>Logout</button>
       </div>
     );
   }
   ```

2. **Using Tenant Context**:
   ```tsx
   'use client';
   
   import { useTenant } from '@/components/providers/TenantProvider';
   
   export default function MyComponent() {
     const { currentTenant, isLoading } = useTenant();
     
     if (isLoading) {
       return <div>Loading...</div>;
     }
     
     return (
       <div>
         <h1>Current Tenant: {currentTenant}</h1>
       </div>
     );
   }
   ```

3. **Server-Side Rendering Considerations**:
   - Mark components using hooks with `'use client'`
   - Use the `mounted` state pattern to prevent hydration mismatches
   ```tsx
   'use client';
   
   import { useState, useEffect } from 'react';
   
   export default function MyComponent() {
     const [mounted, setMounted] = useState(false);
     
     useEffect(() => {
       setMounted(true);
     }, []);
     
     // Use the same loading state for both server and client
     if (!mounted) {
       return <div>Loading...</div>;
     }
     
     // Client-side only content
     return <div>Client content</div>;
   }
   ```

### Creating New Pages

To create a new page in the Next.js app:

1. **Create a New Page File** in the appropriate directory:
   ```tsx
   // app/(tenant)/[tenant]/my-feature/page.tsx
   
   import { Metadata } from 'next';
   
   export const metadata: Metadata = {
     title: 'My Feature - OpenAutomate',
     description: 'Description of my feature',
   };
   
   export default function MyFeaturePage() {
     return (
       <div className="container mx-auto p-4">
         <h1 className="text-2xl font-bold mb-4">My Feature</h1>
         <div className="bg-white shadow rounded-lg p-6">
           {/* Page content */}
         </div>
       </div>
     );
   }
   ```

2. **For Client Components** (if needed):
   ```tsx
   'use client';
   
   import { useState } from 'react';
   
   export default function MyClientPage() {
     const [data, setData] = useState([]);
     
     return (
       <div>
         {/* Client component content */}
       </div>
     );
   }
   ```

3. **For Pages with Data Fetching**:
   ```tsx
   // Server Component approach
   import { apiClient } from '@/lib/api/server-client';
   
   export default async function MyDataPage({ params }) {
     const { tenant } = params;
     const data = await apiClient.get(`/${tenant}/api/my-entities`);
     
     return (
       <div>
         {/* Render data */}
       </div>
     );
   }
   
   // Client Component approach
   'use client';
   
   import { useEffect, useState } from 'react';
   import { apiClient } from '@/lib/api/client';
   
   export default function MyDataPage({ params }) {
     const { tenant } = params;
     const [data, setData] = useState([]);
     const [isLoading, setIsLoading] = useState(true);
     
     useEffect(() => {
       const fetchData = async () => {
         try {
           const response = await apiClient.get(`/my-entities`, { tenant });
           setData(response.data);
         } catch (error) {
           console.error('Error fetching data:', error);
         } finally {
           setIsLoading(false);
         }
       };
       
       fetchData();
     }, [tenant]);
     
     if (isLoading) {
       return <div>Loading...</div>;
     }
     
     return (
       <div>
         {/* Render data */}
       </div>
     );
   }
   ```

### Creating New Components

To create a reusable component:

1. **Create a New Component File**:
   ```tsx
   // components/ui/MyComponent.tsx
   
   interface MyComponentProps {
     title: string;
     children: React.ReactNode;
   }
   
   export function MyComponent({ title, children }: MyComponentProps) {
     return (
       <div className="bg-white shadow rounded-lg p-4">
         <h2 className="text-xl font-semibold mb-2">{title}</h2>
         <div>{children}</div>
       </div>
     );
   }
   ```

2. **For Client Components**:
   ```tsx
   'use client';
   
   import { useState } from 'react';
   
   interface MyClientComponentProps {
     initialValue: number;
     onChange?: (value: number) => void;
   }
   
   export function MyClientComponent({ initialValue, onChange }: MyClientComponentProps) {
     const [value, setValue] = useState(initialValue);
     
     const handleClick = () => {
       const newValue = value + 1;
       setValue(newValue);
       onChange?.(newValue);
     };
     
     return (
       <div>
         <p>Value: {value}</p>
         <button 
           onClick={handleClick}
           className="px-4 py-2 bg-blue-500 text-white rounded"
         >
           Increment
         </button>
       </div>
     );
   }
   ```

### State Management

The application uses React Context for global state management:

1. **Creating a Context Provider**:
   ```tsx
   'use client';
   
   import { createContext, useContext, useState, ReactNode } from 'react';
   
   // Define context type
   interface MyContextType {
     value: string;
     setValue: (value: string) => void;
   }
   
   // Create context with default value
   const MyContext = createContext<MyContextType>({
     value: '',
     setValue: () => {},
   });
   
   // Create provider component
   export function MyProvider({ children }: { children: ReactNode }) {
     const [value, setValue] = useState('');
     
     return (
       <MyContext.Provider value={{ value, setValue }}>
         {children}
       </MyContext.Provider>
     );
   }
   
   // Create hook for using the context
   export function useMyContext() {
     return useContext(MyContext);
   }
   ```

2. **Using the Context**:
   ```tsx
   'use client';
   
   import { useMyContext } from '@/components/providers/MyProvider';
   
   export function MyComponent() {
     const { value, setValue } = useMyContext();
     
     return (
       <div>
         <p>Value: {value}</p>
         <input
           type="text"
           value={value}
           onChange={(e) => setValue(e.target.value)}
           className="border p-2 rounded"
         />
       </div>
     );
   }
   ```

3. **Adding Provider to Layout**:
   ```tsx
   // app/layout.tsx
   import { MyProvider } from '@/components/providers/MyProvider';
   
   export default function RootLayout({ children }) {
     return (
       <html lang="en">
         <body>
           <MyProvider>
             {children}
           </MyProvider>
         </body>
       </html>
     );
   }
   ```

### API Integration

The frontend uses a custom API client for backend integration:

1. **Using the API Client**:
   ```tsx
   'use client';
   
   import { useState, useEffect } from 'react';
   import { apiClient } from '@/lib/api/client';
   import { useTenant } from '@/components/providers/TenantProvider';
   
   export function MyDataComponent() {
     const { currentTenant } = useTenant();
     const [data, setData] = useState([]);
     const [isLoading, setIsLoading] = useState(true);
     
     useEffect(() => {
       const fetchData = async () => {
         if (!currentTenant) return;
         
         try {
           const response = await apiClient.get('/my-entities', { tenant: currentTenant });
           setData(response.data);
         } catch (error) {
           console.error('Error fetching data:', error);
         } finally {
           setIsLoading(false);
         }
       };
       
       fetchData();
     }, [currentTenant]);
     
     // Component rendering
   }
   ```

2. **Creating, Updating, and Deleting Data**:
   ```tsx
   // Create
   const handleCreate = async (data) => {
     try {
       const response = await apiClient.post('/my-entities', data, { tenant: currentTenant });
       // Handle successful creation
     } catch (error) {
       // Handle error
     }
   };
   
   // Update
   const handleUpdate = async (id, data) => {
     try {
       const response = await apiClient.put(`/my-entities/${id}`, data, { tenant: currentTenant });
       // Handle successful update
     } catch (error) {
       // Handle error
     }
   };
   
   // Delete
   const handleDelete = async (id) => {
     try {
       await apiClient.delete(`/my-entities/${id}`, { tenant: currentTenant });
       // Handle successful deletion
     } catch (error) {
       // Handle error
     }
   };
   ```

### Server vs. Client Components

Understanding when to use Server vs. Client Components in Next.js:

1. **Use Server Components When**:
   - You need to fetch data
   - You don't need interactivity or browser APIs
   - You want to reduce JavaScript bundle size
   
   ```tsx
   // app/(tenant)/[tenant]/my-feature/page.tsx
   import { MyServerComponent } from '@/components/MyServerComponent';
   
   export default async function Page({ params }) {
     const { tenant } = params;
     const data = await fetchData(tenant);
     
     return <MyServerComponent data={data} />;
   }
   ```

2. **Use Client Components When**:
   - You need interactivity (useState, event handlers)
   - You need browser APIs (localStorage, window)
   - You need lifecycle effects (useEffect)
   
   ```tsx
   'use client';
   
   import { useState } from 'react';
   
   export function MyClientComponent() {
     const [isOpen, setIsOpen] = useState(false);
     
     return (
       <div>
         <button onClick={() => setIsOpen(!isOpen)}>Toggle</button>
         {isOpen && <div>Content</div>}
       </div>
     );
   }
   ```

3. **Component Composition**:
   - Server components can import and render client components
   - Client components cannot use async/await directly
   - Pass data from server to client components as props
   
   ```tsx
   // Server component
   import { MyClientComponent } from '@/components/MyClientComponent';
   
   export default async function Page() {
     const data = await fetchData();
     
     return <MyClientComponent initialData={data} />;
   }
   
   // Client component
   'use client';
   
   export function MyClientComponent({ initialData }) {
     const [data, setData] = useState(initialData);
     
     // Client-side logic
     
     return <div>{/* Render data */}</div>;
   }
   ```

## Development Workflow

### Setting Up the Development Environment

1. **Prerequisites**:
   - .NET SDK 8.0
   - Node.js 18+
   - SQL Server or PostgreSQL
   - Visual Studio 2022 or VS Code with C# extension

2. **Backend Setup**:
   ```bash
   # Clone the repository
   git clone https://github.com/yourusername/openautomate.git
   
   # Navigate to backend directory
   cd OpenAutomate.Backend
   
   # Restore packages
   dotnet restore
   
   # Update database
   dotnet ef database update -p OpenAutomate.Infrastructure -s OpenAutomate.API
   
   # Run the API
   dotnet run --project OpenAutomate.API
   ```

3. **Frontend Setup**:
   ```bash
   # Navigate to frontend directory
   cd OpenAutomate.Frontend/openautomate-frontend
   
   # Install dependencies
   npm install
   
   # Run development server
   npm run dev
   ```

### Running the Application

1. **Backend**:
   - Run with Visual Studio: Open the solution and press F5
   - Run with CLI: `dotnet run --project OpenAutomate.API`
   - API will be available at `https://localhost:7240`

2. **Frontend**:
   - Run with CLI: `npm run dev`
   - Frontend will be available at `http://localhost:3000`

### Testing

1. **Backend Testing**:
   - Unit tests: `dotnet test`
   - API testing: Use Swagger UI or Postman

2. **Frontend Testing**:
   - Component tests: `npm test`
   - End-to-end tests: `npm run cypress`

### Pull Request Process

1. Create a new branch for your feature
2. Implement the feature with appropriate tests
3. Run all tests to ensure they pass
4. Create a pull request with a detailed description
5. Ensure the CI pipeline passes
6. Request a code review
7. Address any review comments
8. Merge the pull request once approved 
