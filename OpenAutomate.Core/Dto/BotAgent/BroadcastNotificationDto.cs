using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.BotAgent
{
    /// <summary>
    /// DTO for broadcasting notifications to tenant clients
    /// </summary>
    public class BroadcastNotificationDto
    {
        /// <summary>
        /// The type of notification to broadcast
        /// </summary>
        [Required]
        public required string NotificationType { get; set; }
        
        /// <summary>
        /// Data payload for the notification
        /// </summary>
        public object? Data { get; set; }
    }
} 