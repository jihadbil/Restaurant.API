import { createContext } from 'react';
import type { UserDto, LoginRequestDto, LoginResponseDto } from '../types/api';

export interface AuthContextType {
  user: UserDto | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequestDto) => Promise<LoginResponseDto>;
  logout: () => void;
  hasPermission: (permissionName: string) => boolean;
  isAdmin: boolean;
}
export const AuthContext = createContext<AuthContextType | undefined>(undefined);
