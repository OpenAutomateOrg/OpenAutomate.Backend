# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Building and Testing
- **Build solution**: `dotnet build`
- **Run API**: `dotnet run --project OpenAutomate.API`
- **Run with hot reload**: `dotnet watch --project OpenAutomate.API`
- **Run all tests**: `dotnet test`
- **Run specific test project**: `dotnet test OpenAutomate.API.Tests`
- **Run tests with coverage**: `dotnet test --collect:"XPlat Code Coverage"`

### Database Operations
- **Apply migrations**: `dotnet ef database update --project OpenAutomate.Infrastructure --startup-project OpenAutomate.API`
- **Add migration**: `dotnet ef migrations add MigrationName --project OpenAutomate.Infrastructure --startup-project OpenAutomate.API`
- **Remove migration**: `dotnet ef migrations remove --project OpenAutomate.Infrastructure --startup-project OpenAutomate.API`

### Redis Management
- **Start Redis**: `docker-compose -f docker-compose.redis.yml up -d`
- **Stop Redis**: `docker-compose -f docker-compose.redis.yml down`
- **Redis CLI**: `docker exec -it openautomae-redis-dev redis-cli`

## Architecture

### Project Structure (Clean Architecture)
- **OpenAutomate.API** - Controllers, middleware, SignalR hubs
- **OpenAutomate.Core** - Domain entities, interfaces, DTOs, business logic
- **OpenAutomate.Infrastructure** - Data access, external services, implementations

### Multi-Tenancy Implementation
- **Tenant Resolution**: Path-based using URL slug pattern `/{tenant-slug}/api/resource`
- **Data Isolation**: Global query filtering through `TenantEntity` base class in OpenAutomate.Core/Domain/Base/TenantEntity.cs:14
- **Tenant Context**: Managed by `ITenantContext` service in OpenAutomate.Infrastructure/Services/TenantContext.cs:15
- **Middleware**: `TenantResolutionMiddleware` resolves tenant from URL path

### Key Services and Patterns
- **Caching Strategy**: Redis-based caching with decorator pattern (e.g., `AuthorizationManagerCachingDecorator`, `TenantContextCachingDecorator`)
- **Authentication**: JWT with refresh token support, cookie fallback
- **Real-time Communication**: SignalR with Redis backplane for bot agent communication
- **Task Scheduling**: Quartz.NET with SQL Server persistence
- **File Storage**: AWS S3 for automation packages and execution logs
- **Email Service**: AWS SES integration

### Database Design
- **Multi-tenant**: Shared database with tenant filtering via `OrganizationUnitId`
- **ORM**: Entity Framework Core with automatic tenant query filtering
- **Migrations**: Located in OpenAutomate.Infrastructure/Migrations/

### Configuration Management
- **Settings Classes**: Located in OpenAutomate.Core/Configurations/
- **Environment Support**: Development, Staging, Production configurations
- **Key Services**: Database, Redis, JWT, CORS, AWS, Email settings

## Development Workflow

### Adding New Features
1. Follow clean architecture: Core → Infrastructure → API
2. Add domain entities in `OpenAutomate.Core/Domain/Entities/`
3. Define service interfaces in `OpenAutomate.Core/IServices/`
4. Implement services in `OpenAutomate.Infrastructure/Services/`
5. Add controllers in `OpenAutomate.API/Controllers/`
6. Update configurations in `OpenAutomate.Core/Configurations/` if needed

### Multi-tenant Considerations
- Inherit from `TenantEntity` for tenant-specific entities
- Use `ITenantContext.CurrentTenantId` for tenant-aware operations
- Test with different tenant contexts
- Ensure caching keys include tenant information

### Testing Strategy
- Unit tests for services and domain logic
- Integration tests for controllers and repositories
- Separate test projects mirror main project structure
- Use in-memory database for testing when appropriate

### API Development
- RESTful controllers inherit from `CustomControllerBase`
- OData endpoints support `$filter`, `$orderby`, `$select`, `$expand`, `$count`
- JWT authentication with permission-based authorization
- Swagger documentation with security definitions

### SignalR Hub Development
- BotAgentHub handles real-time communication with distributed agents
- Supports both JWT and machine key authentication
- Redis backplane for horizontal scaling
- Group management for tenant isolation

## Key Technologies
- ASP.NET Core 8, Entity Framework Core, SQL Server
- Redis (caching, SignalR backplane), Quartz.NET (scheduling)
- JWT authentication, SignalR, OData, Swagger/OpenAPI
- AWS S3 (storage), AWS SES (email), Serilog (logging)