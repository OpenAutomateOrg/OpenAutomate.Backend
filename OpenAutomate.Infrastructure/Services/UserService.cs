using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.UserDto;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Exceptions;

namespace OpenAutomate.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly ILogger<UserService> _logger;
        private readonly INotificationService _notificationService;

        public UserService(
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            INotificationService notificationService,
            ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, string ipAddress)
        {
            try
            {
                var user = await _unitOfWork.Users.GetFirstOrDefaultAsync(
                    u => u.Email != null && u.Email.ToLower() == request.Email.ToLower());

                if (user == null)
                {
                    _logger.LogWarning("Authentication failed: User not found for email {Email}", request.Email);
                    throw new AuthenticationException("Invalid credentials");
                }

                // Skip password verification for external logins (e.g., Google)
                if (!string.IsNullOrEmpty(request.Password))
                {
                    if (!VerifyPasswordHash(request.Password, user.PasswordHash ?? string.Empty, user.PasswordSalt ?? string.Empty))
                    {
                        _logger.LogWarning("Authentication failed: Invalid password for user {Email}", request.Email);
                        throw new AuthenticationException("Invalid credentials");
                    }
                }

                // Check if email is verified
                if (!user.IsEmailVerified)
                {
                    _logger.LogWarning("Authentication failed: Email not verified for user {Email}", request.Email);
                    throw new EmailVerificationRequiredException("Please verify your email address before logging in. Check your inbox for a verification link or request a new one.");
                }

                _logger.LogInformation("User {Email} authenticated successfully", user.Email);
                return _tokenService.GenerateTokens(user, ipAddress);
            }
            catch (OpenAutomateException)
            {
                // Rethrow application exceptions as they are already properly typed
                throw;
            }
            catch (Exception ex)
            {
                throw new AuthenticationException($"Error during authentication for user: {request.Email}", ex);
            }
        }

        public async Task<AuthenticationResponse> RefreshTokenAsync(string refreshToken, string ipAddress)
        {
            try
            {
                return await Task.FromResult(_tokenService.RefreshToken(refreshToken, ipAddress));
            }
            catch (Exception ex)
            {
                throw new TokenException("Error during token refresh", ex);
            }
        }

        public async Task<bool> RevokeTokenAsync(string token, string ipAddress, string reason = "")
        {
            try
            {
                return await _tokenService.RevokeTokenAsync(token, ipAddress, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return false;
            }
        }

        public async Task<UserResponse> RegisterAsync(RegistrationRequest request, string ipAddress)
        {
            try
            {
                // Check if user already exists
                if (await _unitOfWork.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == request.Email.ToLower()))
                {
                    _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
                    throw new UserAlreadyExistsException(request.Email);
                }

                // Create password hash
                CreatePasswordHash(request.Password, out string passwordHash, out string passwordSalt);

                // Create new user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    IsEmailVerified = false,
                    CreatedAt = DateTime.UtcNow
                };

                // Add user to database
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("User {Email} registered successfully", user.Email);

                return MapToResponse(user);
            }
            catch (UserAlreadyExistsException)
            {
                // Rethrow as it's already the correct exception type
                throw;
            }
            catch (Exception ex)
            {
                throw new ServiceException($"Error during registration for user: {request.Email}", ex);
            }
        }
        
        public async Task<bool> VerifyUserEmailAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Email verification failed: User not found with ID {UserId}", userId);
                    return false;
                }
                
                // Update user's email verification status
                user.IsEmailVerified = true;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.CompleteAsync();
                
                _logger.LogInformation("Email verified for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for user {UserId}", userId);
                return false;
            }
        }
        
        public async Task<bool> SendVerificationEmailAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Failed to send verification email: User not found with ID {UserId}", userId);
                    return false;
                }
                
                // Send verification email
                await _notificationService.SendVerificationEmailAsync(
                    userId, 
                    user.Email ?? string.Empty, 
                    $"{user.FirstName} {user.LastName}");
                
                _logger.LogInformation("Verification email sent to user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email for user {UserId}", userId);
                return false;
            }
        }

        public async Task<UserResponse> GetByIdAsync(Guid id)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);

                return MapToResponse(user);
            }
            catch (Exception ex)
            {
                throw new ServiceException($"Error retrieving user with ID: {id}", ex);
            }
        }
        
        public async Task<UserResponse> GetByEmailAsync(string email)
        {
            try
            {
                var user = await _unitOfWork.Users.GetFirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());

                return MapToResponse(user);
            }
            catch (Exception ex)
            {
                throw new ServiceException($"Error retrieving user with email: {email}", ex);
            }
        }

        public UserResponse MapToResponse(User user)
        {               
            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                IsEmailVerified = user.IsEmailVerified,
                SystemRole = user.SystemRole
            };
        }

        #region Private Helper Methods

        private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            byte[] saltBytes = hmac.Key;
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            
            // Convert to Base64 strings for storage
            passwordSalt = Convert.ToBase64String(saltBytes);
            passwordHash = Convert.ToBase64String(hashBytes);
        }

        private static bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            // Convert from Base64 strings back to bytes
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] storedHashBytes = Convert.FromBase64String(storedHash);
            
            using var hmac = new HMACSHA512(saltBytes);
            byte[] computedHashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            
            // Compare the computed hash with the stored hash
            if (storedHashBytes.Length != computedHashBytes.Length)
                return false;
                
            for (int i = 0; i < computedHashBytes.Length; i++)
            {
                if (computedHashBytes[i] != storedHashBytes[i])
                    return false;

            }
            
            return true;
        }

        #endregion
    }
} 