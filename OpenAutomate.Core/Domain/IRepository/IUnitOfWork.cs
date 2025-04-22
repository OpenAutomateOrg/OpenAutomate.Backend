using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Domain.IRepository
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
        IRepository<OrganizationUnit> OrganizationUnits { get; }
        IRepository<OrganizationUnitUser> OrganizationUnitUsers { get; }

        Task<int> CompleteAsync();
    }
}