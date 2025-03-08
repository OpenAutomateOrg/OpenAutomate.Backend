using OpenAutomate.Common.Models;
using OpenAutomate.Core.Interfaces;
using OpenAutomate.Domain.Entities;


namespace OpenAutomate.Core.Services
{
    public class RobotService
    {
        private readonly IRobotRepository _robotRepository;

        public RobotService(IRobotRepository robotRepository)
        {
            _robotRepository = robotRepository;
        }

        public async Task<Robot> ConnectRobotAsync(RobotConnectionModel model)
        {
            // Check if robot exists by machine key
            var robot = await _robotRepository.GetByMachineKeyAsync(model.MachineKey);

            if (robot == null)
            {
                // Create new robot if not found
                robot = new Robot(model.MachineName, model.MachineKey);
                await _robotRepository.AddAsync(robot);
            }

            // Update robot information
            robot.UpdateConnectionStatus(true);
            robot.UpdateInfo(model.UserName, model.IpAddress, model.AgentVersion, model.OsInfo);

            await _robotRepository.UpdateAsync(robot);
            return robot;
        }

        public async Task<bool> UpdateRobotStatusAsync(string machineKey, bool isConnected)
        {
            var robot = await _robotRepository.GetByMachineKeyAsync(machineKey);
            if (robot == null)
            {
                return false;
            }

            robot.UpdateConnectionStatus(isConnected);
            await _robotRepository.UpdateAsync(robot);
            return true;
        }

        public async Task<IEnumerable<RobotConnectionModel>> GetAllRobotsAsync()
        {
            var robots = await _robotRepository.GetAllAsync();
            return robots.Select(r => new RobotConnectionModel
            {
                Id = r.Id,
                MachineName = r.MachineName,
                MachineKey = r.MachineKey,
                UserName = r.UserName,
                IpAddress = r.IpAddress,
                IsConnected = r.IsConnected,
                LastSeen = r.LastSeen,
                AgentVersion = r.AgentVersion,
                OsInfo = r.OsInfo
            });
        }

        public async Task<bool> ExistsByMachineKeyAsync(string machineKey)
        {
            if (string.IsNullOrEmpty(machineKey))
            {
                return false;
            }

            var robot = await _robotRepository.GetByMachineKeyAsync(machineKey);
            return robot != null;
        }

        public async Task<RobotConnectionModel> GetByIdAsync(Guid id)
        {
            var robot = await _robotRepository.GetByIdAsync(id);
            if (robot == null)
            {
                return null;
            }

            return new RobotConnectionModel
            {
                Id = robot.Id,
                MachineName = robot.MachineName,
                MachineKey = robot.MachineKey,
                UserName = robot.UserName,
                IpAddress = robot.IpAddress,
                IsConnected = robot.IsConnected,
                LastSeen = robot.LastSeen,
                AgentVersion = robot.AgentVersion,
                OsInfo = robot.OsInfo
            };
        }


        // In RobotService.cs - add or update this method
        public async Task<RobotConnectionModel> CreateRobotAsync(string machineName)
        {
            if (string.IsNullOrEmpty(machineName))
            {
                throw new ArgumentException("Machine name cannot be empty", nameof(machineName));
            }

            try
            {
                // Create the robot entity - the constructor will generate a machine key
                var robot = new Robot(machineName);

                // Save to database
                await _robotRepository.AddAsync(robot);

                // Return as DTO with just the essential fields
                return new RobotConnectionModel
                {
                    Id = robot.Id,
                    MachineName = robot.MachineName,
                    MachineKey = robot.MachineKey,
                    IsConnected = false,
                    LastSeen = robot.LastSeen
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create robot: {ex.Message}", ex);
            }
        }




    }
}
