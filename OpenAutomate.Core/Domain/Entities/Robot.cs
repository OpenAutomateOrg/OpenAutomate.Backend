
namespace OpenAutomate.Core.Domain.Entities
{
    public class Robot : BaseEntity.BaseEntity
    {
        public string MachineKey { get; private set; }
        public string MachineName { get; private set; }
        public string? UserName { get; private set; }
        public string? IpAddress { get; private set; }
        public bool IsConnected { get; private set; }
        public DateTime LastSeen { get; private set; }
        public string? OsInfo { get; private set; }

    }
}
