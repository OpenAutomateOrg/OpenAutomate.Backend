using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Domain.IRepository;
using OpenAutomate.Infrastructure.DbContext;
using OpenAutomate.Core.Domain.IRepository;
using System.Data;

namespace OpenAutomate.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IRepository<User> _userRepository;
        private IRepository<BotAgent> _botAgentRepository;
        private IRepository<AutomationPackage> _automationPackageRepository;
        private IRepository<PackageVersion> _packageVersionRepository;
        private IRepository<Execution> _executionRepository;
        private IRepository<Schedule> _scheduleRepository;
        private IRepository<RefreshToken> _refreshTokenRepository;
        private IRepository<OrganizationUnit> _organizationUnitRepository;
        private IRepository<OrganizationUnitUser> _organizationUnitUserRepository;
        private IRepository<Authority> _authorityRepository;
        private IRepository<UserAuthority> _userAuthorityRepository;
        private IRepository<AuthorityResource> _authorityResourceRepository;
        private IRepository<Asset> _assets;
        private IRepository<AssetBotAgent> _assetBotAgents;
        private IRepository<EmailVerificationToken> _emailVerificationTokens;
        private IRepository<PasswordResetToken> _passwordResetTokens;
        private IRepository<OrganizationUnitInvitation> _organizationUnitInvitations;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IRepository<User> Users => _userRepository ??= new Repository<User>(_context);

        public IRepository<BotAgent> BotAgents => _botAgentRepository ??= new Repository<BotAgent>(_context);

        public IRepository<AutomationPackage> AutomationPackages => 
            _automationPackageRepository ??= new Repository<AutomationPackage>(_context);

        public IRepository<PackageVersion> PackageVersions => 
            _packageVersionRepository ??= new Repository<PackageVersion>(_context);

        public IRepository<Execution> Executions => _executionRepository ??= new Repository<Execution>(_context);

        public IRepository<Schedule> Schedules => _scheduleRepository ??= new Repository<Schedule>(_context);

        public IRepository<RefreshToken> RefreshTokens => 
            _refreshTokenRepository ??= new Repository<RefreshToken>(_context);
            
        public IRepository<OrganizationUnit> OrganizationUnits => 
            _organizationUnitRepository ??= new Repository<OrganizationUnit>(_context);
            
        public IRepository<OrganizationUnitUser> OrganizationUnitUsers => 
            _organizationUnitUserRepository ??= new Repository<OrganizationUnitUser>(_context);
            
        public IRepository<Authority> Authorities => 
            _authorityRepository ??= new Repository<Authority>(_context);
            
        public IRepository<UserAuthority> UserAuthorities => 
            _userAuthorityRepository ??= new Repository<UserAuthority>(_context);
            
        public IRepository<AuthorityResource> AuthorityResources => 
            _authorityResourceRepository ??= new Repository<AuthorityResource>(_context);

        public IRepository<Asset> Assets => _assets ??= new Repository<Asset>(_context);

        public IRepository<AssetBotAgent> AssetBotAgents => _assetBotAgents ??= new Repository<AssetBotAgent>(_context);
        
        public IRepository<EmailVerificationToken> EmailVerificationTokens => 
            _emailVerificationTokens ??= new Repository<EmailVerificationToken>(_context);
            
        public IRepository<PasswordResetToken> PasswordResetTokens =>
            _passwordResetTokens ??= new Repository<PasswordResetToken>(_context);
        public IRepository<OrganizationUnitInvitation> OrganizationUnitInvitations =>
            _organizationUnitInvitations ??= new Repository<OrganizationUnitInvitation>(_context);
            
        public IRepository<T> GetRepository<T>() where T : class
        {
            return new Repository<T>(_context);
        }
        
        public SqlCommand CreateCommand()
        {
            var connection = (SqlConnection)_context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
            
            var command = connection.CreateCommand();
            command.Transaction = (SqlTransaction)_context.Database.CurrentTransaction?.GetDbTransaction();
            return command;
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
} 