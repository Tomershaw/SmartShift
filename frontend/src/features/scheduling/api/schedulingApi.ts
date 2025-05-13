import type { Employee, Shift } from "../types";

const API_BASE_URL = "https://localhost:7002/api/scheduling";

export const schedulingApi = {
  async getEmployees(): Promise<Employee[]> {
    const response = await fetch(`${API_BASE_URL}/employees`);
    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to fetch employees: ${text}`);
    }
    return response.json();
  },

  async getShifts(startDate: string, endDate: string): Promise<Shift[]> {
    const response = await fetch(
      `${API_BASE_URL}/shifts?startDate=${startDate}&endDate=${endDate}`
    );
    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to fetch shifts: ${text}`);
    }
    return response.json();
  },

  async assignEmployeeToShift(
    shiftId: string,
    employeeId: string
  ): Promise<Shift> {
    const response = await fetch(`${API_BASE_URL}/shifts/${shiftId}/assign`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ employeeId }),
    });
    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to assign employee to shift: ${text}`);
    }
    return response.json();
  },

  async updateEmployeePriority(
    employeeId: string,
    priorityRating: number
  ): Promise<Employee> {
    const response = await fetch(
      `${API_BASE_URL}/employees/${employeeId}/priority`,
      {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ priorityRating }),
      }
    );
    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to update employee priority: ${text}`);
    }
    return response.json();
  },
};
