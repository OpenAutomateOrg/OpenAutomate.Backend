namespace OpenAutomate.Core.Domain.Dto.UserDto
{
    public class RevokeTokenRequest
    {
        public string Token { get; set; }
        public string Reason { get; set; }
    }
} 