import { useEffect, useMemo, useState } from "react";
import { Check } from "lucide-react";
import { useAuth } from "../../auth/context/useAuth";
import { schedulingApi } from "../api/schedulingApi";
import type { RegisterForShiftRequest } from "../api/schedulingApi";

/* ========= Date helpers - Asia/Jerusalem ========= */
function ilParts(d: Date) {
  const fmt = new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Jerusalem",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  });
  const [y, m, day] = fmt.format(d).split("-").map(Number);
  return { y, m, day };
}
function ilMidnightUTC(d = new Date()): Date {
  const { y, m, day } = ilParts(d);
  return new Date(Date.UTC(y, m - 1, day, 0, 0, 0, 0));
}
function addUTCDays(d: Date, n: number): Date {
  const x = new Date(d);
  x.setUTCDate(x.getUTCDate() + n);
  return x;
}
function ilStartOfSunday(d = new Date()): Date {
  const mid = ilMidnightUTC(d);
  const dow = mid.getUTCDay();
  return addUTCDays(mid, -dow);
}
function ilNextSunday(d = new Date()): Date {
  return addUTCDays(ilStartOfSunday(d), 7);
}
function ilISO(d: Date): string {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Jerusalem",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(d);
}
function ilLong(d: Date): string {
  return new Intl.DateTimeFormat("he-IL", {
    timeZone: "Asia/Jerusalem",
    weekday: "long",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(d);
}
function ilShort(d: Date): string {
  return new Intl.DateTimeFormat("he-IL", {
    timeZone: "Asia/Jerusalem",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(d);
}
function ilWeekdayShort(d: Date): string {
  return new Intl.DateTimeFormat("he-IL", {
    timeZone: "Asia/Jerusalem",
    weekday: "short",
  }).format(d);
}

/* ========= Types ========= */
type DayISO = string;
type ShiftChoice = "early" | "regular";
type DayStatus = "idle" | "pending" | "error";

export default function EmployeeSignupPage() {
  const { user } = useAuth();

  // שבוע הבא - ראשון עד חמישי
  const [weekStart] = useState<Date>(ilNextSunday());
  const days = useMemo(
    () => Array.from({ length: 5 }, (_, i) => addUTCDays(weekStart, i)),
    [weekStart]
  );

  // ISO של 5 הימים
  const isoDays = useMemo(() => days.map(d => ilISO(d)), [days]);

  // בחירת העובד לכל יום
  const [localState, setLocalState] = useState<Record<DayISO, ShiftChoice | null>>({});
  const selectedCount = Object.values(localState).filter(Boolean).length;

  // מיפוי תאריך → shiftId ראשון ליום
  const [shiftIdByIso, setShiftIdByIso] = useState<Record<string, string>>({});

  // דגלי מוכנות - **חשוב: רק אחרי ששניהם מוכנים נפתח כפתורים**
  const [shiftsLoaded, setShiftsLoaded] = useState(false);
  const [registrationsLoaded, setRegistrationsLoaded] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  // סטטוס פר יום וניהול שליחה
  const [dayStatus, setDayStatus] = useState<Record<DayISO, DayStatus>>({});
  const [submitting, setSubmitting] = useState(false);
  const [submitMsg, setSubmitMsg] = useState<string | null>(null);

  // האם הכל מוכן לפתיחת כפתורים
  const dataReady = shiftsLoaded && registrationsLoaded;

  // מאפס הכל כשמשנים שבוע
  useEffect(() => {
    setLocalState({});
    setDayStatus(Object.fromEntries(isoDays.map(iso => [iso, "pending"] as const)));
    setShiftsLoaded(false);
    setRegistrationsLoaded(false);
    setShiftIdByIso({});
    setLoadError(null);
  }, [weekStart, isoDays]);

  // שלב 1: טוען משמרות תחילה
  useEffect(() => {
    if (!user) return;

    let active = true;
    
    (async () => {
      try {
        setLoadError(null);

        const startIso = ilISO(weekStart);
        const endIso = ilISO(addUTCDays(weekStart, 5));

        const shifts = await schedulingApi.getShifts(startIso, endIso);
        if (!active) return;

        // בונה מיפוי יום → משמרת ראשונה
        const isoToFirstShiftId: Record<string, string> = {};
        
        for (const s of shifts) {
          if (!s.startTime || !s.id) continue;
          const d = new Date(s.startTime);
          const iso = ilISO(d);
          if (!isoToFirstShiftId[iso]) {
            isoToFirstShiftId[iso] = s.id;
          }
        }

        setShiftIdByIso(isoToFirstShiftId);
        setShiftsLoaded(true);
      } catch (error) {
        console.log(error)
        if (active) {
          setLoadError("שגיאה בטעינת משמרות");
          setShiftsLoaded(true); // גם במקרה שגיאה, נמשיך הלאה
        }
      }
    })();
    
    return () => { active = false; };
  }, [user, weekStart]);

  // שלב 2: אחרי שהמשמרות נטענו - טוען הרשמות
  useEffect(() => {
    if (!user || !shiftsLoaded) return;

    let active = true;

    (async () => {
      try {
        const startIso = ilISO(weekStart);
        const endIso = ilISO(addUTCDays(weekStart, 5));
        const regs = await schedulingApi.getMyRegistrations(startIso, endIso);

        if (!active) return;

        // מזהה ימים שכבר יש בהם הרשמה פעילה
        const lockedDays = new Set<string>();
        for (const r of regs) {
          if (r.status === 0 || r.status === 1) { // Pending או Approved
            if (r.shiftDate) {
              lockedDays.add(r.shiftDate);
            }
          }
        }

        // מעדכן סטטוס ימים
        setDayStatus(curr => {
          const next: Record<string, DayStatus> = { ...curr };
          for (const iso of isoDays) {
            next[iso] = lockedDays.has(iso) ? "pending" : "idle";
          }
          return next;
        });

        // מנקה בחירות של ימים נעולים
        setLocalState(prev => {
          const copy = { ...prev };
          for (const iso of lockedDays) {
            copy[iso] = null;
          }
          return copy;
        });

        setRegistrationsLoaded(true);
      } catch (error) {
        console.log(error)

        if (active) {
          // במקרה שגיאה - נשאיר הכל נעול
          setDayStatus(Object.fromEntries(isoDays.map(iso => [iso, "pending"] as const)));
          setRegistrationsLoaded(true);
        }
      }
    })();

    return () => { active = false; };
  }, [user, weekStart, shiftsLoaded, isoDays]);

  // דרפט בקשות לשליחה
  type DraftItem = { iso: string; payload: RegisterForShiftRequest };
  const draftRequests = useMemo<DraftItem[]>(() => {
    const out: DraftItem[] = [];
    for (const [iso, choice] of Object.entries(localState)) {
      if (!choice) continue;
      const shiftId = shiftIdByIso[iso];
      if (!shiftId) continue;
      out.push({
        iso,
        payload: {
          shiftId,
          shiftArrivalType: choice === "early" ? 2 : 1,
        },
      });
    }
    return out;
  }, [localState, shiftIdByIso]);

  function select(day: DayISO, type: ShiftChoice) {
    setLocalState(prev => ({
      ...prev,
      [day]: prev[day] === type ? null : type,
    }));
  }

  function resetSelections() {
    setLocalState({});
  }

  async function handleRegister() {
    if (draftRequests.length === 0) {
      setSubmitMsg("לא נבחרו משמרות");
      return;
    }

    setSubmitting(true);
    setSubmitMsg(null);

    try {
      const outcomes = await Promise.all(
        draftRequests.map(async ({ iso, payload }) => {
          try {
            const res = await schedulingApi.registerForShift(payload);
            setDayStatus(prev => ({ ...prev, [iso]: "pending" }));
            setLocalState(prev => ({ ...prev, [iso]: null }));
            return { ok: true, msg: res.message };
          } catch {
            setDayStatus(prev => ({ ...prev, [iso]: "error" }));
            return { ok: false, msg: "שגיאה בשליחת הבקשה למשמרת" };
          }
        })
      );

      const ok = outcomes.filter(o => o.ok).length;
      const fail = outcomes.length - ok;
      setSubmitMsg(`נשלחו ${ok} בקשות בהצלחה, ${fail} נכשלו`);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="mx-auto max-w-7xl p-6" dir="rtl">
      {/* Header */}
      <header className="mb-8">
        <div className="rounded-2xl bg-gradient-to-r from-sky-500/10 via-teal-500/10 to-emerald-500/10 border border-slate-200 p-6">
          <div className="flex flex-col items-center text-center gap-2">
            <h1 className="text-3xl font-extrabold tracking-tight text-slate-800">
              הרשמה למשמרות - עובדים
            </h1>
            <p className="text-slate-600">
              שלום <span className="font-semibold">{user?.email}</span>, ההרשמה מוצגת לשבוע הבא בלבד. בחר משמרת אחת לכל יום בין ראשון לחמישי.
            </p>
            <div className="mt-3 rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm text-slate-700">
              {ilLong(days[0])} - {ilLong(days[4])}
            </div>

            {!dataReady && (
              <div className="mt-3 text-xs text-slate-500">
                טוען נתונים... ({shiftsLoaded ? "✓" : "○"} משמרות, {registrationsLoaded ? "✓" : "○"} הרשמות)
              </div>
            )}
            
            {loadError && (
              <div className="mt-3 rounded-lg bg-red-50 border border-red-200 text-red-700 px-3 py-2 text-sm">
                {loadError}
              </div>
            )}

            {dataReady && (
              <div className="mt-2 text-xs text-slate-500">הנתונים נטענו בהצלחה ✓</div>
            )}

            <div className="mt-3 flex flex-wrap items-center justify-center gap-2">
              <span className="rounded-full bg-slate-100 px-3 py-1 text-xs text-slate-600">
                בחירות נוכחיות: {selectedCount}
              </span>
              <button
                onClick={resetSelections}
                className="rounded-full border border-slate-300 bg-white px-3 py-1 text-xs hover:bg-slate-50"
              >
                איפוס בחירות
              </button>
              <span className="text-xs text-slate-500">טיפ: לחיצה חוזרת על אותה משמרת תבטל את הבחירה ליום.</span>
            </div>
          </div>
        </div>
      </header>

      {/* Grid of days - Sunday on the right */}
      <section className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-5">
        {days.map((d) => {
          const iso = ilISO(d);
          const display = ilShort(d);
          const sel = localState[iso];
          const hasShiftId = Boolean(shiftIdByIso[iso]);
          const status = dayStatus[iso] ?? "pending";
          const locked = status === "pending";

          // **הכפתורים נפתחים רק אחרי ששני הdataReady הוא true**
          const buttonsEnabled = dataReady && hasShiftId && !locked;

          return (
            <article
              key={iso}
              className={`rounded-2xl border p-5 shadow-sm transition ${
                sel ? "border-emerald-300 ring-2 ring-emerald-200 bg-emerald-50/40" : "border-slate-200 bg-white hover:shadow-md"
              }`}
            >
              <header className="mb-4">
                <div className="flex items-center justify-between">
                  <span className="text-xs text-slate-500">{ilWeekdayShort(d)}</span>
                  {!hasShiftId && dataReady && (
                    <span className="text-[10px] text-red-500">אין משמרת ליום זה</span>
                  )}
                </div>
                <div className="mt-1 text-lg font-semibold text-slate-800">{display}</div>

                {status === "pending" && (
                  <div className="mt-2 inline-flex items-center gap-2 rounded-full border border-amber-300 bg-amber-50 px-2.5 py-1 text-xs text-amber-700">
                    כבר נרשמת - ממתין לאישור
                  </div>
                )}
                {status === "error" && (
                  <div className="mt-2 inline-flex items-center gap-2 rounded-full border border-red-300 bg-red-50 px-2.5 py-1 text-xs text-red-700">
                    שגיאה בשליחה - נסו שוב
                  </div>
                )}
              </header>

              <div className="flex flex-col gap-3">
                <button
                  disabled={!buttonsEnabled}
                  onClick={() => select(iso, "early")}
                  className={`group rounded-xl border px-4 py-3 text-sm font-medium transition flex items-center justify-between
                    ${sel === "early" ? "border-emerald-400 bg-white ring-2 ring-emerald-300" : "border-slate-300 bg-slate-50 hover:bg-white"}
                    ${!buttonsEnabled ? "opacity-50 cursor-not-allowed" : ""}`}
                >
                  <span>Early</span>
                  {sel === "early" ? <Check className="h-4 w-4" aria-hidden /> : <span className="text-[10px] text-slate-400">בחר</span>}
                </button>

                <button
                  disabled={!buttonsEnabled}
                  onClick={() => select(iso, "regular")}
                  className={`group rounded-xl border px-4 py-3 text-sm font-medium transition flex items-center justify-between
                    ${sel === "regular" ? "border-sky-400 bg-white ring-2 ring-sky-300" : "border-slate-300 bg-slate-50 hover:bg-white"}
                    ${!buttonsEnabled ? "opacity-50 cursor-not-allowed" : ""}`}
                >
                  <span>Regular</span>
                  {sel === "regular" ? <Check className="h-4 w-4" aria-hidden /> : <span className="text-[10px] text-slate-400">בחר</span>}
                </button>
              </div>
            </article>
          );
        })}
      </section>

      {/* פעולה בתחתית */}
      <div className="mt-8 flex items-center justify-end gap-2">
        <button
          className="rounded-xl border border-slate-300 bg-white px-4 py-2 text-sm hover:bg-slate-50"
          onClick={resetSelections}
        >
          איפוס
        </button>
        <button
          onClick={handleRegister}
          disabled={submitting || draftRequests.length === 0}
          className={`rounded-xl px-5 py-2 text-sm text-white ${
            submitting || draftRequests.length === 0 ? "bg-slate-400 cursor-not-allowed" : "bg-slate-900 hover:bg-slate-800"
          }`}
        >
          {submitting ? "שולח..." : "הרשמה"}
        </button>
      </div>

      {/* פידבק לשליחה */}
      {submitMsg && (
        <div className="mt-3 rounded-lg border px-3 py-2 text-sm border-slate-200 bg-slate-50 text-slate-700">
          {submitMsg}
        </div>
      )}
    </div>
  );
}