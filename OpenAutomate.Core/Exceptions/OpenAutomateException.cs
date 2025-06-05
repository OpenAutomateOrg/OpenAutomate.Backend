using System;

namespace OpenAutomate.Core.Exceptions
{
    /// <summary>
    /// Base exception class for application-specific exceptions
    /// </summary>
    public abstract class OpenAutomateException : Exception
    {
        protected OpenAutomateException(string message) : base(message)
        {
        }

        protected OpenAutomateException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// General exception for service-layer errors
    /// </summary>
    public class ServiceException : OpenAutomateException
    {
        public ServiceException(string message) : base(message)
        {
        }

        public ServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when authentication fails
    /// </summary>
    public class AuthenticationException : OpenAutomateException
    {
        public AuthenticationException(string message) : base(message)
        {
        }

        public AuthenticationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when email verification is required
    /// </summary>
    public class EmailVerificationRequiredException : OpenAutomateException
    {
        public EmailVerificationRequiredException(string message) : base(message)
        {
        }

        public EmailVerificationRequiredException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a user already exists
    /// </summary>
    public class UserAlreadyExistsException : OpenAutomateException
    {
        public string Email { get; }

        public UserAlreadyExistsException(string email) : base($"Email '{email}' is already registered")
        {
            Email = email;
        }

        public UserAlreadyExistsException(string email, Exception innerException) 
            : base($"Email '{email}' is already registered", innerException)
        {
            Email = email;
        }
    }

    /// <summary>
    /// Exception thrown for token-related errors
    /// </summary>
    public class TokenException : OpenAutomateException
    {
        public TokenException(string message) : base(message)
        {
        }

        public TokenException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a requested resource is not found
    /// </summary>
    public class NotFoundException : OpenAutomateException
    {
        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
} 