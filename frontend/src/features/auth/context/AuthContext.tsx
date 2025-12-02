import { createContext } from "react";

export interface AuthContextType {
  user: {
    email: string;
    role: string;
    exp: number;
    gender?: 'Male' | 'Female' | 'Other';
  } | null;
  isAuthenticated: boolean;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextType | undefined>(
  undefined
);
