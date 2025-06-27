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

- **Framework**: ASP.NET Core 8
- **ORM**: Entity Framework Core
- **Database**: SQL Server
- **Cache & Message Broker**: Redis
- **Real-time Communication**: SignalR with Redis backplane
- **Authentication**: JWT with refresh token
- **Task Scheduling**: Quartz.NET
- **Cloud Storage**: AWS S3 (for packages and logs)
- **Email Service**: AWS SES
- **Documentation**: Swagger / OpenAPI
- **Logging**: Serilog

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (Local or Remote)
- Redis (for caching and SignalR backplane)
- Visual Studio 2022 or VS Code
- Docker (optional, for Redis)

### Development Setup

#### 1. Clone the repository

```bash
git clone https://github.com/OpenAutomateOrg/OpenAutomate.Backend.git
cd OpenAutomate.Backend
```

#### 2. Start Redis (Using Docker - Recommended)

We provide a Docker Compose file for easy Redis setup:

```bash
# Start Redis and Redis Insight
docker-compose -f docker-compose.redis.yml up -d

# Verify Redis is running
docker ps
```

This will start:
- **Redis**: Available at `localhost:6379`
- **Redis Insight**: Web UI available at `http://localhost:8001`

#### 3. Configure Application Settings

Create or update `OpenAutomate.API/appsettings.Development.json`:

```json
{
  "AppSettings": {
    "FrontendUrl": "http://localhost:3001",
    "Jwt": {
      "Secret": "your-super-secure-jwt-secret-key-that-is-at-least-32-characters-long",
      "Issuer": "OpenAutomate",
      "Audience": "OpenAutomate-Users",
      "AccessTokenExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7
    },
    "Database": {
      "DefaultConnection": "Server=localhost;Database=OpenAutomate;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
    },
    "Cors": {
      "AllowedOrigins": ["http://localhost:3000", "http://localhost:3001", "https://localhost:3000", "https://localhost:3001"]
    },
    "Redis": {
      "ConnectionString": "localhost:6379",
      "InstanceName": "OpenAutomate",
      "Database": 0,
      "AbortOnConnectFail": false
    },
    "EmailSettings": {
      "SmtpServer": "email-smtp.us-east-1.amazonaws.com",
      "SmtpPort": 587,
      "Username": "your-aws-ses-username",
      "Password": "your-aws-ses-password",
      "FromEmail": "noreply@openautomte.com",
      "FromName": "OpenAutomate"
    }
  },
  "AWS": {
    "AccessKey": "your-aws-access-key",
    "SecretKey": "your-aws-secret-key",
    "Region": "us-east-1",
    "S3": {
      "BucketName": "your-s3-bucket-name",
      "PackagesFolder": "packages",
      "LogsFolder": "logs"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.SignalR": "Debug",
      "Microsoft.AspNetCore.Http.Connections": "Debug"
    }
  }
}
```

#### 4. Apply Database Migrations

```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Apply migrations
dotnet ef database update --project OpenAutomate.Infrastructure --startup-project OpenAutomate.API
```

#### 5. Run the Application

```bash
# From the solution root
dotnet run --project OpenAutomate.API

# Or using hot reload for development
dotnet watch --project OpenAutomate.API
```

#### 6. Access the Application

- **API**: `https://localhost:7043`
- **Swagger UI**: `https://localhost:7043/swagger`
- **Health Check**: `https://localhost:7043/health`
- **Redis Insight**: `http://localhost:8001` (if using Docker)

## Key Features

- **Multi-tenancy** - Path-based tenant resolution with global query filtering
- **Authentication** - JWT with refresh token implementation
- **Real-time updates** - SignalR for real-time communication with Redis backplane
- **Bot agent management** - Register and monitor automation agents
- **Package management** - Create and distribute automation packages via S3
- **Scheduling** - Quartz.NET for robust task scheduling
- **Execution tracking** - Monitor execution status and results with S3 log storage
- **Email notifications** - AWS SES integration for email communications
- **Caching** - Redis for distributed caching and session management

## Development Workflow

### Adding New Features

1. **Create a new branch**
   ```bash
   git checkout -b feature/OA-123-feature-name
   ```

2. **Follow the clean architecture pattern**:
   - Add domain entities in `OpenAutomate.Core/Domain/Entities/`
   - Define interfaces in `OpenAutomate.Core/IServices/`
   - Implement services in `OpenAutomate.Infrastructure/Services/`
   - Add controllers in `OpenAutomate.API/Controllers/`

3. **Write tests** for new functionality in the corresponding test projects

4. **Update configurations** if needed in `OpenAutomate.Core/Configurations/`

### Database Changes

```bash
# Add a new migration
dotnet ef migrations add MigrationName --project OpenAutomate.Infrastructure --startup-project OpenAutomate.API

# Remove last migration (if needed)
dotnet ef migrations remove --project OpenAutomate.Infrastructure --startup-project OpenAutomate.API

# Apply migrations
dotnet ef database update --project OpenAutomate.Infrastructure --startup-project OpenAutomate.API
```

### Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test OpenAutomate.API.Tests
```

### Redis Management

```bash
# Start Redis services
docker-compose -f docker-compose.redis.yml up -d

# Stop Redis services
docker-compose -f docker-compose.redis.yml down

# View Redis logs
docker-compose -f docker-compose.redis.yml logs -f redis

# Connect to Redis CLI
docker exec -it openautomae-redis-dev redis-cli
```

## Configuration

### Environment Variables

The application supports the following environment variables for deployment:

- `ASPNETCORE_ENVIRONMENT` - Environment name (Development, Staging, Production)
- `FrontendUrl` - Frontend application URL
- `ConnectionStrings__DefaultConnection` - Database connection string
- `AppSettings__Redis__ConnectionString` - Redis connection string
- `AWS__AccessKey` - AWS access key for S3 and SES
- `AWS__SecretKey` - AWS secret key
- `AppSettings__Jwt__Secret` - JWT signing secret

### Development vs Production

- **Development**: Uses local Redis, SQL Server, and file-based logging
- **Production**: Uses managed Redis, SQL Server, S3 for storage, and structured logging

## Debugging

### SignalR Connections

Enable detailed SignalR logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.SignalR": "Debug",
      "Microsoft.AspNetCore.Http.Connections": "Debug"
    }
  }
}
```

### Redis Connection Issues

1. Verify Redis is running: `docker ps`
2. Test Redis connection: `docker exec -it openautomae-redis-dev redis-cli ping`
3. Check Redis logs: `docker logs openautomae-redis-dev`

## API Documentation

- **Swagger UI**: Available at `/swagger` endpoint when running in development
- **OData endpoints**: Support for `$filter`, `$orderby`, `$select`, `$expand`, and `$count`
- **Health checks**: Available at `/health` endpoint

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/OA-123-amazing-feature`)
3. Write tests for your changes
4. Ensure all tests pass and coverage meets requirements
5. Commit your changes (`git commit -m 'Add some amazing feature'`)
6. Push to the branch (`git push origin feature/OA-123-amazing-feature`)
7. Open a Pull Request

## Code Coverage

This project maintains 80% code coverage requirement.

### Running Tests with Code Coverage

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report (requires reportgenerator tool)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:"Html"
```

### SonarQube Integration

```bash
# Install SonarScanner
dotnet tool install --global dotnet-sonarscanner

# Run SonarQube analysis
dotnet sonarscanner begin /k:"OpenAutomate.Backend" /d:sonar.host.url="your-sonarqube-url"
dotnet build
dotnet test --collect:"XPlat Code Coverage"
dotnet sonarscanner end
```

## Troubleshooting

### Common Issues

1. **Redis connection errors**: Ensure Redis is running via Docker Compose
2. **Database connection errors**: Verify SQL Server is running and connection string is correct
3. **JWT errors**: Check that JWT secret is at least 32 characters long
4. **Migration errors**: Ensure you're running migrations from the solution root directory

### Getting Help

- Check the logs in the console output
- Review Swagger documentation for API usage
- Use Redis Insight to monitor Redis operations
- Check the `/health` endpoint for system status

## License

This project is licensed under the MIT License.
