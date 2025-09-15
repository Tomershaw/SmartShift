import { createContext } from "react";

export interface AuthContextType {
  user: {
    email: string;
    role: string;
    exp: number;
  } | null;
  isAuthenticated: boolean;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextType | undefined>(
  undefined
);
