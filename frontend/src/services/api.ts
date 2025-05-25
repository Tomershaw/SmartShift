import axios from "axios";
import authService from "../features/auth/api/authService"; // ודא שהנתיב הזה נכון אצלך

const api = axios.create({
  baseURL: "https://localhost:7002/api",
});

// ✅ מוסיף Authorization רק לבקשות רגילות, לא ל־refresh-token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");

  // ❌ אל תשלח Authorization אם זו בקשת refresh-token
  if (config.url?.includes("/account/refresh-token")) {
    console.log("⏭️ Skipping Authorization for refresh-token");
    return config;
  }

  console.log("📢 Sending Token:", token);
  if (token) {
    config.headers = config.headers || {};
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

// ✅ רענון טוקן אוטומטי אם קיבלנו שגיאת 401
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = localStorage.getItem("refreshToken");
        if (!refreshToken) throw new Error("No refresh token available");

        console.log("🔄 Attempting to refresh token...");

        // שליחת בקשת רענון (לא דרך api כדי לא להפעיל שוב את ה־interceptor)
        const refreshResponse = await axios.post<{
          token: string;
          refreshToken: string;
        }>("https://localhost:7002/api/account/refresh-token", {
          refreshToken,
        });

        const {
          token: newAccessToken,
          refreshToken: newRefreshToken,
        } = refreshResponse.data;

        console.log("✅ Token refreshed:", newAccessToken);

        // שמירה ב־localStorage
        localStorage.setItem("token", newAccessToken);
        localStorage.setItem("refreshToken", newRefreshToken);

        // עדכון הבקשה המקורית עם הטוקן החדש
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;

        // שליחה מחדש של הבקשה שנכשלה
        return api(originalRequest);
      } catch (refreshError) {
        console.error("❌ Failed to refresh token:", refreshError);

        // ביצוע Logout במקרה של כישלון
        await authService.logout();
        window.location.href = "/auth";
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export default api;
