import React, { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import type { UserDto, LoginRequestDto, LoginResponseDto } from '../types/api';
import apiClient from '../services/apiClient';

interface AuthContextType {
  user: UserDto | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequestDto) => Promise<LoginResponseDto>;
  logout: () => void;
  hasPermission: (permissionName: string) => boolean;
  isAdmin: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserDto | null>(() => {
    const saved = localStorage.getItem('currentUser');
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch {
        return null;
      }
    }
    return null;
  });

  const [token, setToken] = useState<string | null>(() => {
    return localStorage.getItem('token');
  });

  const [isLoading, setIsLoading] = useState<boolean>(false);

  useEffect(() => {
    const handleUnauthorized = () => {
      setUser(null);
      setToken(null);
    };

    window.addEventListener('auth:unauthorized', handleUnauthorized);
    return () => window.removeEventListener('auth:unauthorized', handleUnauthorized);
  }, []);

  const login = async (credentials: LoginRequestDto): Promise<LoginResponseDto> => {
    setIsLoading(true);
    try {
      const response = await apiClient.post<LoginResponseDto>('/api/auth/login', credentials);
      if (response.isSuccess && response.token && response.user) {
        setToken(response.token);
        setUser(response.user);
        localStorage.setItem('token', response.token);
        localStorage.setItem('currentUser', JSON.stringify(response.user));
      }
      return response;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = () => {
    setToken(null);
    setUser(null);
    localStorage.removeItem('token');
    localStorage.removeItem('currentUser');
    window.location.href = '/login';
  };

  const isAdmin = Boolean(
    user?.roles && (user.roles.includes('Admin') || user.roles.includes('admin') || user.roles.includes('مدير'))
  );

  const hasPermission = (permissionName: string): boolean => {
    if (!user) return false;
    // Admins bypass all specific page permission checks
    if (isAdmin) return true;
    return Boolean(user.permissions && user.permissions.includes(permissionName));
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: Boolean(token && user),
        isLoading,
        login,
        logout,
        hasPermission,
        isAdmin,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
