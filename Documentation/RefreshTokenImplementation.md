# Refresh Token Implementation Guide for OpenAutomate

## Overview

This document outlines the implementation of JWT authentication with refresh tokens for the OpenAutomate platform. The refresh token mechanism enhances security by using short-lived access tokens while maintaining user sessions through longer-lived refresh tokens.

## Architecture

The refresh token system consists of:

1. **Access Tokens**: Short-lived JWT tokens (15 minutes) used for API authentication
2. **Refresh Tokens**: Longer-lived tokens (7 days) used to obtain new access tokens
3. **Token Service**: Responsible for generating, validating, and rotating tokens
4. **User Entity**: Stores refresh tokens associated with user accounts
5. **Authentication Controller**: Provides endpoints for login, refresh, and revoke operations

## Data Model

### RefreshToken Entity

```csharp
public class RefreshToken
{
    public string Token { get; set; }
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; }
    public string CreatedByIp { get; set; }
    public DateTime? Revoked { get; set; }
    public string RevokedByIp { get; set; }
    public string ReplacedByToken { get; set; }
    public string ReasonRevoked { get; set; }
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsRevoked => Revoked != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
```

### User Entity Extension

```csharp
public class User
{
    // Existing properties
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string[] Roles { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    
    // Add refresh tokens collection
    public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
```

## Token Service

```csharp
public interface ITokenService
{
    TokenResponse GenerateTokens(User user, string ipAddress);
    TokenResponse RefreshToken(string token, string ipAddress);
    bool RevokeToken(string token, string ipAddress, string reason = null);
}
```

Implementation details:

1. **GenerateTokens**: 
   - Creates a new JWT access token with user claims
   - Generates a cryptographically secure refresh token
   - Stores refresh token with the user in database
   - Returns both tokens to the client

2. **RefreshToken**:
   - Validates the refresh token
   - If valid, generates new access and refresh tokens
   - Marks the old refresh token as replaced
   - Stores the new refresh token with the user
   - Returns new tokens to the client

3. **RevokeToken**:
   - Finds and validates the refresh token
   - Marks the token as revoked with reason
   - Saves changes to database

## API Endpoints

1. **Login Endpoint**:
   ```
   POST /api/auth/login
   ```
   - Authenticates user credentials
   - Generates access and refresh tokens
   - Returns tokens in response

2. **Refresh Endpoint**:
   ```
   POST /api/auth/refresh
   ```
   - Accepts expired access token and valid refresh token
   - Returns new access and refresh tokens

3. **Revoke Endpoint**:
   ```
   POST /api/auth/revoke
   ```
   - Invalidates a refresh token
   - Prevents further token refresh

## Security Considerations

1. **Token Storage**:
   - Server: Store hashed refresh tokens in database
   - Client: Store refresh token in HttpOnly cookie or secure storage

2. **Token Rotation**:
   - Generate new refresh token with each use
   - Maintain token lineage through ReplacedByToken property
   - Detect and prevent token reuse

3. **Token Revocation**:
   - Ability to invalidate tokens if compromise is suspected
   - Automatic revocation cascade for token families
   - Audit trail for token creation and revocation

4. **Token Lifetime**:
   - Access tokens: 15 minutes
   - Refresh tokens: 7 days
   - Configurable through appsettings.json

## Frontend Implementation

```typescript
// Authentication context setup
const AuthContext = createContext<AuthContextType | null>(null);

export const AuthProvider: React.FC = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  const login = async (username: string, password: string) => {
    const response = await api.post('/api/auth/login', { username, password });
    const { accessToken, refreshToken } = response.data;
    
    // Store tokens
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    
    // Decode and set user
    setUser(decodeToken(accessToken));
  };

  const refreshToken = async () => {
    try {
      const currentRefreshToken = localStorage.getItem('refreshToken');
      
      if (!currentRefreshToken) {
        return false;
      }
      
      const response = await api.post('/api/auth/refresh', {
        refreshToken: currentRefreshToken
      });
      
      const { accessToken, refreshToken } = response.data;
      
      localStorage.setItem('accessToken', accessToken);
      localStorage.setItem('refreshToken', refreshToken);
      
      setUser(decodeToken(accessToken));
      return true;
    } catch (error) {
      logout();
      return false;
    }
  };

  // Setup axios interceptor for automatic token refresh
  useEffect(() => {
    const interceptor = api.interceptors.response.use(
      (response) => response,
      async (error) => {
        const originalRequest = error.config;
        
        if (error.response?.status === 401 && !originalRequest._retry) {
          originalRequest._retry = true;
          
          if (await refreshToken()) {
            // Add new token to request header
            originalRequest.headers.Authorization = 
              `Bearer ${localStorage.getItem('accessToken')}`;
            return api(originalRequest);
          }
        }
        
        return Promise.reject(error);
      }
    );
    
    return () => {
      api.interceptors.response.eject(interceptor);
    };
  }, []);

  // Other auth functions...

  return (
    <AuthContext.Provider value={{ user, login, logout, refreshToken, loading }}>
      {children}
    </AuthContext.Provider>
  );
};
```

## Implementation Steps

1. Update Entity Framework models to include RefreshToken
2. Create migration for RefreshToken storage
3. Implement TokenService in Infrastructure project
4. Add TokenController endpoints
5. Configure JWT options in appsettings.json
6. Update authentication middleware configuration
7. Implement frontend authentication context
8. Test the complete authentication flow

## Conclusion

This refresh token implementation provides a secure, user-friendly authentication system for OpenAutomate. It balances security concerns (short-lived access tokens) with user experience (automatic refresh without requiring re-authentication) while providing robust token management capabilities. 