import { useState } from "react";
import { useScheduling } from "../hooks/useScheduling";
import { EmployeeCard } from "./EmployeeCard";
import { ShiftCard } from "./ShiftCard";

export const ScheduleView = () => {
  const [startDate, setStartDate] = useState(
    new Date().toISOString().split("T")[0]
  );
  const [endDate, setEndDate] = useState(
    new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split("T")[0]
  );

  const { employees, shifts, loading, error } = useScheduling(startDate, endDate);

  if (loading) return <div>טוען...</div>;
  if (error) return <div>שגיאה: {error}</div>;

  return (
    <div className="schedule-view">
      <h1>ניהול משמרות</h1>

      <div className="date-range">
        <div>
          <label>מתאריך:</label>
          <input
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
          />
        </div>
        <div>
          <label>עד תאריך:</label>
          <input
            type="date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
          />
        </div>
      </div>

      <div className="schedule-grid">
        <div className="employees">
          <h2>עובדים</h2>
          <ul>
            {employees.map((employee) => (
              <li key={employee.id}>
                <EmployeeCard employee={employee} />
              </li>
            ))}
          </ul>
        </div>

        <div className="shifts">
          <h2>משמרות</h2>
          <ul>
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