// OpenAutomate.API/Services/ConnectionMonitorService.cs - Modified version
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Services;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAutomate.API.Services
{
    public class ConnectionMonitorService : BackgroundService
    {
        private readonly ILogger<ConnectionMonitorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, DateTime> _lastHeartbeatTimes = new();

        // Store WebSocketManager instances by machine key for cleanup
        private readonly ConcurrentDictionary<string, bool> _monitoredRobots = new();

        // Configure timeout (how long before marking robot as disconnected)
        private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(90);

        public ConnectionMonitorService(
            ILogger<ConnectionMonitorService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        // Called when a robot sends a heartbeat
        public void UpdateHeartbeat(string machineKey)
        {
            _lastHeartbeatTimes[machineKey] = DateTime.UtcNow;
        }

        // Called when a robot connects initially
        public void RegisterRobot(string machineKey)
        {
            _monitoredRobots[machineKey] = true;
            _lastHeartbeatTimes[machineKey] = DateTime.UtcNow;
            _logger.LogDebug("Robot {MachineKey} registered with connection monitor", machineKey);
        }

        // Called when a robot explicitly disconnects
        public void UnregisterRobot(string machineKey)
        {
            _monitoredRobots.TryRemove(machineKey, out _);
            _lastHeartbeatTimes.TryRemove(machineKey, out _);
            _logger.LogDebug("Robot {MachineKey} unregistered from connection monitor", machineKey);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Connection monitor service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var expiredConnections = new ConcurrentBag<string>();

                    // Check all robots with active heartbeats
                    foreach (var entry in _lastHeartbeatTimes)
                    {
                        var machineKey = entry.Key;
                        var lastHeartbeat = entry.Value;

                        // If heartbeat hasn't been received within timeout period, mark as disconnected
                        if ((now - lastHeartbeat) > _connectionTimeout)
                        {
                            _logger.LogWarning("Robot {MachineKey} connection timed out - last heartbeat was {LastHeartbeat}",
                                machineKey, lastHeartbeat);

                            expiredConnections.Add(machineKey);
                        }
                    }

                    // Process expired connections
                    foreach (var machineKey in expiredConnections)
                    {
                        // Remove from tracking
                        _lastHeartbeatTimes.TryRemove(machineKey, out _);
                        _monitoredRobots.TryRemove(machineKey, out _);

                        // Get WebSocketManager to close connection
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            try
                            {
                                // Update database directly
                                var robotService = scope.ServiceProvider.GetRequiredService<RobotService>();
                                await robotService.UpdateRobotStatusAsync(machineKey, false);

                                // Get the WebSocketConnectionManager to close the connection if it's still open
                                // This avoids circular reference at startup while still allowing cleanup during runtime
                                var webSocketManager = scope.ServiceProvider.GetRequiredService<WebSocketConnectionManager>();
                                await webSocketManager.RemoveConnectionAsync(machineKey);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error cleaning up expired connection for {MachineKey}", machineKey);
                            }
                        }
                    }

                    // Check every 30 seconds
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in connection monitor");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
    }
}
