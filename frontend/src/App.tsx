// src/App.tsx
import { Routes, Route, Navigate } from "react-router-dom";
import AuthPage from "./features/auth/components/AuthPage";
import { ProtectedRoute } from "./features/auth/components/ProtectedRoute";
import EmployeeSignupPage from "./features/scheduling/components/EmployeeSignupPage";
import AdminDashboardPage from "./features/scheduling/admin/pages/AdminDashboardPage";
import AdminShiftsPage from "./features/scheduling/admin/pages/AdminShiftsPage";
import AdminShiftSummaryPage from "./features/scheduling/admin/pages/AdminShiftSummaryPage";
import AdminRegisterEmployeePage from "./features/scheduling/admin/pages/AdminRegisterEmployeePage";
import AdminEmployeesPage from "./features/scheduling/admin/pages/ratings/pages/AdminEmployeesPage";
import AdminEmployeeParametersPage from "./features/scheduling/admin/pages/ratings/pages/AdminEmployeeParametersPage";
import AdminCreateShiftPage from "./features/scheduling/admin/pages/AdminCreateShiftPage";
import AdminCreateWeekPage from "./features/scheduling/admin/pages/AdminCreateWeekPage";

import "./App.css";

function App() {
  return (
    <Routes>
      {/* הזדהות */}
      <Route path="/auth" element={<AuthPage />} />

      {/* מנהל/מנג'ר - דף מנהל */}
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

      {/* ניהול עובדים - רישום עובד חדש */}
      <Route
        path="/admin/employees/register"
        element={
          <ProtectedRoute allowedRoles={["Admin", "Manager"]}>
            <AdminRegisterEmployeePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/ratings"
        element={
          <ProtectedRoute allowedRoles={["Admin", "Manager"]}>
            <AdminEmployeesPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/employees/:employeeId/parameters"
        element={
          <ProtectedRoute allowedRoles={["Admin", "Manager"]}>
            <AdminEmployeeParametersPage />
          </ProtectedRoute>
        }
      />
      {/* יצירת משמרת בודדת */}
      <Route
        path="/admin/shifts/create"
        element={
          <ProtectedRoute allowedRoles={["Admin", "Manager"]}>
            <AdminCreateShiftPage />
          </ProtectedRoute>
        }
      />

      {/* יצירת שבוע (ראשון–חמישי) */}
      <Route
        path="/admin/shifts/create-week"
        element={
          <ProtectedRoute allowedRoles={["Admin", "Manager"]}>
            <AdminCreateWeekPage />
          </ProtectedRoute>
        }
      />

      {/* ברירות מחדל */}
      <Route path="/" element={<Navigate to="/auth" replace />} />
      <Route path="*" element={<Navigate to="/auth" replace />} />
    </Routes>
  );
}

export default App;
