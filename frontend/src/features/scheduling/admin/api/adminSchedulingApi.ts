// src/features/scheduling/admin/api/adminSchedulingApi.ts
import api from "../../../../services/api";
import type { WeeklyAssignment } from "../types/assignments";

/** תוצאה פר־משמרת מהשרת לפי ProcessShiftsCommandHandler */
export type ProcessShiftServerItem = {
    shiftId: string;
    startTime: string;              // ISO-UTC
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
  
    // קיימים רק כשיש תכנון מלא
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

  export type ApproveShiftEmployeesResponse = {
    success: boolean;
    approvedCount: number;
    message?: string;
  };

/** API אדמין - ניהול שיבוצים */
export const adminSchedulingApi = {
  /**
   * קיים בשרת: GET /api/ai/shifts/process
   * מחזיר מערך פריטים פר־משמרת או אובייקט { message }
   */
  async processShifts(params: { startDate?: string; endDate?: string } = {}) {
    const res = await api.get<ProcessShiftServerItem[] | { message: string }>(
      "/ai/shifts/process",
      { params }
    );
    return res.data;
  },

  /**
   * לפאזה הבאה - דורש endpoint תואם בצד שרת:
   * POST /api/admin/scheduling/create-week-shifts
   */
  async createWeekShifts(request: { weekStart: string }): Promise<WeeklyAssignment> {
    const response = await api.post<WeeklyAssignment>(
      "/admin/scheduling/create-week-shifts",
      request
    );
    return response.data;
  },

  /**
   * לפאזה הבאה - דורש endpoint תואם בצד שרת:
   * POST /api/admin/scheduling/confirm-assignments
   */
  async confirmWeekAssignments(assignments: WeeklyAssignment): Promise<{ success: boolean }> {
    const response = await api.post<{ success: boolean }>(
      "/admin/scheduling/confirm-assignments",
      assignments
    );
    return response.data;
  },

  async approveShiftEmployees(shiftId: string, employeeIds: string[]) {
    const { data } = await api.post<ApproveShiftEmployeesResponse>(
      `/admin/scheduling/shifts/${shiftId}/approve-employees`,
      { shiftId, employeeIds }
    );
    return data;
  },
};
