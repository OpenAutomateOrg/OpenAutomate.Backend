# OpenAutomate Backend - Comprehensive Testing Plan

## Executive Summary

This document outlines a complete testing strategy for the OpenAutomate backend system. The plan is organized into phases based on priority and covers unit tests, integration tests, and specialized testing scenarios.

## 🎯 IMMEDIATE NEXT STEPS (Priority Order)

Based on the current state of 415 passing tests, here are the most critical tests to implement next:

### 1. 🔴 CRITICAL: ScheduleService Implementation Tests
**Why**: Scheduling is a key capstone feature using Quartz.NET
**File**: `OpenAutomate.Core.Tests/ServiceTests/ScheduleServiceTests.cs`
**Estimated**: 20 tests

### 2. 🔴 CRITICAL: CronExpressionService Tests
**Why**: Essential for schedule validation and user experience
**File**: `OpenAutomate.Core.Tests/ServiceTests/CronExpressionServiceTests.cs`
**Estimated**: 15 tests

### 3. 🔴 HIGH: AutomationPackageController Tests
**Why**: Core automation functionality for package management
**File**: `OpenAutomate.API.Tests/ControllerTests/AutomationPackageControllerTests.cs`
**Estimated**: 20 tests

### 4. 🔴 HIGH: ExecutionController Tests
**Why**: Core execution functionality for running automations
**File**: `OpenAutomate.API.Tests/ControllerTests/ExecutionControllerTests.cs`
**Estimated**: 15 tests

### 5. 🔴 HIGH: SchedulesController Tests
**Why**: API layer for schedule management
**File**: `OpenAutomate.API.Tests/ControllerTests/SchedulesControllerTests.cs`
**Estimated**: 15 tests

**Total Priority Tests**: ~85 tests to reach ~500 total tests

## Current State Analysis (Updated: 2025-01-17)

### ✅ Existing Test Coverage - EXCELLENT PROGRESS!
**Total Tests: 415 (All Passing)**

- **API Layer**: 10 controller test files ✅ **EXPANDED**
  - ✅ AdminController (9 tests)
  - ✅ AssetController (5 tests)
  - ✅ AuthenController (15 tests)
  - ✅ BotAgentController (6 tests)
  - ✅ BotAgentAssetController (9 tests) **NEW**
  - ✅ BotAgentConnectionController (21 tests) **NEW**
  - ✅ EmailVerificationController (10 tests) **NEW**
  - ✅ OrganizationUnitController (5 tests)
  - ✅ OrganizationUnitInvitationController (14 tests) **NEW**
  - ✅ UserController (8 tests)

- **Core Layer**: 30+ test files ✅ **SIGNIFICANTLY EXPANDED**
  - ✅ 19 Domain entity tests (comprehensive coverage)
  - ✅ 7 Service implementation tests (Admin, Authorization, BotAgent, Email, OrganizationUnit, Token, User)
  - ✅ 4 Service interface tests (Asset, AutomationPackage, Execution, OrganizationUnit) **NEW CATEGORY**

- **Infrastructure Layer**: 2 repository test files ✅
  - ✅ AssetRepository (19 tests)
  - ✅ UserRepository (15 tests)

### 🔄 Remaining Test Coverage Gaps
- **Missing Controllers**: 3 critical controllers without tests
- **Missing Services**: 2 core services without comprehensive tests
- **Missing Repositories**: 9+ repositories without tests
- **Missing Integration Tests**: No end-to-end testing
- **Missing Middleware Tests**: No middleware coverage
- **Missing SignalR Tests**: No real-time communication testing
- **Missing OData Tests**: 8 OData controllers without tests

---

## Phase 1: Critical Unit Tests - Core Layer

### Priority: 🔴 HIGH - Sprint 1 (UPDATED)

### 1.1 ✅ COMPLETED Service Interface Tests
**Status**: All major service interface tests are now implemented!
- ✅ IExecutionService Tests (22 tests) - **COMPLETED**
- ✅ IAutomationPackageService Tests (18 tests) - **COMPLETED**
- ✅ IAssetService Tests (22 tests) - **COMPLETED**
- ✅ IOrganizationUnitService Tests (22 tests) - **COMPLETED**

### 1.2 🔄 Missing Service Implementation Tests

#### IScheduleService Implementation Tests
**File**: `OpenAutomate.Core.Tests/ServiceTests/ScheduleServiceTests.cs`
**Priority**: 🔴 HIGH (Quartz.NET integration critical for capstone)

**Test Cases**:
```csharp
// Schedule CRUD
- CreateScheduleAsync_WithValidData_ReturnsSchedule
- CreateScheduleAsync_WithInvalidCron_ThrowsException
- GetScheduleByIdAsync_WithValidId_ReturnsSchedule
- GetScheduleByIdAsync_CrossTenant_ReturnsNull
- UpdateScheduleAsync_WithValidData_UpdatesSuccessfully
- UpdateScheduleAsync_CrossTenant_ThrowsException
- DeleteScheduleAsync_WithValidId_DeletesSuccessfully
- DeleteScheduleAsync_CrossTenant_ThrowsException

// Schedule Management
- GetSchedulesAsync_FiltersByTenant_ReturnsCorrectSchedules
- EnableScheduleAsync_WithValidId_EnablesSuccessfully
- DisableScheduleAsync_WithValidId_DisablesSuccessfully
- GetNextExecutionTimeAsync_WithValidCron_ReturnsCorrectTime
- GetNextExecutionTimeAsync_WithInvalidCron_ThrowsException

// Quartz Integration
- ScheduleJobAsync_WithValidSchedule_CreatesQuartzJob
- UnscheduleJobAsync_WithValidSchedule_RemovesQuartzJob
- UpdateQuartzJobAsync_WithModifiedSchedule_UpdatesJob
```

#### ICronExpressionService Tests
**File**: `OpenAutomate.Core.Tests/ServiceTests/CronExpressionServiceTests.cs`
**Priority**: 🔴 HIGH (Critical for scheduling validation)

**Test Cases**:
```csharp
// Cron Validation
- ValidateCronExpression_WithValidExpression_ReturnsTrue
- ValidateCronExpression_WithInvalidExpression_ReturnsFalse
- ValidateCronExpression_WithNullExpression_ReturnsFalse
- ValidateCronExpression_WithEmptyExpression_ReturnsFalse

// Next Execution Calculation
- GetNextExecutionTime_WithValidCron_ReturnsCorrectTime
- GetNextExecutionTime_WithInvalidCron_ThrowsException
- GetNextExecutionTimes_WithValidCron_ReturnsMultipleTimes
- GetNextExecutionTimes_WithCount_ReturnsCorrectCount

// Cron Description
- GetCronDescription_WithValidExpression_ReturnsDescription
- GetCronDescription_WithInvalidExpression_ThrowsException
```

### 1.2 Enhanced Service Tests

#### IAssetService Tests (Expand Existing)
**File**: `OpenAutomate.Core.Tests/ServiceTests/AssetServiceTests.cs`

**Additional Test Cases**:
```csharp
// Bot Agent Access
- GetAssetValueForBotAgentAsync_WithValidMachineKey_ReturnsValue
- GetAssetValueForBotAgentAsync_WithInvalidMachineKey_ThrowsException
- GetAssetValueForBotAgentAsync_WithUnauthorizedAgent_ThrowsException
- GetAccessibleAssetsForBotAgentAsync_WithValidKey_ReturnsAssets
- GetAccessibleAssetsForBotAgentAsync_WithInvalidKey_ReturnsNull

// Encryption/Decryption
- CreateAssetAsync_WithEncryptedValue_EncryptsCorrectly
- GetAssetValueAsync_WithEncryptedAsset_DecryptsCorrectly
- UpdateAssetAsync_WithEncryptionChange_HandlesCorrectly

// Authorization
- AuthorizeAssetForBotAgentAsync_WithValidData_AuthorizesSuccessfully
- RevokeAssetAuthorizationAsync_WithValidData_RevokesSuccessfully
- GetAuthorizedBotAgentsAsync_WithValidAsset_ReturnsAgents
```

### 1.3 Missing Domain Entity Tests

#### OrganizationUnitInvitation Tests
**File**: `OpenAutomate.Core.Tests/DomainTests/OrganizationUnitInvitationTests.cs`

#### PasswordResetToken Tests  
**File**: `OpenAutomate.Core.Tests/DomainTests/PasswordResetTokenTests.cs`

---

## Phase 2: API Layer Tests

### Priority: 🔴 HIGH - Sprint 1-2 (UPDATED)

### 2.1 ✅ COMPLETED Controller Tests
**Status**: Major controller coverage achieved!
- ✅ BotAgentAssetController Tests (9 tests) - **COMPLETED**
- ✅ BotAgentConnectionController Tests (21 tests) - **COMPLETED**
- ✅ EmailVerificationController Tests (10 tests) - **COMPLETED**
- ✅ OrganizationUnitInvitationController Tests (14 tests) - **COMPLETED**

### 2.2 🔄 Missing Critical Controller Tests

#### AutomationPackageController Tests
**File**: `OpenAutomate.API.Tests/ControllerTests/AutomationPackageControllerTests.cs`
**Priority**: 🔴 HIGH (Core automation functionality)

**Test Cases**:
```csharp
// Package CRUD
- CreatePackage_WithValidData_ReturnsCreated
- CreatePackage_WithInvalidData_ReturnsBadRequest
- CreatePackage_WithoutAuth_ReturnsUnauthorized
- GetPackage_WithValidId_ReturnsPackage
- GetPackage_WithInvalidId_ReturnsNotFound
- GetPackage_CrossTenant_ReturnsNotFound
- GetAllPackages_FiltersByTenant_ReturnsCorrectPackages
- UpdatePackage_WithValidData_ReturnsUpdated
- UpdatePackage_CrossTenant_ReturnsNotFound
- DeletePackage_WithValidId_ReturnsNoContent
- DeletePackage_WithActiveExecutions_ReturnsBadRequest

// Version Management
- UploadVersion_WithValidFile_ReturnsCreated
- UploadVersion_WithInvalidFile_ReturnsBadRequest
- UploadVersion_WithDuplicateVersion_ReturnsConflict
- DownloadVersion_WithValidVersion_ReturnsFile
- DownloadVersion_WithInvalidVersion_ReturnsNotFound
- DownloadVersion_CrossTenant_ReturnsNotFound
- DeleteVersion_WithValidVersion_ReturnsNoContent
- DeleteVersion_WithActiveExecutions_ReturnsBadRequest

// Error Handling
- CreatePackage_WhenServiceThrows_ReturnsInternalServerError
- UploadVersion_WhenStorageUnavailable_ReturnsServiceUnavailable
```

#### ExecutionController Tests
**File**: `OpenAutomate.API.Tests/ControllerTests/ExecutionControllerTests.cs`
**Priority**: 🔴 HIGH (Core execution functionality)

**Test Cases**:
```csharp
// Execution Management
- CreateExecution_WithValidData_ReturnsCreated
- CreateExecution_WithInvalidBotAgent_ReturnsBadRequest
- CreateExecution_WithInvalidPackage_ReturnsBadRequest
- GetExecution_WithValidId_ReturnsExecution
- GetExecution_WithInvalidId_ReturnsNotFound
- GetExecution_CrossTenant_ReturnsNotFound
- GetExecutions_FiltersByTenant_ReturnsCorrectExecutions

// Status Updates
- UpdateStatus_WithValidData_ReturnsOk
- UpdateStatus_WithInvalidExecution_ReturnsNotFound
- UpdateStatus_CrossTenant_ReturnsNotFound
- UpdateStatus_WithInvalidStatus_ReturnsBadRequest

// Logs
- GetLogs_WithValidExecution_ReturnsLogs
- GetLogs_WithInvalidExecution_ReturnsNotFound
- GetLogs_CrossTenant_ReturnsNotFound
- AppendLog_WithValidData_ReturnsOk
- AppendLog_WithInvalidExecution_ReturnsNotFound

// Real-time Updates
- GetExecutionStream_WithValidId_ReturnsStream
- GetExecutionStream_WithInvalidId_ReturnsNotFound
```

#### SchedulesController Tests
**File**: `OpenAutomate.API.Tests/ControllerTests/SchedulesControllerTests.cs`
**Priority**: 🔴 HIGH (Scheduling is key capstone feature)

**Test Cases**:
```csharp
// Schedule CRUD
- CreateSchedule_WithValidData_ReturnsCreated
- CreateSchedule_WithInvalidCron_ReturnsBadRequest
- GetSchedule_WithValidId_ReturnsSchedule
- GetSchedule_WithInvalidId_ReturnsNotFound
- GetSchedule_CrossTenant_ReturnsNotFound
- GetAllSchedules_FiltersByTenant_ReturnsCorrectSchedules
- UpdateSchedule_WithValidData_ReturnsUpdated
- UpdateSchedule_CrossTenant_ReturnsNotFound
- DeleteSchedule_WithValidId_ReturnsNoContent
- DeleteSchedule_CrossTenant_ReturnsNotFound

// Schedule Management
- EnableSchedule_WithValidId_ReturnsOk
- DisableSchedule_WithValidId_ReturnsOk
- TriggerSchedule_WithValidId_ReturnsOk
- GetNextExecutions_WithValidId_ReturnsExecutionTimes

// Validation
- ValidateCronExpression_WithValidCron_ReturnsOk
- ValidateCronExpression_WithInvalidCron_ReturnsBadRequest
```

#### Additional Missing Controllers
- AuthorController Tests
- EmailTestController Tests
- OrganizationUnitUserController Tests

### 2.2 OData Controller Tests

#### OData Controllers Test Suite
**Files**: `OpenAutomate.API.Tests/ControllerTests/OData/`

**Controllers to Test**:
- AssetsController
- AutomationPackagesController
- BotAgentsController  
- ExecutionsController
- OrganizationUnitInvitationsController
- OrganizationUnitUsersController
- PackageVersionsController
- UsersController

**Common Test Patterns**:
```csharp
// OData Query Support
- Get_WithFilterQuery_ReturnsFilteredResults
- Get_WithSelectQuery_ReturnsSelectedFields
- Get_WithOrderByQuery_ReturnsOrderedResults
- Get_WithTopQuery_ReturnsLimitedResults
- Get_WithSkipQuery_ReturnsSkippedResults
- Get_WithExpandQuery_ReturnsExpandedResults

// Tenant Isolation
- Get_FiltersByTenant_ReturnsOnlyTenantData
- Get_CrossTenantQuery_ReturnsEmpty

// Security
- Get_WithoutAuth_ReturnsUnauthorized
- Get_WithInvalidAuth_ReturnsUnauthorized
```

---

## Phase 3: Infrastructure Layer Tests

### Priority: 🟡 MEDIUM - Sprint 2-3

### 3.1 Missing Repository Tests

#### Repository Test Files Needed:
- `BotAgentRepositoryTests.cs`
- `AutomationPackageRepositoryTests.cs`
- `PackageVersionRepositoryTests.cs`
- `ExecutionRepositoryTests.cs`
- `ScheduleRepositoryTests.cs`
- `OrganizationUnitRepositoryTests.cs`
- `AuthorityRepositoryTests.cs`
- `RefreshTokenRepositoryTests.cs`
- `EmailVerificationTokenRepositoryTests.cs`
- `PasswordResetTokenRepositoryTests.cs`
- `OrganizationUnitInvitationRepositoryTests.cs`

**Common Repository Test Patterns**:
```csharp
// CRUD Operations
- AddAsync_WithValidEntity_AddsSuccessfully
- GetByIdAsync_WithValidId_ReturnsEntity
- GetByIdAsync_WithInvalidId_ReturnsNull
- GetAllAsync_ReturnsAllEntities
- UpdateAsync_WithValidEntity_UpdatesSuccessfully
- DeleteAsync_WithValidEntity_DeletesSuccessfully

// Tenant Filtering
- GetAllAsync_FiltersByTenant_ReturnsOnlyTenantData
- GetByIdAsync_CrossTenant_ReturnsNull

// Query Operations
- FindAsync_WithPredicate_ReturnsMatchingEntities
- CountAsync_WithPredicate_ReturnsCorrectCount
- ExistsAsync_WithValidId_ReturnsTrue
- ExistsAsync_WithInvalidId_ReturnsFalse

// Relationship Handling
- AddAsync_WithNavigationProperties_HandlesCorrectly
- GetWithIncludeAsync_LoadsRelatedData
- DeleteAsync_WithDependentEntities_HandlesCorrectly
```

### 3.2 Service Implementation Tests

#### Service Implementation Test Files:
- `ExecutionServiceImplementationTests.cs`
- `AutomationPackageServiceImplementationTests.cs`
- `ScheduleServiceImplementationTests.cs`
- `AwsSesEmailServiceTests.cs`
- `AuthorizationManagerImplementationTests.cs`

### 3.3 Database Context Tests

#### ApplicationDbContext Tests
**File**: `OpenAutomate.Infrastructure.Tests/DbContext/ApplicationDbContextTests.cs`

**Test Cases**:
```csharp
// Entity Configuration
- OnModelCreating_ConfiguresEntitiesCorrectly
- OnModelCreating_ConfiguresRelationshipsCorrectly
- OnModelCreating_ConfiguresIndexesCorrectly
- OnModelCreating_ConfiguresConstraintsCorrectly

// Tenant Filtering
- SaveChanges_SetsTenantIdAutomatically
- Query_FiltersByTenantAutomatically
- Query_CrossTenant_FiltersCorrectly

// Audit Fields
- SaveChanges_SetsCreatedAtAutomatically
- SaveChanges_UpdatesLastModifyAtAutomatically
- SaveChanges_PreservesCreatedAtOnUpdate
```

---

## Phase 4: Integration Tests

### Priority: 🟡 MEDIUM - Sprint 3

### 4.1 API Integration Tests

#### End-to-End Test Scenarios
**File**: `OpenAutomate.Integration.Tests/`

**Test Scenarios**:
```csharp
// User Journey Tests
- CompleteUserRegistrationFlow_WorksEndToEnd
- UserLoginAndTokenRefresh_WorksEndToEnd
- BotAgentRegistrationAndAssetAccess_WorksEndToEnd
- PackageUploadAndExecution_WorksEndToEnd
- ScheduleCreationAndExecution_WorksEndToEnd

// Multi-Tenant Scenarios
- MultiTenantDataIsolation_WorksCorrectly
- CrossTenantAccessPrevention_WorksCorrectly
- TenantSwitching_WorksCorrectly

// Error Scenarios
- DatabaseConnectionFailure_HandledGracefully
- ExternalServiceFailure_HandledGracefully
- ConcurrentAccess_HandledCorrectly
```

### 4.2 Service Integration Tests

#### Cross-Service Communication Tests
```csharp
// Service Interaction Tests
- ExecutionService_WithBotAgentService_IntegratesCorrectly
- AssetService_WithBotAgentService_IntegratesCorrectly
- ScheduleService_WithExecutionService_IntegratesCorrectly
- EmailService_WithUserService_IntegratesCorrectly

// Transaction Tests
- MultiServiceTransaction_RollsBackOnFailure
- MultiServiceTransaction_CommitsOnSuccess
```

---

## Phase 5: Specialized Tests

### Priority: 🟢 LOW-MEDIUM - Sprint 3-4

### 5.1 Middleware Tests

#### Middleware Test Files:
- `JwtAuthenticationMiddlewareTests.cs`
- `TenantResolutionMiddlewareTests.cs`
- `ErrorHandlingMiddlewareTests.cs`
- `RequestLoggingMiddlewareTests.cs`

### 5.2 SignalR Hub Tests

#### BotAgentHub Tests
**File**: `OpenAutomate.API.Tests/Hubs/BotAgentHubTests.cs`

**Test Cases**:
```csharp
// Connection Management
- OnConnectedAsync_WithValidMachineKey_ConnectsSuccessfully
- OnConnectedAsync_WithInvalidMachineKey_RejectsConnection
- OnDisconnectedAsync_UpdatesBotAgentStatus
- OnDisconnectedAsync_CleansUpResources

// Real-time Communication
- SendExecutionUpdate_ToConnectedClients_DeliversMessage
- SendStatusUpdate_ToSpecificClient_DeliversMessage
- BroadcastMessage_ToTenantClients_DeliversToCorrectClients

// Authentication
- Hub_WithJwtAuth_AuthenticatesCorrectly
- Hub_WithMachineKeyAuth_AuthenticatesCorrectly
- Hub_WithInvalidAuth_RejectsConnection
```

### 5.3 Security Tests

#### Security Test Suite
**File**: `OpenAutomate.Security.Tests/`

**Test Categories**:
```csharp
// Authentication Tests
- JwtToken_WithValidClaims_AuthenticatesCorrectly
- JwtToken_WithExpiredToken_RejectsAuthentication
- JwtToken_WithInvalidSignature_RejectsAuthentication
- MachineKey_WithValidKey_AuthenticatesCorrectly
- MachineKey_WithInvalidKey_RejectsAuthentication

// Authorization Tests
- UserAccess_WithCorrectRole_AllowsAccess
- UserAccess_WithIncorrectRole_DeniesAccess
- TenantAccess_WithCorrectTenant_AllowsAccess
- TenantAccess_WithIncorrectTenant_DeniesAccess

// Input Validation Tests
- SqlInjection_InUserInput_PreventedCorrectly
- XssAttack_InUserInput_PreventedCorrectly
- PathTraversal_InFileUpload_PreventedCorrectly
- LargePayload_Attack_HandledCorrectly

// Encryption Tests
- AssetEncryption_WorksCorrectly
- PasswordHashing_WorksCorrectly
- TokenEncryption_WorksCorrectly
```

---

## Phase 6: Performance & Load Tests

### Priority: 🔵 LOW - Sprint 4

### 6.1 Performance Tests

#### Performance Test Suite
**File**: `OpenAutomate.Performance.Tests/`

**Test Categories**:
```csharp
// Database Performance
- DatabaseQuery_Performance_MeetsThresholds
- DatabaseInsert_Performance_MeetsThresholds
- DatabaseUpdate_Performance_MeetsThresholds
- ComplexQuery_Performance_MeetsThresholds

// API Performance
- ControllerResponse_Time_MeetsThresholds
- FileUpload_Performance_MeetsThresholds
- FileDownload_Performance_MeetsThresholds
- ConcurrentRequests_Performance_MeetsThresholds

// Memory Performance
- MemoryUsage_UnderLoad_StaysWithinLimits
- MemoryLeaks_Detection_PassesChecks
- GarbageCollection_Performance_Acceptable
```

### 6.2 Load Tests

#### Load Test Scenarios
```csharp
// User Load Tests
- ConcurrentUsers_100_HandledCorrectly
- ConcurrentUsers_500_HandledCorrectly
- ConcurrentUsers_1000_HandledCorrectly

// Bot Agent Load Tests
- ConcurrentBotAgents_50_HandledCorrectly
- ConcurrentBotAgents_200_HandledCorrectly
- ConcurrentExecutions_100_HandledCorrectly

// File Operations Load Tests
- ConcurrentFileUploads_HandledCorrectly
- ConcurrentFileDownloads_HandledCorrectly
- LargeFileOperations_HandledCorrectly
```

---

## Implementation Guidelines

### Test Infrastructure Setup

#### Required NuGet Packages
```xml
<!-- Testing Framework -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />

<!-- Mocking -->
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />

<!-- Code Coverage -->
<PackageReference Include="coverlet.collector" Version="6.0.4" />
<PackageReference Include="coverlet.msbuild" Version="6.0.4" />

<!-- Integration Testing -->
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />

<!-- Performance Testing -->
<PackageReference Include="NBomber" Version="5.0.0" />
<PackageReference Include="BenchmarkDotNet" Version="0.13.12" />
```

#### Test Database Configuration
```csharp
// In-Memory Database for Unit Tests
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

// SQLite Database for Integration Tests
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={Path.GetTempFileName()}"));
```

#### Mock Factory Pattern
```csharp
public static class MockFactory
{
    public static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var mock = new Mock<IUnitOfWork>();
        // Setup common mock behavior
        return mock;
    }

    public static Mock<ITenantContext> CreateTenantContext(Guid? tenantId = null)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns(tenantId ?? Guid.NewGuid());
        return mock;
    }
}
```

#### Test Data Builders
```csharp
public class UserBuilder
{
    private User _user = new User();

    public UserBuilder WithEmail(string email)
    {
        _user.Email = email;
        return this;
    }

    public UserBuilder WithTenant(Guid tenantId)
    {
        _user.OrganizationUnitId = tenantId;
        return this;
    }

    public User Build() => _user;
}
```

### CI/CD Integration

#### GitHub Actions Workflow
```yaml
name: Test Suite
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore
    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
    - name: Upload coverage
      uses: codecov/codecov-action@v3
```

#### Code Coverage Targets
- **Overall Coverage**: 80%+
- **Core Services**: 90%+
- **Controllers**: 85%+
- **Repositories**: 85%+
- **Domain Entities**: 95%+

---

## Sprint Planning

### Sprint 1 (UPDATED - Current Focus)
**Focus**: Complete critical missing tests for capstone features
- ✅ ExecutionService interface tests (COMPLETED)
- ✅ AutomationPackageService interface tests (COMPLETED)
- ✅ Major controller tests (COMPLETED)
- 🔄 **NEXT**: ScheduleService implementation tests (Quartz.NET integration)
- 🔄 **NEXT**: CronExpressionService tests (scheduling validation)
- 🔄 **NEXT**: AutomationPackageController tests
- 🔄 **NEXT**: ExecutionController tests
- 🔄 **NEXT**: SchedulesController tests

### Sprint 2 (Week 2)
**Focus**: Complete API layer and expand infrastructure
- 🔄 Remaining controller tests (Author, EmailTest, OrganizationUnitUser)
- 🔄 OData controller tests (8 controllers)
- 🔄 Missing repository tests (9+ repositories)
- 🔄 Enhanced service tests

### Sprint 3 (Week 3)
**Focus**: Integration and middleware tests
- 🔄 End-to-end integration tests
- 🔄 Middleware tests (JWT, Tenant Resolution, Error Handling)
- 🔄 SignalR hub tests (BotAgentHub)
- 🔄 Security tests

### Sprint 4 (Week 4)
**Focus**: Performance and specialized tests
- 🔄 Performance tests
- 🔄 Load tests
- 🔄 Advanced security tests
- 🔄 Documentation and cleanup

---

## Success Metrics

### Quantitative Metrics
- **Code Coverage**: 80%+ overall
- **Test Count**: 500+ tests
- **Test Execution Time**: <5 minutes for full suite
- **Build Success Rate**: 95%+

### Qualitative Metrics
- **Bug Detection**: Early detection of regressions
- **Confidence**: High confidence in deployments
- **Maintainability**: Easy to add new tests
- **Documentation**: Clear test documentation

---

## Test Categories Summary

### By Priority (UPDATED)
- **🔴 HIGH (Sprint 1)**: ~80 critical tests remaining
  - ScheduleService implementation tests (20 tests)
  - CronExpressionService tests (15 tests)
  - AutomationPackageController tests (20 tests)
  - ExecutionController tests (15 tests)
  - SchedulesController tests (15 tests)

- **🟡 MEDIUM (Sprint 2-3)**: ~200 infrastructure tests
  - Repository tests (9 missing repositories × 15 tests = 135 tests)
  - Service implementation tests (3 services × 10 tests = 30 tests)
  - Database context tests (20 tests)
  - OData controller tests (8 controllers × 8 tests = 64 tests)

- **🟢 LOW-MEDIUM (Sprint 3-4)**: ~100 specialized tests
  - Integration tests (30 tests)
  - Middleware tests (4 middleware × 8 tests = 32 tests)
  - SignalR hub tests (15 tests)
  - Security tests (25 tests)

- **🔵 LOW (Sprint 4)**: ~50 performance tests
  - Performance benchmarks (20 tests)
  - Load testing scenarios (20 tests)
  - Memory and resource tests (10 tests)

### By Layer (UPDATED)
- **API Layer**: ~100 tests remaining
  - 3 critical controllers × 20 tests = 60 tests
  - 8 OData controllers × 8 tests = 64 tests
  - Minus overlap = ~100 tests

- **Core Layer**: ~35 tests remaining
  - 2 service implementations × 17 tests = 34 tests

- **Infrastructure Layer**: ~155 tests remaining
  - 9 repositories × 15 tests = 135 tests
  - Database context tests = 20 tests

- **Integration & Specialized**: ~100 tests
  - Integration scenarios: 30 tests
  - Middleware: 32 tests
  - SignalR: 15 tests
  - Security: 25 tests
  - Performance: 50 tests

**Current Total**: 415 tests ✅
**Estimated Remaining**: ~390 tests
**Projected Final Total**: ~805 tests

---

## Conclusion

This comprehensive testing plan ensures robust coverage of the OpenAutomate backend system. The phased approach allows for incremental implementation while prioritizing the most critical components first. Regular execution of this test suite will provide confidence in system reliability and facilitate safe continuous deployment.

The plan covers all layers of the application architecture and includes specialized testing for security, performance, and integration scenarios. Following this plan will result in a well-tested, maintainable, and reliable backend system.

### Next Steps
1. **Review and approve** this testing plan
2. **Set up test infrastructure** (NuGet packages, CI/CD)
3. **Begin Sprint 1** with critical service tests
4. **Establish code coverage monitoring**
5. **Regular progress reviews** at end of each sprint

### Key Benefits
- **Risk Reduction**: Early detection of bugs and regressions
- **Quality Assurance**: Consistent code quality across all components
- **Developer Confidence**: Safe refactoring and feature development
- **Documentation**: Tests serve as living documentation
- **Maintainability**: Easier to maintain and extend the system
