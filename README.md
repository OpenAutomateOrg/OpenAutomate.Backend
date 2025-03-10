# OpenAutomate.Backend

Backend API server for OpenAutomate - an open-source automation orchestration platform.

## Overview

OpenAutomate.Backend provides the core API services for managing automation processes, machine connections, scheduling, and user management. This repository is built with ASP.NET Core using Clean Architecture principles.

## Project Structure

```
OpenAutomate.Backend/
├── OpenAutomate.Common/             # Submodule containing shared contracts and models
├── src/
│   ├── OpenAutomate.API/            # ASP.NET Core Web API project
│   ├── OpenAutomate.Core/           # Business logic and application services
│   ├── OpenAutomate.Domain/         # Domain entities and business rules
│   └── OpenAutomate.Infrastructure/ # Data access, external services integration
├── tests/
│   ├── OpenAutomate.API.Tests/
│   ├── OpenAutomate.Core.Tests/
│   └── OpenAutomate.Infrastructure.Tests/
└── OpenAutomate.Backend.sln         # Solution file
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (or configured alternative)
- Visual Studio 2022 or VS Code

### Setup Instructions

1. **Clone the repository:**

```bash
git clone https://github.com/OpenAutomateOrg/OpenAutomate.Backend.git
cd OpenAutomate.Backend

```

2. **Open the solution:**

```bash
# Using Visual Studio:
start OpenAutomate.Backend.sln

# Or using VS Code:
code .
```

3. **Configure your database connection:**

Update `appsettings.Development.json` in the OpenAutomate.API project with your database connection:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OpenAutomate;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

4. **Run database migrations:**

```bash
cd src/OpenAutomate.API
dotnet ef database update
```

5. **Run the API:**

```bash
dotnet run
```

The API will be available at `https://localhost:5001/swagger` by default.

## Key Features

The API provides endpoints for:

- **Machine Management**: Connect, monitor, and manage remote machines
- **Job Management**: Create, schedule, and monitor automation jobs
- **Source Code Management**: Store and version automation scripts
- **User/Role Management**: Manage users and RBAC
- **Scheduling**: Configure job schedules and triggers
- **Secret Management**: Securely store and retrieve credentials

## Development Workflow


### Database Migrations

When modifying the data model:

```bash
cd src/OpenAutomate.API
dotnet ef migrations add [MigrationName]
dotnet ef database update
```

### Running Tests

```bash
dotnet test
```

### Branch Strategy

- `main`: Production-ready code
- `develop`: Integration branch for ongoing development
- `feature/*`: Feature branches

## API Documentation

Swagger documentation is available when running the API at `/swagger`.

Key API areas:
- `/api/machines`: Machine management
- `/api/jobs`: Job management
- `/api/users`: User management
- `/api/schedules`: Scheduling
- `/api/secrets`: Secret management

## Dependencies

- Entity Framework Core for data access
- ASP.NET Core Identity for authentication
- SignalR for real-time communication
- Common library (via submodule) for shared contracts

## Contribution Guidelines

1. Create feature branches from `develop`
2. Follow the coding standards and architecture patterns
3. Include unit tests for all new features
4. Create pull requests to merge back to `develop`

## Deployment

CI/CD pipelines are configured to:
1. Build and test on PR
2. Deploy to staging on merge to `develop`
3. Deploy to production on merge to `main`

## Need Help?

Contact the backend team in the `#backend` channel on Slack.
