// OpenAutomate.Infrastructure/Repositories/RobotRepository.cs
using Microsoft.EntityFrameworkCore;
using OpenAutomate.Domain.Interfaces;
using OpenAutomate.Domain.Entities;
using OpenAutomate.Infrastructure.DbContext;


namespace OpenAutomate.Infrastructure.Repositories
{
    public class RobotRepository : IRobotRepository
    {
        private readonly ApplicationDbContext _context;

        public RobotRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Robot> GetByIdAsync(Guid id)
        {
            return await _context.Robots.FindAsync(id);
        }

        public async Task<Robot> GetByMachineKeyAsync(string machineKey)
        {
            return await _context.Robots
                .FirstOrDefaultAsync(r => r.MachineKey == machineKey);
        }

        public async Task<IEnumerable<Robot>> GetAllAsync()
        {
            return await _context.Robots.ToListAsync();
        }

        public async Task<IEnumerable<Robot>> GetConnectedAsync()
        {
            return await _context.Robots
                .Where(r => r.IsConnected)
                .ToListAsync();
        }

        public async Task AddAsync(Robot robot)
        {
            await _context.Robots.AddAsync(robot);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Robot robot)
        {
            _context.Robots.Update(robot);
            await _context.SaveChangesAsync();
        }
    }
}
