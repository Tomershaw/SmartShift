import axios from "axios";

const api = axios.create({
  baseURL: "https://localhost:7002/api",
});

// Interceptor – מוסיף אוטומטית את ה-Token לכל בקשה
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  console.log("📢 Sending Token:", token); // לראות אם באמת נשלח טוקן!
  if (token) {
    config.headers = config.headers || {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default api;
