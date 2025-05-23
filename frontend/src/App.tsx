import { Routes, Route, Navigate } from "react-router-dom";
import AuthPage from "./features/auth/components/AuthPage";
import { ScheduleView } from "./features/scheduling/components/ScheduleView";
import { ProtectedRoute } from "./features/auth/components/ProtectedRoute";
import "./App.css";

function App() {
  return (
    <Routes>
      <Route path="/auth" element={<AuthPage />} />
      <Route
        path="/schedule"
        element={
          <ProtectedRoute>
            <ScheduleView />
          </ProtectedRoute>
        }
      />
      <Route path="/" element={<Navigate to="/auth" />} />
      <Route path="*" element={<Navigate to="/auth" />} />
    </Routes>
  );
}

export default App;
