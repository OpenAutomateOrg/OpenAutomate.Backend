// OpenAutomate.Core/Interfaces/IRobotRepository.cs

using OpenAutomate.Domain.Entities;

namespace OpenAutomate.Domain.Interfaces
{
    public interface IRobotRepository
    {
        Task<Robot> GetByIdAsync(Guid id);
        Task<Robot> GetByMachineKeyAsync(string machineKey);
        Task<IEnumerable<Robot>> GetAllAsync();
        Task<IEnumerable<Robot>> GetConnectedAsync();
        Task AddAsync(Robot robot);
        Task UpdateAsync(Robot robot);
    }
}
