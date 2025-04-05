# OpenAutomate Project Summary

## 3.1. Capstone Project Name
**English**: OpenAutomate - Multi-Tenant Business Process Automation Management & Orchestration Platform using ASP.NET Core API, Worker, Next.js, Python

**Vietnamese**: OpenAutomate - Nền tảng đa người thuê để quản lý và điều phối tự động hóa quy trình doanh nghiệp sử dụng ASP.NET Core API, Worker, Next.js, Python

**Abbreviation**: BPAMS (Business Process Automation Management Platform)

## 3.2. Context
In today's business environment, automation processes have become essential for efficiency and competitiveness. Many organizations are dependent on commercial automation platforms that require substantial licensing costs, specialized training, and create vendor lock-in situations. These platforms often limit flexibility and scalability while requiring significant investment in proprietary technologies.

OpenAutomate addresses these challenges by providing an open-source alternative based on Python for business process automation management. By leveraging the accessibility and extensive library ecosystem of Python, organizations can significantly reduce costs while gaining greater control over their automation solutions. This approach eliminates vendor dependency and empowers teams to create, modify, and extend automation processes without the constraints of commercial licensing.

The platform is built with a multi-tenant architecture, allowing multiple organizations to use the system while ensuring data isolation and security between tenants through a shared database with tenant filtering approach. Each organization is represented as a tenant, and all data is isolated between tenants.

The platform enables businesses to transition from expensive proprietary solutions to a more flexible, cost-effective approach that can be tailored to specific organizational needs. This not only reduces operational costs but also allows for more rapid innovation and adaptation to changing business requirements.

### Objectives
- Provide a cost-effective alternative to expensive automation platforms
- Provide organizations with complete control over their automation assets
- Simplify the creation, deployment, and management of business process automation
- Empower technical teams with familiar Python-based technologies rather than proprietary systems
- Create a scalable platform that grows with organizational needs
- Facilitate integration with other systems and emerging technologies like AI
- Build technical expertise in widely-applicable Python skills rather than vendor-specific knowledge
- Support multi-tenant usage with complete data isolation between organizations

### Technology/Algorithm
- **Front-end**: Next.js with tenant-specific routing
- **Back-end**: ASP.NET Core API with multi-tenant architecture
- **Worker Service**: .NET Worker Service for background processing
- **Automation**: Python for execution of automation scripts
- **Database**: SQL Server with global query filters for tenant isolation
- **Authentication**: JWT with refresh tokens and tenant context
- **Real-time Communication**: WebSockets/SignalR for real-time monitoring
- **Version Control**: Git/GitHub

## 3.3. Research Contents
- Complete technology stack including Next.js, ASP.NET Core, and Worker services
- Multi-tenant architecture implementation with shared database and tenant filtering
- Real-time monitoring and control systems for distributed automation agents
- Security patterns for automation package deployment and execution with tenant isolation
- Full software development lifecycle from analysis to deployment
- Python-based automation implementation and package management
- Tenant resolution through URL path-based identification
- Global query filters for automatic tenant data isolation
- Tenant-aware database context and repositories

## 3.4. Expected Features
1. **User Authentication and Authorization**
   - Secure login system with role-based permissions
   - JWT with refresh token mechanism incorporating tenant context
   - Multi-tenant authorization ensuring users can only access their tenant's data

2. **Multi-Tenant Architecture**
   - Path-based tenant resolution (`/{tenant-slug}/api/resource`)
   - Data isolation between tenants at the database level
   - Ability for users to belong to multiple organizations

3. **Bot Agent Deployment and Registration**
   - Streamlined process for installing and registering automation agents across an organization
   - Automatic tenant association for bot agents

4. **Real-time Monitoring of Bot Activities**
   - Live dashboard showing bot status, performance, and execution progress
   - Tenant-specific monitoring views

5. **Execution Logging and Reporting**
   - Comprehensive logging of all automation activities with customizable reporting
   - Tenant-aware filtering of execution logs

6. **Centralized Configuration Management**
   - Single location to manage all environment settings, credentials, and parameters
   - Tenant-specific configuration options

7. **Package Distribution System**
   - Secure repository for storing and deploying automation packages to agents
   - Tenant-specific package management

8. **Scheduling**
   - Tools for scheduling automation tasks
   - Tenant-aware scheduling system

9. **Status Notifications and Alerts**
   - Customizable notification system for automation success, failures, and performance issues
   - Organization-specific alert configurations

10. **Performance Analytics and Metrics**
    - Dashboards and reports showing automation performance, trends, and optimization opportunities
    - Cross-tenant analytics for system administrators

11. **Role-based Access Control**
    - Granular permission system ensuring users can only access appropriate resources
    - Tenant-specific role assignments

12. **Audit Trail and Activity History**
    - Comprehensive logging of user activities and system changes for security and compliance
    - Tenant context included in all audit records

13. **Multi-environment Support**
    - Isolated development, testing, and production environments
    - Environment-specific tenant configurations

## Implementation Approach
The platform implements multi-tenancy using the shared database with tenant filtering approach:

- A single database instance hosts data for all tenants
- Each tenant-specific entity includes a reference to its tenant (Organization)
- Queries are automatically filtered by the current tenant using Entity Framework Core global query filters
- URL format follows the pattern: `domain.com/{tenant-slug}/api/resource`
- Tenant resolution is handled through middleware that extracts tenant information from the URL path

### Key Components
1. **Tenant Entity**: Organization entity serves as the tenant identifier
2. **Tenant Resolution Middleware**: Extracts tenant information from the URL path
3. **Tenant Context Service**: Provides access to the current tenant throughout the application
4. **Global Query Filters**: Ensures data isolation between tenants at the database level
5. **Tenant-aware Repositories**: Ensures all data operations respect tenant boundaries

This architecture provides a flexible, secure approach to hosting multiple organizations within the OpenAutomate platform while maintaining efficient resource utilization through a shared infrastructure. 