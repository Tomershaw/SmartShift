// src/App.tsx
import { Routes, Route, Navigate } from "react-router-dom";
import AuthPage from "./features/auth/components/AuthPage";
import { ProtectedRoute } from "./features/auth/components/ProtectedRoute";
import EmployeeSignupPage from "./features/scheduling/components/EmployeeSignupPage";
import AdminDashboardPage from "./features/scheduling/admin/pages/AdminDashboardPage";
import AdminShiftsPage from "./features/scheduling/admin/pages/AdminShiftsPage";
import AdminShiftSummaryPage from "./features/scheduling/admin/pages/AdminShiftSummaryPage";

import "./App.css";

function App() {
  return (
    <Routes>
      {/* הזדהות */}
      <Route path="/auth" element={<AuthPage />} />

      {/* מנהל/מנג'ר - דף המנהל החדש */}
      <Route
        path="/admin"
        element={
          <ProtectedRoute allowedRoles={["Admin", "Manager"]}>
            <AdminDashboardPage />
          </ProtectedRoute>
        }
      />

      {/* עובד - דף ההרשמה */}
      <Route
        path="/employee/signup"
        element={
          <ProtectedRoute allowedRoles={["Employee"]}>
            <EmployeeSignupPage />
          </ProtectedRoute>
        }
      />

      {/* ניהול משמרות - רשימה/גריד */}
      <Route
        path="/admin/shifts"
        element={
          <ProtectedRoute allowedRoles={["Admin", "Manager"]}>
            <AdminShiftsPage />
          </ProtectedRoute>
        }
      />

      {/* ניהול משמרת - תקציר משמרת בודדת */}
      <Route
        path="/admin/shifts/:shiftId/summary"
        element={
          <ProtectedRoute allowedRoles={["Admin", "Manager"]}>
            <AdminShiftSummaryPage />
          </ProtectedRoute>
        }
      />

      {/* השבתת המסך הישן והפניה לחדש */}
      <Route path="/schedule" element={<Navigate to="/admin" replace />} />

      {/* ברירות מחדל */}
      <Route path="/" element={<Navigate to="/auth" replace />} />
      <Route path="*" element={<Navigate to="/auth" replace />} />
    </Routes>
  );
}

export default App;
