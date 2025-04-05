# OpenAutomate Backend

OpenAutomate is a distributed automation platform that allows organizations to create, manage, and execute automation tasks across multiple machines. This repository contains the backend components of the OpenAutomate platform.

## Architecture Overview

OpenAutomate follows a distributed microservices architecture with centralized management. The system consists of:

- **Web-based control panel** (frontend)
- **Backend API services** (this repository)
- **Worker services** for job processing
- **Distributed bot agents** that execute automation tasks on target machines

The platform is built as a multi-tenant system where each organization represents a tenant, with data isolation implemented through tenant filtering.

### Multi-Tenant Design

OpenAutomate implements multi-tenancy using the shared database with tenant filtering approach:

- A single database instance hosts data for all tenants
- Each tenant-specific entity includes a reference to its tenant (Organization)
- Queries are automatically filtered by the current tenant
- URL format: `domain.com/{tenant-slug}/api/resource`

## Project Structure

The backend is structured following clean architecture principles:

- **OpenAutomate.API** - ASP.NET Core API controllers and middleware
- **OpenAutomate.Core** - Domain entities, interfaces, and business logic
- **OpenAutomate.Infrastructure** - Data access, external services, and implementation details

## Technologies

- **Framework**: ASP.NET Core
- **ORM**: Entity Framework Core
- **Database**: SQL Server
- **Real-time Communication**: SignalR
- **Authentication**: JWT with refresh token
- **Documentation**: Swagger / OpenAPI

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server
- Visual Studio 2022 or VS Code

### Setup Instructions

1. **Clone the repository**

   ```bash
   git clone https://github.com/yourusername/OpenAutomate.Backend.git
   cd OpenAutomate.Backend
   ```

2. **Configure the database connection**

   Update the connection string in `OpenAutomate.API/appsettings.json`:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=OpenAutomate;Trusted_Connection=True;MultipleActiveResultSets=true"
     }
   }
   ```

3. **Apply migrations**

   ```bash
   cd OpenAutomate.Backend
   dotnet ef database update --project OpenAutomate.Infrastructure --startup-project OpenAutomate.API
   ```

4. **Run the application**

   ```bash
   cd OpenAutomate.Backend
   dotnet run --project OpenAutomate.API
   ```

5. **Access the API**

   The API will be available at `https://localhost:7043/swagger` (or similar port)

## Key Features

- **Multi-tenancy** - Path-based tenant resolution with global query filtering
- **Authentication** - JWT with refresh token implementation
- **Real-time updates** - SignalR for real-time communication
- **Bot agent management** - Register and monitor automation agents
- **Package management** - Create and distribute automation packages
- **Scheduling** - Schedule automation tasks to run at specific times
- **Execution tracking** - Monitor execution status and results

## Development

### Adding Migrations

```bash
cd OpenAutomate.Backend
dotnet ef migrations add MigrationName --project OpenAutomate.Infrastructure --startup-project OpenAutomate.API
```

### Running Tests

```bash
dotnet test
```

## Documentation

- [Multi-Tenant Architecture](./Documentation/MultiTenantArchitecture.md)
- API Documentation: Available through Swagger at `/swagger` endpoint

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License
