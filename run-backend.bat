@echo off
echo ========================================
echo    OpenAutomate Backend - Setup and Start
echo ========================================
echo.

REM Check if we're in the right directory
if not exist "OpenAutomate.API" (
    echo ERROR: Please run this script from the OpenAutomate.Backend directory
    echo Current directory: %CD%
    echo.
    echo Expected to find: OpenAutomate.API folder
    pause
    exit /b 1
)

REM Check if already configured
if exist "OpenAutomate.API\appsettings.Development.json" (
    echo Configuration already exists. Skipping setup...
    goto start_services
)

echo ========================================
echo    FIRST TIME SETUP
echo ========================================
echo.

echo [1/4] Starting Redis...
docker-compose -f docker-compose.redis.yml up -d
if %errorlevel% neq 0 (
    echo ERROR: Failed to start Redis. Make sure Docker Desktop is running.
    pause
    exit /b 1
)
echo ✓ Redis started successfully

echo.
echo [2/4] Creating local development configuration...

REM Create the development configuration file
(
echo {
echo   "Logging": {
echo     "LogLevel": {
echo       "Default": "Information",
echo       "Microsoft.AspNetCore": "Warning"
echo     }
echo   },
echo   "FrontendUrl": "http://localhost:3001",
echo   "AllowedHosts": "*",
echo   "AppSettings": {
echo     "Database": {
echo       "DefaultConnection": "Server=^(localdb^)\\MSSQLLocalDB;Database=OpenAutomateDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
echo     },
echo     "Redis": {
echo       "ConnectionString": "localhost:6379",
echo       "InstanceName": "OpenAutomate_Dev",
echo       "Database": 0,
echo       "AbortOnConnectFail": false,
echo       "Username": "default",
echo       "Password": ""
echo     },
echo     "Jwt": {
echo       "Secret": "YourSecretKeyHere_ThisShouldBeAtLeast32CharsLong",
echo       "Issuer": "OpenAutomate",
echo       "Audience": "OpenAutomateClients",
echo       "AccessTokenExpirationMinutes": 600000,
echo       "RefreshTokenExpirationDays": 7
echo     },
echo     "Cors": {
echo       "AllowedOrigins": [
echo         "http://localhost:3000",
echo         "http://localhost:3001",
echo         "http://localhost:5252",
echo         "https://localhost:5252"
echo       ]
echo     },
echo     "UserSeed": {
echo       "EnableSeeding": true,
echo       "Users": [
echo         {
echo           "Email": "admin@openautomate.io",
echo           "Password": "openAutomate@12345",
echo           "SystemRole": "Admin",
echo           "FirstName": "System",
echo           "LastName": "Administrator"
echo         },
echo         {
echo           "Email": "user1@openautomate.io",
echo           "Password": "openAutomate@12345",
echo           "SystemRole": "User",
echo           "FirstName": "Test",
echo           "LastName": "User1"
echo         },
echo         {
echo           "Email": "user2@openautomate.io",
echo           "Password": "openAutomate@12345",
echo           "SystemRole": "User",
echo           "FirstName": "Test",
echo           "LastName": "User2"
echo         }
echo       ]
echo     }
echo   }
echo }
) > "OpenAutomate.API\appsettings.Development.json"

echo ✓ Local development configuration created

echo.
echo [3/4] Installing dependencies...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages. Make sure .NET 8 SDK is installed.
    pause
    exit /b 1
)
echo ✓ Dependencies restored

echo.
echo [4/4] Building the application...
dotnet build
if %errorlevel% neq 0 (
    echo ERROR: Build failed. Check the error messages above.
    pause
    exit /b 1
)
echo ✓ Application built successfully

echo.
echo ========================================
echo    Setup Complete! Starting Backend...
echo ========================================
echo.

:start_services
REM Check if Redis is running
echo Checking Redis status...
docker ps | findstr "openautomae-redis-dev" >nul
if %errorlevel% neq 0 (
    echo Starting Redis...
    docker-compose -f docker-compose.redis.yml up -d
    timeout /t 3 >nul
)
echo ✓ Redis is running

echo.
echo Starting Backend API...
echo ✓ Backend will be available at: http://localhost:5252
echo ✓ API Documentation: http://localhost:5252/swagger
echo ✓ Health Check: http://localhost:5252/health
echo.
echo Press Ctrl+C to stop the backend
echo ========================================
echo.

dotnet run --project OpenAutomate.API
