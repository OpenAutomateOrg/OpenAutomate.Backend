# OpenAutomate.Backend

Backend API service for OpenAutomate - an open-source automation orchestration platform inspired by UiPath Orchestrator.

## Overview

OpenAutomate.Backend provides the core REST API and business logic for managing automation processes across machines. It enables:

- Machine management and monitoring
- Python script/bot management and version control
- Job scheduling and execution
- User management with role-based access control
- Secret management for secure credential storage

## Architecture

This repository follows Clean Architecture principles with clear separation of concerns:

```
OpenAutomate.Backend/
├── OpenAutomate.Common/             # Shared contracts and models (submodule)
├── src/
│   ├── OpenAutomate.API/            # API controllers, endpoints, configuration
│   ├── OpenAutomate.Core/           # Business logic and application services
│   ├── OpenAutomate.Domain/         # Domain entities and business rules
│   └── OpenAutomate.Infrastructure/ # Data access, external services integration
└── tests/                           # Unit and integration tests
```

### Key Architectural Components

- **API Layer**: ASP.NET Core controllers, SignalR hubs, authentication
- **Core Layer**: Business logic, use cases, service interfaces
- **Domain Layer**: Business entities and rules
- **Infrastructure Layer**: Database access, external services, framework implementations

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (or configured alternative)
- Visual Studio 2022 or VS Code
- Git (with Git LFS for larger files)

### Setup Instructions

1. **Clone the repository with submodules:**

```bash
git clone --recursive https://github.com/OpenAutomateOrg/OpenAutomate.Backend.git
cd OpenAutomate.Backend

# If you forgot --recursive:
git submodule init
git submodule update
```

2. **Restore dependencies and build:**

```bash
dotnet restore
dotnet build
```

3. **Configure your database:**

Edit `appsettings.Development.json` in the OpenAutomate.API project:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OpenAutomateDb;Trusted_Connection=True;"
  },
  "Storage": {
    "BasePath": "C:\\OpenAutomate\\SourceCodes"  // Adjust as needed
  }
}
```

4. **Apply database migrations:**

```bash
cd src/OpenAutomate.API
dotnet ef database update
```

5. **Run the API:**

```bash
dotnet run --project src/OpenAutomate.API
```

The API will be available at `https://localhost:7188/swagger` by default.

## Key Features and Endpoints

- **Machine Management**: `/api/machines` - Register, connect, and monitor machines
- **Source Code Management**: `/api/sourcecode` - Store and version Python automation scripts
- **Job Management**: `/api/jobs` - Create, schedule, and monitor automation jobs
- **User Management**: `/api/users` - Manage users and permissions
- **Secret Management**: `/api/secrets` - Store and retrieve credentials securely

## Development Workflow

### Branch Strategy

- `main`: Production-ready code
- `develop`: Integration branch for features
- `feature/*`: Feature branches

### Database Migrations

When modifying the data model:

```bash
dotnet ef migrations add [MigrationName] -p src/OpenAutomate.Infrastructure -s src/OpenAutomate.API
dotnet ef database update -p src/OpenAutomate.Infrastructure -s src/OpenAutomate.API
```

### Running Tests

```bash
dotnet test
```

### Updating the Common Submodule

```bash
cd OpenAutomate.Common
git pull origin main
cd ..
git add OpenAutomate.Common
git commit -m "Update Common submodule"
```

## API Documentation

- **Swagger UI**: Available at `/swagger` when running the application
- **API Versioning**: All endpoints are versioned (`api/v1/...`)

## Deployment

The API is designed to be deployed to an AWS EC2 instance:

- Configure the EC2 instance with appropriate security groups
- Set up file storage in a dedicated directory
- Configure nginx as a reverse proxy with SSL
- Set up CI/CD pipelines for automated deployment

## Dependencies

- **ASP.NET Core 8**: Web API framework
- **Entity Framework Core**: Data access
- **AutoMapper**: Object mapping
- **FluentValidation**: Request validation
- **SignalR**: Real-time communication
- **Serilog**: Structured logging

## Contributing

1. Update the Common submodule if necessary
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass (`dotnet test`)
6. Commit your changes (`git commit -m 'Add some feature'`)
7. Push to the branch (`git push origin feature/your-feature`)
8. Create a Pull Request

## License

[Your license information here]

---

For questions or support, contact the architecture team through the project Slack channel.
