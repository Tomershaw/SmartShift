import type { Employee, Shift } from "../types";
import api from "../../../services/api";

export const schedulingApi = {
  async getEmployees(): Promise<Employee[]> {
    const response = await api.get<Employee[]>("/scheduling/employees");
    return response.data;
  },

  async getShifts(startDate: string, endDate: string): Promise<Shift[]> {
    const response = await api.get<Shift[]>(`/scheduling/shifts`, {
      params: { startDate, endDate },
    });
    return response.data;
  },

  async assignEmployeeToShift(
    shiftId: string,
    employeeId: string
  ): Promise<Shift> {
    const response = await api.post<Shift>(`/scheduling/shifts/${shiftId}/assign`, { employeeId });
    return response.data;
  },

  async updateEmployeePriority(
    employeeId: string,
    priorityRating: number
  ): Promise<Employee> {
    const response = await api.put<Employee>(
      `/scheduling/employees/${employeeId}/priority`,
      { priorityRating }
    );
    return response.data;
  },

                                                                                         
};
