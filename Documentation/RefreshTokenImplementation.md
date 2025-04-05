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

## EF Core Query Optimization

When working with computed properties in LINQ queries, Entity Framework Core cannot translate these properties to SQL, resulting in query translation errors. To address this, we implement a two-step approach:

```csharp
// Incorrect approach - will cause EF Core translation error
var token = _unitOfWork.RefreshTokens.GetFirstOrDefaultAsync(
    t => t.Token == refreshToken && !t.IsRevoked && !t.IsExpired,
    t => t.User).GetAwaiter().GetResult();

// Correct approach - separate database query from computed property checks
var token = _unitOfWork.RefreshTokens.GetFirstOrDefaultAsync(
    t => t.Token == refreshToken,
    t => t.User).GetAwaiter().GetResult();
    
if (token == null || token.IsRevoked || token.IsExpired)
    throw new Exception("Invalid token");
```

This pattern:
1. Queries the database using only properties that can be translated to SQL
2. Performs checks with computed properties in memory after retrieving the entity
3. Prevents "The LINQ expression could not be translated" exceptions

## API Endpoints

1. **Login Endpoint**:
   ```
   POST /api/auth/login
   ```
   - Authenticates user credentials
   - Generates access and refresh tokens
   - Returns tokens in response
   - Sets refresh token in HTTP-only cookie

2. **Refresh Endpoint**:
   ```
   POST /api/auth/refresh-token
   ```
   - Accepts refresh token from HTTP-only cookie
   - Returns new access token and sets new refresh token cookie
   - No request body is required as the token is extracted from cookies

3. **Revoke Endpoint**:
   ```
   POST /api/auth/revoke-token
   ```
   - Invalidates a refresh token
   - Accepts token from request body or cookie
   - Prevents further token refresh

## Security Considerations

1. **Token Storage**:
   - Server: Store refresh tokens in database
   - Client: Store refresh token in HttpOnly cookie
   - Client: Store access token in memory with sessionStorage fallback

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

The frontend implementation uses a provider architecture with React Context and prioritizes security with in-memory token storage and sessionStorage fallback for better user experience.

### AuthProvider Component

```tsx
'use client';

import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { AUTH_CONFIG } from '@/lib/config';
import { getAuthToken, setAuthToken } from '@/lib/api/client';

// Create context
const AuthContext = createContext(null);

// Export hook for easy context usage
export const useAuthContext = () => useContext(AuthContext);

// Public routes that don't require authentication
const publicRoutes = ['/login', '/register', '/forgot-password'];

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const router = useRouter();
  
  // Check if the current route is public
  const isPublicRoute = useCallback((path) => {
    return publicRoutes.some(route => path.startsWith(route));
  }, []);
  
  // Check authentication status on mount
  useEffect(() => {
    const checkAuth = async () => {
      // Get token from memory/session storage
      const token = getAuthToken();
      
      if (!token) {
        setIsAuthenticated(false);
        setUser(null);
        setIsLoading(false);
        return;
      }
      
      try {
        // Fetch user data with the token
        const response = await fetch(`${AUTH_CONFIG.baseUrl}/api/user`, {
          headers: {
            Authorization: `Bearer ${token}`
          }
        });
        
        if (response.ok) {
          const userData = await response.json();
          setUser(userData);
          setIsAuthenticated(true);
        } else {
          // If the token is invalid, try to refresh it
          const refreshed = await refreshToken();
          
          if (!refreshed) {
            setAuthToken(null);
            setIsAuthenticated(false);
            setUser(null);
          }
        }
      } catch (error) {
        console.error('Auth check failed:', error);
        setAuthToken(null);
        setIsAuthenticated(false);
        setUser(null);
      } finally {
        setIsLoading(false);
      }
    };
    
    checkAuth();
    
    // Set up event listener for tab focus to refresh token
    const handleFocus = () => {
      if (getAuthToken()) {
        refreshToken();
      }
    };
    
    window.addEventListener('focus', handleFocus);
    return () => window.removeEventListener('focus', handleFocus);
  }, []);
  
  // Refresh token function
  const refreshToken = async () => {
    try {
      const response = await fetch(`${AUTH_CONFIG.baseUrl}/api/auth/refresh-token`, {
        method: 'POST',
        credentials: 'include' // Include cookies
      });
      
      if (response.ok) {
        const data = await response.json();
        setAuthToken(data.token);
        setUser(data.user);
        setIsAuthenticated(true);
        return true;
      }
      
      return false;
    } catch (error) {
      console.error('Token refresh failed:', error);
      return false;
    }
  };
  
  // Return the provider with context value
  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated,
        isLoading,
        refreshToken
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
```

### Token Storage Strategy

```typescript
// In client.ts
// Token storage with fallback strategy:
// 1. In-memory (primary storage for best security)
// 2. sessionStorage (fallback for page refreshes)

let inMemoryToken: string | null = null;

// Safe access to sessionStorage
const getTokenFromSession = (): string | null => {
  if (typeof window === 'undefined') return null;
  try {
    return sessionStorage.getItem(AUTH_CONFIG.tokenKey);
  } catch (e) {
    return null;
  }
};

// Safe storage to sessionStorage
const setTokenToSession = (token: string | null): void => {
  if (typeof window === 'undefined') return;
  try {
    if (token) {
      sessionStorage.setItem(AUTH_CONFIG.tokenKey, token);
    } else {
      sessionStorage.removeItem(AUTH_CONFIG.tokenKey);
    }
  } catch (e) {
    // Ignore errors (private browsing mode, etc.)
  }
};

// Initialize token from session if available
if (typeof window !== 'undefined') {
  inMemoryToken = getTokenFromSession();
}

// Getter for token - prioritize memory storage but fall back to session
export const getAuthToken = (): string | null => {
  if (inMemoryToken) return inMemoryToken;
  
  // If no in-memory token, try to retrieve from session
  const sessionToken = getTokenFromSession();
  if (sessionToken) {
    // Restore to memory if found in session
    inMemoryToken = sessionToken;
  }
  
  return inMemoryToken;
};

// Setter for token - update both memory and session storage
export const setAuthToken = (token: string | null): void => {
  inMemoryToken = token;
  setTokenToSession(token);
};
```

## Server-Side Rendering Considerations

For Next.js applications, special care must be taken to handle authentication with server-side rendering (SSR):

1. Mark authentication components with the `'use client'` directive
2. Use a mounted state to prevent hydration mismatches
3. Provide consistent UI during server rendering and client hydration

```tsx
'use client';

import { useState, useEffect } from 'react';
import { useAuth } from '@/lib/hooks/use-auth';

export default function LoginPage() {
  const { login, isLoading } = useAuth();
  const [mounted, setMounted] = useState(false);
  
  // Set mounted state on client side
  useEffect(() => {
    setMounted(true);
  }, []);
  
  // Only show errors after component has mounted
  const [error, setError] = useState(null);
  const displayError = mounted ? error : null;
  
  // Show the same loading state on both server and client
  if (!mounted || isLoading) {
    return <div>Loading...</div>;
  }
  
  // Rest of component...
}
```

## Implementation Steps

1. Update Entity Framework models to include RefreshToken
2. Create migration for RefreshToken storage
3. Implement TokenService in Infrastructure project with optimized EF Core queries
4. Add AuthController endpoints
5. Configure JWT options in appsettings.json
6. Update authentication middleware configuration
7. Implement frontend AuthProvider component
8. Add TenantProvider for tenant context management
9. Optimize token storage with in-memory first approach
10. Test the complete authentication flow

## Conclusion

This refresh token implementation provides a secure, user-friendly authentication system for OpenAutomate. It balances security concerns (short-lived access tokens) with user experience (automatic refresh without requiring re-authentication) while providing robust token management capabilities. The implementation handles both server-side rendering compatibility and Entity Framework Core query optimization for better performance and reliability. 