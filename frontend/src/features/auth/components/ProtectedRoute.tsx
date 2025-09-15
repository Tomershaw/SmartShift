import { useContext } from "react";
import { Navigate } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";

type ProtectedRouteProps = {
  children: React.ReactNode;
  allowedRoles?: string[];
  redirectTo?: string;
};

export const ProtectedRoute = ({children,allowedRoles, redirectTo = "/auth",}: ProtectedRouteProps) => {
  const context = useContext(AuthContext);
  if (!context) return null;

  const { user, isAuthenticated } = context;

  // בזמן טעינת ה-AuthProvider
  if (user === null) return null;

  // לא מחובר
  if (!isAuthenticated) return <Navigate to={redirectTo} replace />;

  // בדיקת תפקידים אם הועברו
  if (allowedRoles?.length && user.role && !allowedRoles.includes(user.role)) {
    return <Navigate to={redirectTo} replace />;
  }

  return <>{children}</>;
};
