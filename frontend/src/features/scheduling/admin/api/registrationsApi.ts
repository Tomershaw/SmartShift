import api from "../../../../services/api";

/** תמונת מצב ליום אחד בשורת הימים */
export type RegistrationsSnapshotDayDto = {
  date: string; // "yyyy-MM-dd" מקומי (Asia/Jerusalem)
  requiredCount: number; // כמה נדרשים ביום
  registeredCount: number; // כמה נרשמו סהכ
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
  cancelledCount: number;
};

/** שם פרטי+משפחה + סטטוס ליום שנבחר */
export type DayRegistrantNameDto = {
  employeeId: string; // מזהה יציב לשורת הרשימה
  firstName: string;
  lastName: string;
  status: "Pending" | "Approved" | "Rejected" | "Cancelled";
  shiftDate: string; // YYYY-MM-DD (IL)
  shiftArrivalType: 1 | 2; // 1=Regular, 2=Early
};

/** תמונת מצב לטווח ימים - מזין את כפתורי א-ב-ג-ד-ה */
export async function getRegistrationsSnapshot(params: {
  from: string; // "yyyy-MM-dd" מקומי
  to: string; // "yyyy-MM-dd" מקומי
}): Promise<RegistrationsSnapshotDayDto[]> {
  const { data } = await api.get<RegistrationsSnapshotDayDto[]>(
    "/admin/registrations/snapshot",
    { params }
  );
  return data;
}

/** שמות ליום מסוים - ברירת מחדל כדאי לקרוא עם status=Pending */
export async function getDayRegistrants(params: {
  date: string; // "yyyy-MM-dd" מקומי
  status?: "Pending" | "Approved" | "Rejected" | "Cancelled";
  skip?: number; // דיפולט 0
  take?: number; // דיפולט 50
}): Promise<DayRegistrantNameDto[]> {
  const { date, status, skip = 0, take = 50 } = params;
  const { data } = await api.get<DayRegistrantNameDto[]>(
    "/admin/registrations/day",
    { params: { date, status, skip, take } }
  );
  return data;
}
