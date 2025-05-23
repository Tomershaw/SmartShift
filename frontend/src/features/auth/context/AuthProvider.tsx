import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { AuthContextType } from "./AuthContext";
import authService from "../api/authService";
import { AuthContext } from "./AuthContext";

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [user, setUser] = useState<AuthContextType["user"] | null>(null);
  const navigate = useNavigate();

 
  useEffect(() => {
    const logout = () => {
      authService.logout();
      setUser(null);
      navigate("/auth");
    };

    const init = () => {
      const token = localStorage.getItem("token");
      if (!token || authService.isTokenExpired(token)) {
        logout();
        return;
      }

      try {
        const payload = JSON.parse(atob(token.split(".")[1]));
        const role =
          payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.role;

        setUser({ email: payload.email, role, exp: payload.exp * 1000 });
      } catch {
        logout();
      }
    };

    init();
  }, [navigate]);

  const logout = () => {
    authService.logout();
    setUser(null);
    navigate("/auth");
  };

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, logout }}>
      {children}
    </AuthContext.Provider>
  );
};
