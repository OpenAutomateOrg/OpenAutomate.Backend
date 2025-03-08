// OpenAutomate.Domain/Entities/Robot.cs
using OpenAutomate.Domain.BaseEntity;
using System;

namespace OpenAutomate.Domain.Entities
{
    public class Robot : BaseEntity.BaseEntity
    {
        public string MachineKey { get; private set; }
        public string MachineName { get; private set; }

        // Make these properties nullable
        public string? UserName { get; private set; }
        public string? IpAddress { get; private set; }
        public bool IsConnected { get; private set; }
        public DateTime LastSeen { get; private set; }
        public string? AgentVersion { get; private set; }
        public string? OsInfo { get; private set; }

        // For EF Core
        protected Robot() { }

        public Robot(string machineName, string? machineKey = null)
        {
            MachineName = machineName ?? throw new ArgumentNullException(nameof(machineName));
            MachineKey = machineKey ?? Guid.NewGuid().ToString();
            LastSeen = DateTime.UtcNow;
            IsConnected = false;
        }

        public void UpdateConnectionStatus(bool isConnected)
        {
            IsConnected = isConnected;
            if (isConnected)
            {
                LastSeen = DateTime.UtcNow;
            }
        }

        public void UpdateInfo(string? userName, string? ipAddress, string? agentVersion, string? osInfo)
        {
            UserName = userName;
            IpAddress = ipAddress;
            AgentVersion = agentVersion;
            OsInfo = osInfo;
            LastSeen = DateTime.UtcNow;
        }
    }
}
