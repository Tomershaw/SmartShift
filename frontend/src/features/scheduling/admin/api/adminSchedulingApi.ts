// src/features/scheduling/admin/api/adminSchedulingApi.ts
import api from "../../../../services/api";
import type { WeeklyAssignment } from "../types/assignments";

/** תוצאה פר־משמרת מהשרת לפי ProcessShiftsCommandHandler */
export type ProcessShiftServerItem = {
  shiftId: string;
  startTime: string; // ISO-UTC
  required: number;
  minimum: number;
  minimumEarly: number;
  approvedCount: number;
  pendingCount: number;
  remainingNeeded: number;
  plannedCount: number;
  plannedEarlyCount: number;
  plannedRegularCount: number;
  meetsMinimumEarly: boolean;

  planned?: Array<{
    id: string;
    name: string;
    skillLevel: number;
    priorityRating: number;
    arrivalType: "Early" | "Regular" | "Unknown";
  }>;
  analysis?: unknown;
  summary?: unknown;
};

export type CreateShiftRequest = {
  name: string;
  startTime: string; // ISO-UTC
  requiredEmployeeCount: number;
  minimumEmployeeCount: number;
  minimumEarlyEmployees: number;
  skillLevelRequired: number;
  description: string;
};

export type ApproveShiftEmployeesResponse = {
  success: boolean;
  approvedCount: number;
  message?: string;
};

export type CreateShiftResponse = {
  success: boolean;
  message?: string;
  shiftId?: string;
  startTime?: string;
  shiftName?: string;
};

/** DTO בסיסי לנתוני /scheduling/shifts */
export type ShiftSummaryDto = {
  id: string;
  startTime: string;
  assignedEmployeeId?: string | null;
};

export type CancelShiftResponse = {
  success: boolean;
  message?: string;
  shiftId?: string;
  startTime?: string;
  shiftName?: string;
};

export type DeleteShiftResponse = {
  success: boolean;
  message?: string;
  shiftId?: string;
  startTime?: string;
  shiftName?: string;
};
/** API אדמין - ניהול שיבוצים */
export const adminSchedulingApi = {
  /** AI: עיבוד משמרות */
  async processShifts(params: { startDate?: string; endDate?: string } = {}) {
    const res = await api.get<ProcessShiftServerItem[] | { message: string }>(
      "/ai/shifts/process",
      { params }
    );
    return res.data;
  },

  /** יצירת משמרות שבועיות (אם ימומש בצד שרת) */
  async createWeekShifts(request: {
    weekStart: string;
  }): Promise<WeeklyAssignment> {
    const response = await api.post<WeeklyAssignment>(
      "/admin/scheduling/create-week-shifts",
      request
    );
    return response.data;
  },

  /** אישור שיבוצים (אם ימומש בצד שרת) */
  async confirmWeekAssignments(
    assignments: WeeklyAssignment
  ): Promise<{ success: boolean }> {
    const response = await api.post<{ success: boolean }>(
      "/admin/scheduling/confirm-assignments",
      assignments
    );
    return response.data;
  },

  /** אישור עובדים למשמרת קיימת */
  async approveShiftEmployees(shiftId: string, employeeIds: string[]) {
    const { data } = await api.post<ApproveShiftEmployeesResponse>(
      `/admin/scheduling/shifts/${shiftId}/approve-employees`,
      { shiftId, employeeIds }
    );
    return data;
  },

  /** יצירת משמרת בודדת */
  async createShift(payload: CreateShiftRequest): Promise<CreateShiftResponse> {
    const res = await api.post<CreateShiftResponse>(
      "/admin/scheduling/shifts",
      payload
    );
    return res.data;
  },

  /** משמרות בטווח תאריכים (לבדיקה אם תאריך תפוס) */
  async getShiftsInRange(
    startDate: string,
    endDate: string
  ): Promise<ShiftSummaryDto[]> {
    const { data } = await api.get<ShiftSummaryDto[]>("/scheduling/shifts", {
      params: { startDate, endDate },
    });
    return data;
  },

  async cancelShift(shiftId: string): Promise<CancelShiftResponse> {
    const { data } = await api.delete<CancelShiftResponse>(
      `/admin/scheduling/shifts/${shiftId}`
    );
    return data;
  },

  async deleteShift(shiftId: string): Promise<DeleteShiftResponse> {
    const res = await api.delete<DeleteShiftResponse>(
      `/admin/scheduling/shifts/${shiftId}/hard-delete`
    );
    return res.data;
  },
};
