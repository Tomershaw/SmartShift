import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useScheduling } from "../hooks/useScheduling";
import { EmployeeCard } from "./EmployeeCard";
import { ShiftCard } from "./ShiftCard";
import { useEffect } from "react";

import { useAuth } from "../../auth/context/useAuth"; // הנתיב תלוי במיקום שלך



export const ScheduleView = () => {
  const navigate = useNavigate();
  const {logout,isAuthenticated} = useAuth();

 useEffect(() => {
  if (!isAuthenticated) {
    navigate("/auth");
  }
}, [isAuthenticated, navigate]);
  
  const [startDate, setStartDate] = useState(
    new Date().toISOString().split("T")[0]
  );
  const [endDate, setEndDate] = useState(
    new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split("T")[0]
  );

  const { employees, shifts, loading, error } = useScheduling(startDate, endDate);
 
  if (error) return <div className="text-center mt-10 text-red-500">שגיאה: {error}</div>;


 if(loading) {
  return (
    <div className="fixed inset-0 flex items-center justify-center bg-white">
      <div className="w-12 h-12 border-4 border-blue-500 border-dashed rounded-full animate-spin"></div>
    </div>
  );
}
  return (
    <div className="schedule-view p-8">
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-3xl font-bold text-gray-800">ניהול משמרות</h1>
        <button
          onClick={logout}
          className="bg-red-500 text-white px-4 py-2 rounded-lg hover:bg-red-600 transition"
        >
          התנתק
        </button>
      </div>

      <div className="date-range flex space-x-4 mb-8">
        <div>
          <label className="block text-gray-700 mb-1">מתאריך:</label>
          <input
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            className="p-2 border border-gray-300 rounded-lg"
          />
        </div>
        <div>
          <label className="block text-gray-700 mb-1">עד תאריך:</label>
          <input
            type="date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
            className="p-2 border border-gray-300 rounded-lg"
          />
        </div>
      </div>

      <div className="schedule-grid grid grid-cols-1 md:grid-cols-2 gap-8">
        <div className="employees">
          <h2 className="text-2xl font-semibold text-gray-700 mb-4">עובדים</h2>
          <ul className="space-y-2">
            {employees.map((employee) => (
              <EmployeeCard key={employee.id} employee={employee} />
            ))}
          </ul>
        </div>

        <div className="shifts">
          <h2 className="text-2xl font-semibold text-gray-700 mb-4">משמרות</h2>
          <ul className="space-y-2">
            {shifts.map((shift) => (
              <li key={shift.id}>
                <ShiftCard shift={shift} employees={employees} />
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
};
