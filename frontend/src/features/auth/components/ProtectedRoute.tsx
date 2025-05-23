import { Navigate } from "react-router-dom";
import type { ReactNode } from "react";
import authService from "../api/authService";

interface ProtectedRouteProps {
  children: ReactNode;
}

export const ProtectedRoute = ({ children }: ProtectedRouteProps) => {
  const token = localStorage.getItem("token");

  if (!token || authService.isTokenExpired(token)) {
    return <Navigate to="/auth" replace />;
  }

  return <>{children}</>;
};
