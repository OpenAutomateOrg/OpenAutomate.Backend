using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Domain.Interfaces.IRepository
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        IRepository<BotAgent> BotAgents { get; }
        IRepository<AutomationPackage> AutomationPackages { get; }
        IRepository<PackageVersion> PackageVersions { get; }
        IRepository<Execution> Executions { get; }
        IRepository<Schedule> Schedules { get; }
        IRepository<RefreshToken> RefreshTokens { get; }
        IRepository<Organization> Organizations { get; }
        IRepository<OrganizationUser> OrganizationUsers { get; }
        
        Task<int> CompleteAsync();
    }
} 