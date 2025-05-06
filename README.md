# OpenAutomate Backend

OpenAutomate is an automation platform that allows organizations to create, manage, and execute automation tasks across multiple machines. This repository contains the backend components of the OpenAutomate platform.

## Architecture Overview

OpenAutomate follows a centralized orchestration platform with distributed execution architecture. The system consists of:

- **Web-based control panel** (frontend)
- **Backend API services** (this repository)
- **Worker services** for job processing
- **Distributed bot agents** that execute automation tasks on target machines

The platform uses a client-server model where the central components (server) are hosted by OpenAutomate, while the execution agents (clients) are deployed and hosted by customers on their own infrastructure. Customers control how many agents they deploy based on their needs.

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

## Code Coverage

This project is configured to meet SonarQube's 80% code coverage requirement.

### Running Tests with Code Coverage Locally

1. **Using the Command Line**:
   ```bash
   # Navigate to the backend project directory
   cd OpenAutomate.Backend
   
   # Run tests with code coverage
   dotnet test --collect:"XPlat Code Coverage"
   ```

2. **Using Visual Studio**:
   - Right-click on the test project in Solution Explorer
   - Select "Run Tests with Coverage"

3. **Using VS Code**:
   - Open the Command Palette (Ctrl+Shift+P)
   - Run the "Tasks: Run Task" command
   - Select "test with coverage"

### Coverage Reports

After running tests with coverage, reports will be generated in the `TestResults` directory of each test project. These reports are in Cobertura XML format and can be viewed with coverage visualization tools.

### SonarQube Integration

To run a complete SonarQube analysis with code coverage:

1. Install the SonarScanner for .NET globally:
   ```bash
   dotnet tool install --global dotnet-sonarscanner
   ```

2. Run the analysis script:
   ```bash
   # From PowerShell
   .\sonarqube-analysis.ps1
   ```
   
   Or use the VS Code task "run sonarqube analysis".

3. View the results in your SonarQube dashboard.

### Coverage Requirements

- Aim for 80% code coverage for all new code
- Focus on testing business logic and services
- Exclude boilerplate and generated code from coverage calculations

### Continuous Integration

The GitHub Actions workflow automatically runs tests with coverage and reports the results to SonarQube on each pull request and push to main branches.
