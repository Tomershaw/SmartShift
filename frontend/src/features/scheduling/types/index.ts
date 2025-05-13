export interface Employee {
  id: string;
  name: string;
  email: string;
  phoneNumber: string;
  priorityRating: number;
  availabilities: Availability[];
  assignedShifts: Shift[];
}

export interface Shift {
  id: string;
  startTime: string;
  endTime: string;
  requiredPriorityRating: number;
  assignedEmployeeId?: string;
  status: ShiftStatus;
}

export interface Availability {
  id: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isRecurring: boolean;
}

export enum ShiftStatus {
  Open = "Open",
  Assigned = "Assigned",
  Completed = "Completed",
  Cancelled = "Cancelled",
}
