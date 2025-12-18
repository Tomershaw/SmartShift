import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { AuthContextType } from "./AuthContext";
import authService from "../api/authService";
import { AuthContext } from "./AuthContext";
import api from "../../../services/api";

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [user, setUser] = useState<AuthContextType["user"] | null>(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    const logout = () => {
      authService.logout();
      setUser(null);
      navigate("/auth");
    };

    const init = async () => {
      console.log("🔥 init started");

      let token = localStorage.getItem("token");
      const refreshToken = localStorage.getItem("refreshToken");

      console.log("📦 token:", token);
      console.log("📦 refreshToken:", refreshToken);

      if ((!token || authService.isTokenExpired(token)) && refreshToken) {
        try {
          console.log("🔄 Token expired – trying refresh on page load");

          const response = await api.post<{
            token: string;
            refreshToken: string;
          }>("/account/refresh-token", { refreshToken });

          token = response.data.token;
          const newRefreshToken = response.data.refreshToken;

          localStorage.setItem("token", token);
          localStorage.setItem("refreshToken", newRefreshToken);

          console.log("✅ Token refreshed on init");
        } catch (err) {
          console.error("❌ Failed to refresh token on init", err);
          logout();
          setLoading(false);
          return;
        }
      }

      // if (!token) {
      //   console.log("🚪 No token available – logging out");
      //   logout();
      //   setLoading(false);
      //   return;
      // }

      if (!token) {
        setLoading(false); // פשוט מאשרים שהטעינה נגמרה
        return; // ועוצרים כאן. זה הכל.
      }

      try {
        const payload = JSON.parse(atob(token.split(".")[1]));

        const role =
          payload[
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
          ] || payload.role;

        // 🔥 הוסף Gender!
        const gender =
          payload[
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/gender"
          ] || payload.gender;

        setUser({
          email: payload.email,
          role,
          gender, // 🔥
          exp: payload.exp * 1000,
        });

        // 🔥 הדפס לוודא
        console.log("✅ User restored from token");
        console.log("👤 Gender:", gender);
      } catch (err) {
        console.error("❌ Failed to parse token payload", err);
        logout();
      }

      setLoading(false);
    };

    init();
  }, [navigate]);

  const logout = async () => {
    await authService
      .logout()
      .catch(err => console.error("Logout error:", err));

    setUser(null);
    navigate("/auth");
  };

  if (loading) return null;

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, logout }}>
      {children}
    </AuthContext.Provider>
  );
};
