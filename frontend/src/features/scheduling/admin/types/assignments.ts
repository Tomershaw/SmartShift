export type ShiftType = "early" | "regular";

export type EmployeeMini = {
  id: string;
  name: string;
  canEarly: boolean; // נרשם early -> true, רק regular -> false
};

export type DayAssignments = {
  iso: string;       // YYYY-MM-DD
  title?: string;    // אופציונלי להצגה
  early: EmployeeMini[];
  regular: EmployeeMini[];
};

export type WeeklyAssignment = {
  weekStart: string; // YYYY-MM-DD
  days: DayAssignments[];
};
