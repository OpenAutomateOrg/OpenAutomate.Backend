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
        IRepository<Authority> Authorities { get; }
        IRepository<UserAuthority> UserAuthorities { get; }
        IRepository<AuthorityResource> AuthorityResources { get; }
        IRepository<Asset> Assets { get; }
        IRepository<AssetBotAgent> AssetBotAgents { get; }
        IRepository<EmailVerificationToken> EmailVerificationTokens { get; }
        
        // Get a repository for a specific entity type
        IRepository<T> GetRepository<T>() where T : class;

        Task<int> CompleteAsync();
    }
}