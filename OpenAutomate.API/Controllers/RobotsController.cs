// OpenAutomate.API/Controllers/RobotsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenAutomate.API.Services;
using OpenAutomate.Common.Models;
using OpenAutomate.Core.Services;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RobotsController : ControllerBase
    {
        private readonly ILogger<RobotsController> _logger;
        private readonly RobotService _robotService;
        private readonly WebSocketConnectionManager _websocketManager;

        public RobotsController(
            ILogger<RobotsController> logger,
            RobotService robotService,
            WebSocketConnectionManager websocketManager)
        {
            _logger = logger;
            _robotService = robotService;
            _websocketManager = websocketManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRobots()
        {
            var robots = await _robotService.GetAllRobotsAsync();
            return Ok(robots);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRobot(Guid id)
        {
            var robot = await _robotService.GetByIdAsync(id);
            if (robot == null)
            {
                return NotFound();
            }
            return Ok(robot);
        }

        [HttpPost("connect")]
        public async Task<IActionResult> ConnectRobot([FromBody] RobotConnectionModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.MachineKey) || string.IsNullOrEmpty(model.MachineName))
            {
                return BadRequest("Invalid connection data");
            }

            try
            {
                var robot = await _robotService.ConnectRobotAsync(model);

                // Return success with robot ID
                return Ok(new RobotConnectionResponse
                {
                    RobotId = robot.Id,
                    Success = true,
                    Message = "Connected successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting robot: {MachineName}", model.MachineName);
                return StatusCode(500, "Error connecting robot: " + ex.Message);
            }
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] HeartbeatModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.MachineKey))
            {
                return BadRequest("Invalid heartbeat data");
            }

            try
            {
                bool result = await _robotService.UpdateRobotStatusAsync(model.MachineKey, true);
                if (!result)
                {
                    return NotFound("Robot not found");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing heartbeat for machine key: {MachineKey}", model.MachineKey);
                return StatusCode(500, "Error processing heartbeat: " + ex.Message);
            }
        }

        [HttpPost("command/{id}")]
        public async Task<IActionResult> SendCommand(Guid id, [FromBody] WebSocketCommandMessage command)
        {
            if (command == null)
            {
                return BadRequest("Command cannot be null");
            }

            try
            {
                var robot = await _robotService.GetByIdAsync(id);
                if (robot == null)
                {
                    return NotFound("Robot not found");
                }

                if (!robot.IsConnected)
                {
                    return BadRequest("Robot is not connected");
                }

                // Set the robotId in the command if not already set
                if (command.RobotId == null || command.RobotId == Guid.Empty)
                {
                    command.RobotId = id;
                }

                await _websocketManager.SendMessageAsync(robot.MachineKey, command);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending command to robot: {RobotId}", id);
                return StatusCode(500, "Error sending command: " + ex.Message);
            }
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> BroadcastCommand([FromBody] WebSocketCommandMessage command)
        {
            if (command == null)
            {
                return BadRequest("Command cannot be null");
            }

            try
            {
                await _websocketManager.BroadcastMessageAsync(command);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting command");
                return StatusCode(500, "Error broadcasting command: " + ex.Message);
            }
        }

        [HttpGet("ws")]
        public async Task AcceptWebSocket()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                return;
            }

            // Get the machine key from the header
            if (!HttpContext.Request.Headers.TryGetValue("X-Machine-Key", out var machineKeyValues) ||
                string.IsNullOrEmpty(machineKeyValues))
            {
                HttpContext.Response.StatusCode = 401;
                await HttpContext.Response.WriteAsync("Machine key is required");
                return;
            }

            string machineKey = machineKeyValues.ToString();

            // Verify the machine key exists in our database
            var robotExists = await _robotService.ExistsByMachineKeyAsync(machineKey);
            if (!robotExists)
            {
                HttpContext.Response.StatusCode = 403;
                await HttpContext.Response.WriteAsync("Invalid machine key");
                return;
            }

            // Accept the WebSocket connection
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            _logger.LogInformation("WebSocket connection accepted for machine key: {MachineKey}", machineKey);

            // Add the connection to the manager
            _websocketManager.AddConnection(machineKey, webSocket);

            // Update the robot status
            await _robotService.UpdateRobotStatusAsync(machineKey, true);

            // Request a status update from the robot
            await _websocketManager.SendMessageAsync(machineKey,
                new WebSocketCommandMessage(CommandTypes.Status));

            // Handle the WebSocket connection until it's closed
            await _websocketManager.HandleWebSocketMessagesAsync(webSocket, machineKey,
                HttpContext.RequestAborted);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRobot([FromBody] CreateRobotRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.MachineName))
            {
                return BadRequest("Machine name is required");
            }

            try
            {
                var robot = await _robotService.CreateRobotAsync(request.MachineName);

                // Return just the ID, name, and machine key
                return Ok(new
                {
                    Id = robot.Id,
                    MachineName = robot.MachineName,
                    MachineKey = robot.MachineKey,
                    Message = "Robot created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating robot with name: {MachineName}", request.MachineName);
                return StatusCode(500, $"Error creating robot: {ex.Message}");
            }
        }


        [HttpGet("admin/list")]
        [Authorize(Roles = "Admin")] // Requires authorization - implement as needed
        public async Task<IActionResult> ListAllRobots()
        {
            try
            {
                var robots = await _robotService.GetAllRobotsAsync();

                // Transform to admin view model with all details including machine keys
                var result = robots.Select(r => new RobotAdminViewModel
                {
                    Id = r.Id,
                    MachineName = r.MachineName,
                    MachineKey = r.MachineKey, // Important: only show to admins
                    UserName = r.UserName,
                    IpAddress = r.IpAddress,
                    IsConnected = r.IsConnected,
                    LastSeen = r.LastSeen,
                    AgentVersion = r.AgentVersion,
                    OsInfo = r.OsInfo,
                    Tags = r.Tags
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing all robots");
                return StatusCode(500, "Error listing robots: " + ex.Message);
            }
        }

        [HttpGet("validate")]
        public async Task<IActionResult> ValidateMachineKey([FromQuery] string machineKey)
        {
            if (string.IsNullOrEmpty(machineKey))
            {
                return BadRequest("Machine key is required");
            }

            try
            {
                bool exists = await _robotService.ExistsByMachineKeyAsync(machineKey);
                if (!exists)
                {
                    return NotFound("Invalid machine key");
                }

                return Ok(new { valid = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating machine key: {MachineKey}", machineKey);
                return StatusCode(500, "Error validating machine key: " + ex.Message);
            }
        }

        [HttpGet("status/{id}")]
        public async Task<IActionResult> GetRobotStatus(Guid id)
        {
            var robot = await _robotService.GetByIdAsync(id);
            if (robot == null)
            {
                return NotFound("Robot not found");
            }

            return Ok(new
            {
                id = robot.Id,
                machineName = robot.MachineName,
                isConnected = robot.IsConnected,
                lastSeen = robot.LastSeen,
                ipAddress = robot.IpAddress
            });
        }




    }

    public class RobotAdminViewModel
    {
        public Guid Id { get; set; }
        public string MachineName { get; set; }
        public string MachineKey { get; set; } // Machine key is shown to admins
        public string UserName { get; set; }
        public string IpAddress { get; set; }
        public bool IsConnected { get; set; }
        public DateTime LastSeen { get; set; }
        public string AgentVersion { get; set; }
        public string OsInfo { get; set; }
        public string[] Tags { get; set; }
    }


    public class CreateRobotRequest
    {
        public string MachineName { get; set; }
    }


    public class RobotConnectionResponse
    {
        public Guid RobotId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class HeartbeatModel
    {
        public string MachineKey { get; set; }
        public string MachineName { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
