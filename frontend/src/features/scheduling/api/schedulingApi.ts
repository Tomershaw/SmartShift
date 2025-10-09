import type { Employee, Shift } from "../types";
import api from "../../../services/api";

/* ===================== Types ===================== */
export type RegisterForShiftRequest = {
  shiftId: string;
  shiftArrivalType: number; // 1=Regular, 2=Early
};

export type RegisterForShiftResponse = {
  message: string;
};

type MyShiftsResponse = {
  success: boolean;
  message: string | null;
  employeeId: string;
  employeeName: string | null;
  shifts: ShiftRow[];
};

type ShiftRow = {
  shiftId: string;
  startTime: string;                    // "2025-09-21T16:30:00"
  registrationStatus?: string | number; // אופציונלי
  shiftArrivalType?: string | number;   // אופציונלי
};

export type MyRegistration = {
  shiftId: string;
  shiftDate: string;        // ISO YYYY-MM-DD לפי Asia/Jerusalem
  shiftArrivalType: number; // 1=Regular, 2=Early
  status: number;           // 0=Pending, 1=Approved, 2=Rejected, 3=Cancelled
};

export type EmployeesShifts = {
  employeeId: string;
};

/* ===================== Mappers ===================== */
// ממיר סטטוס למספר 0..3
function toNumStatus(s: string | number | undefined): 0 | 1 | 2 | 3 {
  if (s === 1 || s === "Approved") return 1;
  if (s === 2 || s === "Rejected") return 2;
  if (s === 3 || s === "Cancelled") return 3;
  return 0; // Pending
}

// ממיר סוג הגעה למספר 1..2
function toNumArrival(a: string | number | undefined): 1 | 2 {
  return a === 2 || a === "Early" ? 2 : 1; // ברירת מחדל Regular
}

// תאריך "YYYY-MM-DD" לפי Asia/Jerusalem
function toIlISO(startTime: string): string {
  const d = new Date(startTime);
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Jerusalem",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(d);
}

/* ===================== API ===================== */
export const schedulingApi = {
  async getEmployees(): Promise<Employee[]> {
    const response = await api.get<Employee[]>("/scheduling/employees");
    return response.data;
  },

  async getShifts(startDate: string, endDate: string): Promise<Shift[]> {
    const response = await api.get<Shift[]>("/scheduling/shifts", {
      params: { startDate, endDate },
    });
    return response.data;
  },

  async assignEmployeeToShift(shiftId: string, employeeId: string): Promise<Shift> {
    const response = await api.post<Shift>(`/scheduling/shifts/${shiftId}/assign`, { employeeId });
    return response.data;
  },

  async updateEmployeePriority(employeeId: string, priorityRating: number): Promise<Employee> {
    const response = await api.put<Employee>(`/scheduling/employees/${employeeId}/priority`, {
      priorityRating,
    });
    return response.data;
  },

  async registerForShift(payload: RegisterForShiftRequest): Promise<RegisterForShiftResponse> {
    const response = await api.post<RegisterForShiftResponse>("/shifts/register", payload);
    return response.data;
  },

  async getMyRegistrations(startDate: string, endDate: string): Promise<MyRegistration[]> {
    const { data } = await api.get<MyShiftsResponse>(
      "/scheduling/employees/my-shifts",
      { params: { startDate, endDate } }
    );
   // console.log("RAW:", data);
   // console.table(data?.shifts ?? []);
    // אם אתה סומך 100% על השרת שזה תמיד מערך: החזר ישירות data.shifts.map(...)
    const rows = data.shifts ?? [];

    return rows.map((x) => ({
      shiftId: x.shiftId,
      shiftDate: toIlISO(x.startTime),
      status: toNumStatus(x.registrationStatus),
      shiftArrivalType: toNumArrival(x.shiftArrivalType),
    }));
  },
};
