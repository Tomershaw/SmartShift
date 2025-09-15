import { Routes, Route, Navigate } from "react-router-dom";
import AuthPage from "./features/auth/components/AuthPage";
import { ScheduleView } from "./features/scheduling/components/ScheduleView";
import { ProtectedRoute } from "./features/auth/components/ProtectedRoute";
import EmployeeSignupPlaceholder from "./features/scheduling/components/EmployeeSignupPage.tsx";
import "./App.css";

function App() {
  return (
    <Routes>
      <Route path="/auth" element={<AuthPage />} />

      {/* מסך ניהולי - רק Admin/Manager */}
      <Route
        path="/schedule"
        element={
          <ProtectedRoute allowedRoles={["Admin", "Manager"]}>
            <ScheduleView />
          </ProtectedRoute>
        }
      />

      {/* דף ההרשמה לעובד */}
      <Route
        path="/employee/signup"
        element={
          <ProtectedRoute allowedRoles={["Employee"]}>
            <EmployeeSignupPlaceholder />
          </ProtectedRoute>
        }
      />

      <Route path="/" element={<Navigate to="/auth" />} />
      <Route path="*" element={<Navigate to="/auth" />} />
    </Routes>
  );
}

export default App;
