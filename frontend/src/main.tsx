import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import "./index.css";
import { AuthProvider } from "./features/auth/context/AuthProvider";
// CHANGED: מייבא את ה־Provider של הלודר הגלובלי
import { LoadingProvider } from "./features/appLoading/context/LoadingProvider"; // CHANGED

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <BrowserRouter>
      {/* CHANGED: עוטפים את כל האפליקציה בלודר הגלובלי מעל הראוטים */}
      <LoadingProvider>
        <AuthProvider>
          <App />
        </AuthProvider>
      </LoadingProvider>
    </BrowserRouter>
  </React.StrictMode>
);
