import type { WeeklyAssignment, DayAssignments, EmployeeMini } from "../types/assignments";
import type { ProcessShiftServerItem } from "../api/adminSchedulingApi";

function toIsoIL(startTime: string): string | null {
  if (!startTime) return null;
  let s = startTime.trim();

  if (s.includes(" ") && !s.includes("T")) s = s.replace(" ", "T");
  s = s.replace(/\.(\d{3})\d+([+-]\d{2}:\d{2}|Z)?$/, ".$1$2");

  const d = new Date(s);
  if (Number.isNaN(d.getTime())) return null;

  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Jerusalem",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(d);
}

// עדיפות ל-Early במקרה כפילות באותו יום
export function mapProcessToWeekly(items: ProcessShiftServerItem[], weekStartISO: string): WeeklyAssignment {
  const dayBuckets = new Map<
    string,
    { early: EmployeeMini[]; regular: EmployeeMini[]; earlyIds: Set<string>; regularIds: Set<string> }
  >();

  for (const it of items) {
    const iso = toIsoIL(it.startTime);
    if (!iso) continue; // 👈 חשוב: לדלג אם התאריך לא תקין

    if (!dayBuckets.has(iso)) {
      dayBuckets.set(iso, { early: [], regular: [], earlyIds: new Set(), regularIds: new Set() });
    }
    const bucket = dayBuckets.get(iso)!;

    const planned = "planned" in it && Array.isArray(it.planned) ? it.planned : [];
    for (const p of planned) {
      const emp: EmployeeMini = {
        id: String(p.id),
        name: p.name,
        canEarly: p.arrivalType === "Early",
      };

      if (p.arrivalType === "Early") {
        if (!bucket.earlyIds.has(emp.id)) {
          bucket.early.push(emp);
          bucket.earlyIds.add(emp.id);
          if (bucket.regularIds.has(emp.id)) {
            bucket.regular = bucket.regular.filter(e => e.id !== emp.id);
            bucket.regularIds.delete(emp.id);
          }
        }
      } else {
        if (!bucket.earlyIds.has(emp.id) && !bucket.regularIds.has(emp.id)) {
          bucket.regular.push(emp);
          bucket.regularIds.add(emp.id);
        }
      }
    }
  }

  const days: DayAssignments[] = Array.from(dayBuckets.entries())
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([iso, v]) => ({ iso, early: v.early, regular: v.regular }));

  return { weekStart: weekStartISO, days };
}
