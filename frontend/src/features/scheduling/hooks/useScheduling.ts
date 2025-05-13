import { useState, useEffect } from "react";
import type { Employee, Shift } from "../types";
import { schedulingApi } from "../api/schedulingApi";

export function useScheduling(startDate: string, endDate: string) {
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [shifts, setShifts] = useState<Shift[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadData = async () => {
      try {
        setLoading(true);
        const [employeesData, shiftsData] = await Promise.all([
          schedulingApi.getEmployees(),
          schedulingApi.getShifts(startDate, endDate),
        ]);
        setEmployees(employeesData);
        setShifts(shiftsData);
        setError(null);
      } catch (err) {
        setError(
          `Failed to load scheduling data: ${
            err instanceof Error ? err.message : "Unknown error"
          }`
        );
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, [startDate, endDate]);

  const assignEmployeeToShift = async (shiftId: string, employeeId: string) => {
    try {
      const updatedShift = await schedulingApi.assignEmployeeToShift(
        shiftId,
        employeeId
      );
      setShifts(prev => prev.map(s => (s.id === shiftId ? updatedShift : s)));
      return updatedShift;
    } catch (err) {
      setError("Failed to assign employee to shift");
      throw err;
    }
  };

  const updateEmployeePriority = async (
    employeeId: string,
    priorityRating: number
  ) => {
    try {
      const updatedEmployee = await schedulingApi.updateEmployeePriority(
        employeeId,
        priorityRating
      );
      setEmployees(prev =>
        prev.map(e => (e.id === employeeId ? updatedEmployee : e))
      );
      return updatedEmployee;
    } catch (err) {
      setError("Failed to update employee priority");
      throw err;
    }
  };

  const refresh = async () => {
    try {
      setLoading(true);
      const [employeesData, shiftsData] = await Promise.all([
        schedulingApi.getEmployees(),
        schedulingApi.getShifts(startDate, endDate),
      ]);
      setEmployees(employeesData);
      setShifts(shiftsData);
      setError(null);
    } catch (err) {
      setError(
        `Failed to load scheduling data: ${
          err instanceof Error ? err.message : "Unknown error"
        }`
      );
    } finally {
      setLoading(false);
    }
  };

  return {
    employees,
    shifts,
    loading,
    error,
    assignEmployeeToShift,
    updateEmployeePriority,
    refresh,
  };
}
