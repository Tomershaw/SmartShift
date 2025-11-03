import api from "../../../../../../services/api";

export type EmployeeParametersDto = {
  skillLevel: number;
  priorityRating: number;
  maxShiftsPerWeek: number;
  adminNotes: string | null;
};

type UpdateEmployeeParametersRequest = {
  skillLevel: number;
  priorityRating: number;
  maxShiftsPerWeek: number;
  adminNotes: string | null;
};

type UpdateEmployeeParametersResponse = {
  success: boolean;
  message: string;
  employeeId: string;
  errors: string[];
};

export const employeeParametersApi = {
  // ✅ GET: להביא את הפרמטרים הקיימים לעובד
  async getParameters(employeeId: string): Promise<EmployeeParametersDto> {
    const { data } = await api.get<EmployeeParametersDto>(
      `/admin/employees/${employeeId}`
    );
    return data;
  },

  // ✅ PUT: לעדכן פרמטרים (מה שהיה לך)
  async updateParameters(
    employeeId: string,
    params: UpdateEmployeeParametersRequest
  ) {
    const { data } = await api.put<UpdateEmployeeParametersResponse>(
      `/admin/employees/${employeeId}/parameters`,
      params
    );
    return data;
  },
};
