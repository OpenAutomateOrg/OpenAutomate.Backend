using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Domain.IRepository;
using OpenAutomate.Infrastructure.DbContext;
using OpenAutomate.Core.Domain.IRepository;

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
        private IRepository<Organization> _organizationRepository;
        private IRepository<OrganizationUser> _organizationUserRepository;

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
            
        public IRepository<Organization> Organizations => 
            _organizationRepository ??= new Repository<Organization>(_context);
            
        public IRepository<OrganizationUser> OrganizationUsers => 
            _organizationUserRepository ??= new Repository<OrganizationUser>(_context);

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