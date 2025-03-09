// Updated WebSocketConnectionManager.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAutomate.Common.Models;
using OpenAutomate.Core.Services;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAutomate.API.Services
{
    public class WebSocketConnectionManager
    {
        private readonly ILogger<WebSocketConnectionManager> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

        public WebSocketConnectionManager(
            ILogger<WebSocketConnectionManager> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }





        public void AddConnection(string machineKey, WebSocket webSocket)
        {
            _connections.TryAdd(machineKey, webSocket);
            _logger.LogInformation("Robot connected with machine key: {MachineKey}", machineKey);
        }

        public async Task RemoveConnectionAsync(string machineKey)
        {
            if (_connections.TryRemove(machineKey, out var webSocket))
            {
                try
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Connection closed by server",
                            CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing WebSocket for machine key: {MachineKey}", machineKey);
                }

                // Create a scope to resolve the RobotService
                using (var scope = _serviceProvider.CreateScope())
                {
                    var robotService = scope.ServiceProvider.GetRequiredService<RobotService>();
                    await robotService.UpdateRobotStatusAsync(machineKey, false);
                }

                _logger.LogInformation("Robot disconnected with machine key: {MachineKey}", machineKey);
            }
        }

        public async Task SendMessageAsync(string machineKey, WebSocketCommandMessage message)
        {
            if (!_connections.TryGetValue(machineKey, out var webSocket) ||
                webSocket.State != WebSocketState.Open)
            {
                _logger.LogWarning("Cannot send message to disconnected robot: {MachineKey}", machineKey);
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                _logger.LogDebug("Message sent to robot {MachineKey}: {MessageType}",
                    machineKey, message.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to robot: {MachineKey}", machineKey);
                await RemoveConnectionAsync(machineKey);
            }
        }

        public async Task BroadcastMessageAsync(WebSocketCommandMessage message)
        {
            foreach (var connection in _connections)
            {
                await SendMessageAsync(connection.Key, message);
            }
        }

        public async Task HandleWebSocketMessagesAsync(WebSocket webSocket, string machineKey, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            WebSocketReceiveResult result = null;

            try
            {
                while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
                {
                    using var messageStream = new System.IO.MemoryStream();

                    do
                    {
                        result = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer), cancellationToken);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await RemoveConnectionAsync(machineKey);
                            return;
                        }

                        await messageStream.WriteAsync(
                            new ReadOnlyMemory<byte>(buffer, 0, result.Count),
                            cancellationToken);
                    }
                    while (!result.EndOfMessage);

                    messageStream.Seek(0, System.IO.SeekOrigin.Begin);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        using var reader = new System.IO.StreamReader(messageStream, Encoding.UTF8);
                        var messageText = await reader.ReadToEndAsync(cancellationToken);

                        await ProcessWebSocketMessageAsync(machineKey, messageText);
                    }
                }
            }
            catch (WebSocketException ex)
            {
                _logger.LogError(ex, "WebSocket error for machine key {MachineKey}", machineKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket message for machine key {MachineKey}", machineKey);
            }
            finally
            {
                await RemoveConnectionAsync(machineKey);
            }
        }




        private async Task ProcessWebSocketMessageAsync(string machineKey, string messageText)
        {
            try
            {
                var message = JsonSerializer.Deserialize<WebSocketCommandMessage>(messageText);

                _logger.LogDebug("Received message from robot {MachineKey}: {MessageType}",
                    machineKey, message.Type);

                // Create a scope to resolve the RobotService
                using (var scope = _serviceProvider.CreateScope())
                {
                    var robotService = scope.ServiceProvider.GetRequiredService<RobotService>();

                    switch (message.Type)
                    {
                        case CommandTypes.Heartbeat:
                            await robotService.UpdateRobotStatusAsync(machineKey, true);
                            break;

                        case CommandTypes.StatusResponse:
                            if (message.Payload != null)
                            {
                                // Convert payload to RobotConnectionModel and update
                                var model = JsonSerializer.Deserialize<RobotConnectionModel>(
                                    JsonSerializer.Serialize(message.Payload));

                                if (model != null)
                                {
                                    model.MachineKey = machineKey; // Ensure correct key
                                    await robotService.ConnectRobotAsync(model);
                                }
                            }
                            break;

                        case CommandTypes.TaskProgress:
                        case CommandTypes.TaskComplete:
                            // Handle task updates
                            // TODO: Implement task tracking
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WebSocket message from robot {MachineKey}", machineKey);
            }
        }
    }
}
