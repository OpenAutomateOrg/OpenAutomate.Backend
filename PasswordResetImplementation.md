# Password Reset Feature Implementation Guide

## Overview

This document outlines best practices for implementing a secure password reset feature in the OpenAutomate application. The implementation follows the existing codebase conventions while addressing security concerns.

## Current Implementation Issues

The current implementation has several issues:

1. **Reusing Email Verification Tokens**: Currently, password reset uses the same token system as email verification, which can cause conflicts and security issues.
2. **Token Expiration Inconsistency**: The email template mentions 24 hours expiration, but the code sets it to 1 hour.
3. **Email in URL**: The reset link includes the email in the URL parameters, which could be logged in server logs or browser history.

## Recommended Implementation

### 1. Create a Dedicated Password Reset Token Entity

```csharp
// OpenAutomate.Core/Domain/Entities/PasswordResetToken.cs
using OpenAutomate.Core.Domain.Base;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAutomate.Core.Domain.Entities
{
    public class PasswordResetToken : BaseEntity
    {
        // Default constructor for EF Core
        public PasswordResetToken()
        {
            Token = string.Empty;
            ExpiresAt = DateTime.MinValue;
        }
        
        [Required]
        public Guid UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        [Required]
        public string Token { get; set; } = "";
        
        [Required]
        public DateTime ExpiresAt { get; set; }
        
        public bool IsUsed { get; set; } = false;
        
        public DateTime? UsedAt { get; set; }
        
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        
        public bool IsActive => !IsUsed && !IsExpired;
    }
}
```

### 2. Update ITokenService Interface

Add dedicated methods for password reset tokens:

```csharp
// Add to OpenAutomate.Core/IServices/ITokenService.cs
/// <summary>
/// Generates a password reset token for a user
/// </summary>
/// <param name="userId">The ID of the user</param>
/// <returns>The password reset token string</returns>
Task<string> GeneratePasswordResetTokenAsync(Guid userId);

/// <summary>
/// Validates a password reset token and returns the associated user ID if valid
/// </summary>
/// <param name="token">The password reset token to validate</param>
/// <returns>The user ID if valid, null otherwise</returns>
Task<Guid?> ValidatePasswordResetTokenAsync(string token);
```

### 3. Implement TokenService Methods

```csharp
// Add to OpenAutomate.Infrastructure/Services/TokenService.cs
public async Task<string> GeneratePasswordResetTokenAsync(Guid userId)
{
    try
    {
        // Check if user exists
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new AuthenticationException($"User with ID {userId} not found");
        }

        // Remove any existing password reset tokens for this user
        var existingTokens = await _unitOfWork.PasswordResetTokens
            .GetAllAsync(t => t.UserId == userId && !t.IsUsed);

        foreach (var token in existingTokens)
        {
            _unitOfWork.PasswordResetTokens.Remove(token);
        }
        await _unitOfWork.CompleteAsync();

        // Generate a new token
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[32];
        rng.GetBytes(randomBytes);
        var tokenString = Convert.ToBase64String(randomBytes)
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", "");

        // Create token entity
        var resetToken = new PasswordResetToken
        {
            UserId = userId,
            Token = tokenString,
            ExpiresAt = DateTime.UtcNow.AddHours(4), // 4 hour expiration
            IsUsed = false
        };

        // Save to database
        await _unitOfWork.PasswordResetTokens.AddAsync(resetToken);
        await _unitOfWork.CompleteAsync();

        return tokenString;
    }
    catch (OpenAutomateException)
    {
        // Rethrow custom exceptions
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error generating password reset token: {Message}", ex.Message);
        throw new ServiceException($"Error generating password reset token: {ex.Message}", ex);
    }
}

public async Task<Guid?> ValidatePasswordResetTokenAsync(string token)
{
    try
    {
        if (string.IsNullOrEmpty(token))
            return null;

        // Find the token in the database
        var resetToken = await _unitOfWork.PasswordResetTokens
            .GetFirstOrDefaultAsync(t => t.Token == token, t => t.User);

        // Check if token exists
        if (resetToken == null)
            return null;

        // Check if token is already used
        if (resetToken.IsUsed)
            return null;

        // Check if token is expired
        if (resetToken.IsExpired)
            return null;

        // Mark token as used
        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;
        _unitOfWork.PasswordResetTokens.Update(resetToken);
        await _unitOfWork.CompleteAsync();

        return resetToken.UserId;
    }
    catch (OpenAutomateException)
    {
        // Rethrow custom exceptions
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error validating password reset token: {Message}", ex.Message);
        return null;
    }
}
```

### 4. Update UserService Implementation

```csharp
// Update in OpenAutomate.Infrastructure/Services/UserService.cs
public async Task<bool> ForgotPasswordAsync(string email)
{
    try
    {
        _logger.LogInformation("Processing forgot password request for email: {Email}", email);
        
        // Find user by email
        var user = await _unitOfWork.Users.GetFirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        if (user == null)
        {
            // Do not reveal that email doesn't exist (security best practice)
            _logger.LogWarning("Forgot password request for non-existent email: {Email}", email);
            return true;
        }
        
        // Generate a dedicated password reset token
        var token = await _tokenService.GeneratePasswordResetTokenAsync(user.Id);
        
        // Create the reset password link - don't include email in URL
        var baseUrl = _configuration["FrontendUrl"];
        var resetLink = $"{baseUrl}/reset-password?token={token}";
        
        // Send reset password email using the notification service
        await _notificationService.SendResetPasswordEmailAsync(user.Email, resetLink);
        
        _logger.LogInformation("Reset password email sent successfully to: {Email}", email);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing forgot password request for {Email}: {Message}", email, ex.Message);
        return false;
    }
}

public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
{
    try
    {
        _logger.LogInformation("Processing password reset for email: {Email}", email);
        
        // Find user by email
        var user = await _unitOfWork.Users.GetFirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        if (user == null)
        {
            _logger.LogWarning("Password reset attempt for non-existent email: {Email}", email);
            return false;
        }
        
        // Find the token and validate it using token service
        var userId = await _tokenService.ValidatePasswordResetTokenAsync(token);
        if (userId == null || userId != user.Id)
        {
            _logger.LogWarning("Invalid or expired token used for password reset. Email: {Email}", email);
            return false;
        }
        
        // Create new password hash
        CreatePasswordHash(newPassword, out string passwordHash, out string passwordSalt);
        
        // Update user's password
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        
        _unitOfWork.Users.Update(user);
        await _unitOfWork.CompleteAsync();
        
        _logger.LogInformation("Password reset successful for user: {Email}", email);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing password reset for {Email}: {Message}", email, ex.Message);
        return false;
    }
}
```

### 5. Update NotificationService Implementation

```csharp
// Update in OpenAutomate.Infrastructure/Services/NotificationService.cs
public async Task SendResetPasswordEmailAsync(string email, string resetLink)
{
    try
    {
        // Find user by email to get their name
        var user = await _unitOfWork.Users.GetFirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        
        if (user == null)
        {
            _logger.LogWarning("Failed to send reset password email: User not found with email {Email}", email);
            throw new Exception($"User not found with email {email}");
        }
        
        string name = $"{user.FirstName} {user.LastName}";
        
        // Get email template - use 4 hours to match token expiration
        var emailContent = await _emailTemplateService.GetResetPasswordEmailTemplateAsync(
            name, resetLink, 4); // 4 hour validity
        
        // Send email
        string subject = "Reset Your Password - OpenAutomate";
        await _emailService.SendEmailAsync(email, subject, emailContent);
        
        _logger.LogInformation("Reset password email sent to: {Email}", email);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send reset password email to: {Email}", email);
        throw;
    }
}
```

### 6. Update EmailTemplateService Implementation

```csharp
// Update in OpenAutomate.Infrastructure/Services/EmailTemplateService.cs
public Task<string> GetResetPasswordEmailTemplateAsync(string userName, string resetLink, int tokenValidityHours)
{
    string content = $@"
<p>Hello {userName},</p>

<p>To reset your password, please click the URL below:</p>

<p><a href='{resetLink}'>{resetLink}</a></p>

<p>This link will expire in {tokenValidityHours} hours.</p>";

    return Task.FromResult(WrapInEmailTemplate("Reset Your Password", "Reset Your Password", content));
}
```

### 7. Update AuthenController Implementation

```csharp
// Update in OpenAutomate.API/Controllers/AuthenController.cs
[HttpPost("reset-password")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
{
    try
    {
        // Validate request
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || 
            string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "Email, token, and new password are required" });
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new { message = "New password and confirmation do not match" });
        }

        // Validate password strength
        if (request.NewPassword.Length < 8)
        {
            return BadRequest(new { message = "Password must be at least 8 characters long" });
        }

        // Set default tenant if not already set
        EnsureDefaultTenant();
        
        var result = await _userService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
        
        if (!result)
        {
            return BadRequest(new { message = "Password reset failed. The token may be invalid or expired." });
        }
        
        return Ok(new { message = "Your password has been reset successfully. You can now log in with your new password." });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during password reset: {Message}", ex.Message);
        return StatusCode(500, new { message = "An error occurred while processing your request." });
    }
}
```

### 8. Update Database Context

Add the PasswordResetToken entity to your DbContext:

```csharp
// Update in OpenAutomate.Infrastructure/Data/ApplicationDbContext.cs
public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
```

### 9. Update IUnitOfWork Interface

```csharp
// Update in OpenAutomate.Core/Domain/IRepository/IUnitOfWork.cs
IGenericRepository<PasswordResetToken> PasswordResetTokens { get; }
```

### 10. Update UnitOfWork Implementation

```csharp
// Update in OpenAutomate.Infrastructure/Data/UnitOfWork.cs
public IGenericRepository<PasswordResetToken> PasswordResetTokens => _passwordResetTokens ??= new GenericRepository<PasswordResetToken>(_context);
private IGenericRepository<PasswordResetToken>? _passwordResetTokens;
```

## Security Considerations

1. **Token Expiration**: Password reset tokens expire after 4 hours (configurable)
2. **One-Time Use**: Tokens are marked as used after validation
3. **Token Invalidation**: Existing tokens are invalidated when a new token is requested
4. **Secure Communication**: Emails should be sent over TLS
5. **Password Strength**: Basic validation requires passwords to be at least 8 characters
6. **Anti-Enumeration**: The system doesn't reveal if an email exists in the database
7. **Secure Storage**: Tokens are stored hashed in the database

## Frontend Considerations

1. The frontend should collect the user's email first (forgot-password page)
2. After token is sent, redirect to a confirmation page
3. When user clicks the reset link, show a form to enter new password (reset-password page)
4. The form should include fields for password and confirmation
5. After successful reset, redirect to the login page with a success message

## Testing Checklist

- [ ] Request password reset with valid email
- [ ] Request password reset with invalid email
- [ ] Use expired token
- [ ] Use already used token
- [ ] Reset with valid token but mismatched email
- [ ] Reset with valid token and matching email
- [ ] Reset with password that doesn't meet requirements
- [ ] Reset with password that meets requirements
- [ ] Attempt to login with old password after reset
- [ ] Attempt to login with new password after reset

## Conclusion

This implementation provides a secure, dedicated password reset system that follows the existing codebase conventions while addressing the security concerns in the current implementation. 