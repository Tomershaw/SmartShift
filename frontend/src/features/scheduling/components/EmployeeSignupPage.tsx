import { useEffect, useMemo, useState } from "react";
import { Check } from "lucide-react";
import { useAuth } from "../../auth/context/useAuth";
import { schedulingApi } from "../api/schedulingApi";
import type { RegisterForShiftRequest } from "../api/schedulingApi";
import { useLoading } from "../../appLoading/context/useLoading";

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
  const dow = mid.getUTCDay(); // 0=Sunday
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
  return new Intl.DateTimeFormat("en-CA", {
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
function ilDateTime(d: Date): string {
  return new Intl.DateTimeFormat("he-IL", {
    timeZone: "Asia/Jerusalem",
    dateStyle: "medium",
    timeStyle: "short",
  }).format(d);
}

// חלון ייצור: פתיחה רביעי 00:00, סגירה שישי 12:00 לפי Asia/Jerusalem
function getSubmissionWindow(now = new Date()) {
  const sunday = ilStartOfSunday(now); // ראשון של השבוע הנוכחי
  const wednesday = addUTCDays(sunday, 3); // רביעי 00:00
  const friday = addUTCDays(sunday, 5); // שישי 00:00

  const start = new Date(wednesday);
  start.setUTCHours(0, 0, 0, 0);

  const end = new Date(friday);
  end.setUTCHours(12, 0, 0, 0); // סגירה ב-12:00
  return { start, end };
}

/* ========= Countdown helpers ========= */
function formatCountdown(ms: number) {
  if (ms <= 0) return "00:00:00";
  const secs = Math.floor(ms / 1000);
  const days = Math.floor(secs / 86400);
  const hh = Math.floor((secs % 86400) / 3600)
    .toString()
    .padStart(2, "0");
  const mm = Math.floor((secs % 3600) / 60)
    .toString()
    .padStart(2, "0");
  const ss = Math.floor(secs % 60)
    .toString()
    .padStart(2, "0");
  return days > 0 ? `${days} ימים ${hh}:${mm}:${ss}` : `${hh}:${mm}:${ss}`;
}

/** offset שעות בפועל ל־Asia/Jerusalem עבור היום (2 או 3 בד"כ) */
function ilOffsetHoursForDate(y: number, m: number, day: number): number {
  const utcMidnight = new Date(Date.UTC(y, m - 1, day, 0, 0, 0, 0));
  const hourStr = new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Jerusalem",
    hour: "2-digit",
    hour12: false,
  }).format(utcMidnight);
  return Number(hourStr);
}

/** "היום ב־X:00" לפי ישראל - כולל DST נכון */
function ilTodayAtHourIL(hour: number, now = new Date()): Date {
  const { y, m, day } = ilParts(now); // היום לפי IL
  const offset = ilOffsetHoursForDate(y, m, day);
  return new Date(Date.UTC(y, m - 1, day, hour - offset, 0, 0, 0)); // UTC = IL - offset
}

/* ========= Types ========= */
type DayISO = string;
type ShiftChoice = "early" | "regular";
type DayStatus = "idle" | "pending" | "error" | "loading";

export default function EmployeeSignupPage() {
  const { user, logout } = useAuth();
  const { show, hide } = useLoading();

  // שבוע הבא - ראשון עד חמישי
  const [weekStart] = useState<Date>(ilNextSunday);
  const days = useMemo(
    () => Array.from({ length: 5 }, (_, i) => addUTCDays(weekStart, i)),
    [weekStart]
  );
  const isoDays = useMemo(() => days.map(d => ilISO(d)), [days]);

  // בחירות ליום
  const [localState, setLocalState] = useState<
    Record<DayISO, ShiftChoice | null>
  >({});

  // מיפוי iso -> shiftId
  const [shiftIdByIso, setShiftIdByIso] = useState<Record<string, string>>({});

  // דגלי טעינה
  const [shiftsLoaded, setShiftsLoaded] = useState(false);
  const [registrationsLoaded, setRegistrationsLoaded] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  // סטטוס פר יום
  const [dayStatus, setDayStatus] = useState<Record<DayISO, DayStatus>>({});

  // סטטוס אישי שלי לכל יום: 0 Pending, 1 Approved, 2 Rejected, 3 Cancelled
  const [myStatusByIso, setMyStatusByIso] = useState<
    Record<string, 0 | 1 | 2 | 3>
  >({});

  // שליחה
  const [submitting, setSubmitting] = useState(false);
  const [submitMsg, setSubmitMsg] = useState<string | null>(null);

  const dataReady = shiftsLoaded && registrationsLoaded;

  /* ===== Countdown state ===== */
  const [now, setNow] = useState<Date>(new Date());
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(id);
  }, []);

  const TEST_MODE = true;
  const { start: submitStart, end: submitEnd } = useMemo(() => {
    if (TEST_MODE) {
      const end = ilTodayAtHourIL(20, now);
      const start = new Date(now.getTime() - 60_000);
      return { start, end };
    }
    return getSubmissionWindow(now);
  }, [now, TEST_MODE]);

  const beforeWindow = now < submitStart;
  const withinWindow = now >= submitStart && now < submitEnd;
  const afterWindow = now >= submitEnd;
  const countdownTarget = beforeWindow ? submitStart : submitEnd;
  const countdownText = formatCountdown(
    countdownTarget.getTime() - now.getTime()
  );

  /* ===== Loader control ===== */
  useEffect(() => {
    if (!dataReady) show("טוען משמרות והרשמות...");
    else hide();
    return () => hide();
  }, [dataReady, show, hide]);

  /* ===== Reset when week changes ===== */
  useEffect(() => {
    setLocalState({});
    setDayStatus(
      Object.fromEntries(isoDays.map(iso => [iso, "loading"] as const))
    );
    setShiftsLoaded(false);
    setRegistrationsLoaded(false);
    setShiftIdByIso({});
    setMyStatusByIso({});
    setLoadError(null);
  }, [weekStart, isoDays]);

  /* ===== Load shifts ===== */
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

        const map: Record<string, string> = {};
        for (const s of shifts) {
          if (!s.startTime || !s.id) continue;
          const iso = ilISO(new Date(s.startTime));
          if (!map[iso]) map[iso] = s.id;
        }
        setShiftIdByIso(map);
        setShiftsLoaded(true);
      } catch {
        if (active) {
          setLoadError("שגיאה בטעינת משמרות");
          setShiftsLoaded(true);
        }
      }
    })();
    return () => {
      active = false;
    };
  }, [user, weekStart]);

  /* ===== Load my registrations and set locks/status ===== */
  useEffect(() => {
    if (!user || !shiftsLoaded) return;
    let active = true;

    (async () => {
      try {
        const startIso = ilISO(weekStart);
        const endIso = ilISO(addUTCDays(weekStart, 5));

        const regs = await schedulingApi.getMyRegistrations(startIso, endIso);
        if (!active) return;

        const statusMap: Record<string, 0 | 1 | 2 | 3> = {};
        const locked = new Set<string>();

        for (const r of regs) {
          if (r.shiftDate) {
            statusMap[r.shiftDate] = r.status;
            if (r.status === 0 || r.status === 1) locked.add(r.shiftDate); // Pending/Approved נועל
          }
        }

        setMyStatusByIso(statusMap);

        // עדכן כל יום ל-idle אלא אם נעול
        const next: Record<string, DayStatus> = {};
        for (const iso of isoDays)
          next[iso] = locked.has(iso) ? "pending" : "idle";
        setDayStatus(next);

        // נקה בחירה מקומית לימים שננעלו
        setLocalState(prev => {
          const copy = { ...prev };
          for (const iso of locked) copy[iso] = null;
          return copy;
        });
      } catch (err) {
        console.error("getMyRegistrations failed", err);
        if (active) {
          // אל תחזיר ל-"loading" - זה נתקע. סמן "error" כדי לאפשר כפתורים.
          const errState = Object.fromEntries(
            isoDays.map(iso => [iso, "error"] as const)
          );
          setDayStatus(errState);
          setSubmitMsg("שגיאה בטעינת ההרשמות");
        }
      } finally {
        if (active) setRegistrationsLoaded(true);
      }
    })();

    return () => {
      active = false;
    };
  }, [user, weekStart, shiftsLoaded, isoDays]);

  /* ===== Draft requests ===== */
  type DraftItem = { iso: string; payload: RegisterForShiftRequest };
  const draftRequests = useMemo<DraftItem[]>(() => {
    const drafts: DraftItem[] = [];
    for (const [iso, choice] of Object.entries(localState)) {
      if (!choice) continue;
      const shiftId = shiftIdByIso[iso];
      if (!shiftId) continue;
      drafts.push({
        iso,
        payload: { shiftId, shiftArrivalType: choice === "early" ? 2 : 1 },
      });
    }
    return drafts;
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
    if (afterWindow || beforeWindow) {
      setSubmitMsg(beforeWindow ? "ההרשמה עוד לא נפתחה" : "ההרשמה נסגרה");
      return;
    }
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
            setDayStatus(p => ({ ...p, [iso]: "pending" }));
            setMyStatusByIso(p => ({ ...p, [iso]: 0 })); // Pending
            setLocalState(p => ({ ...p, [iso]: null }));
            return { ok: true, msg: res.message };
          } catch {
            setDayStatus(p => ({ ...p, [iso]: "error" }));
            return { ok: false };
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

  /* ===== Cancel my registration for a day ===== */
  async function handleCancel(iso: string) {
    const shiftId = shiftIdByIso[iso];
    if (!shiftId) return;

    setDayStatus(p => ({ ...p, [iso]: "loading" }));
    try {
      const res = await schedulingApi.cancelMyRegistration(shiftId);
      if (res?.success) {
        // שחרר לבחירה מחדש
        setDayStatus(p => ({ ...p, [iso]: "idle" }));
        setMyStatusByIso(p => ({ ...p, [iso]: 3 })); // Cancelled
        setSubmitMsg("ההרשמה בוטלה");
      } else {
        setDayStatus(p => ({ ...p, [iso]: "error" }));
        setSubmitMsg("לא ניתן לבטל (לא נמצא או לא Pending)");
      }
    } catch {
      setDayStatus(p => ({ ...p, [iso]: "error" }));
      setSubmitMsg("שגיאה בביטול");
    }
  }

  /* ========= UI ========= */
  return (
    <div
      className="mx-auto max-w-7xl p-6 transition-opacity duration-200"
      dir="rtl"
    >
      {/* Header */}
      <header className="mb-6">
        <div className="flex items-center justify-between rounded-2xl bg-gradient-to-r from-sky-500/10 via-teal-500/10 to-emerald-500/10 border border-slate-200 p-6">
          {/* כותרת + פרטים במרכז */}
          <div className="flex flex-col items-center text-center gap-2 mx-auto">
            <h1 className="text-3xl font-extrabold tracking-tight text-slate-800">
              הרשמה למשמרות - עובדים
            </h1>
            <p className="text-slate-600">
              שלום <span className="font-semibold">{user?.email}</span>, ההרשמה
              מוצגת לשבוע הבא בלבד
            </p>
            <div className="mt-2 rounded-xl border border-slate-200 bg-white px-4 py-1.5 text-sm text-slate-700">
              {ilLong(days[0])} - {ilLong(days[4])}
            </div>

            {/* טיימר חלון הגשה */}
            <div
              className={`mt-2 px-3 py-1.5 rounded-full text-xs sm:text-sm border
                ${
                  withinWindow
                    ? "border-emerald-300 bg-emerald-50 text-emerald-800"
                    : beforeWindow
                    ? "border-sky-300 bg-sky-50 text-sky-800"
                    : "border-rose-300 bg-rose-50 text-rose-800"
                }`}
              title={`סגירה: ${ilDateTime(submitEnd)}`}
            >
              {beforeWindow && (
                <>
                  ההרשמה תיפתח בעוד:{" "}
                  <span className="font-semibold">{countdownText}</span> •
                  פתיחה: {ilDateTime(submitStart)}
                </>
              )}
              {withinWindow && (
                <>
                  נשאר זמן להגשה:{" "}
                  <span className="font-semibold">{countdownText}</span> •
                  סגירה: {ilDateTime(submitEnd)}
                </>
              )}
              {afterWindow && (
                <>ההרשמה נסגרה • סגירה: {ilDateTime(submitEnd)}</>
              )}
            </div>

            {!!loadError && (
              <div className="mt-3 rounded-lg bg-red-50 border border-red-200 text-red-700 px-3 py-2 text-sm">
                {loadError}
              </div>
            )}
          </div>
        </div>
      </header>

      {/* Grid of days */}
      <section className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-5">
        {days.map(d => {
          const iso = ilISO(d);
          const display = ilShort(d);
          const sel = localState[iso];
          const hasShiftId = Boolean(shiftIdByIso[iso]);

          const status = dayStatus[iso] ?? "loading"; // "idle" | "pending" | "error" | "loading"
          const myStatus = myStatusByIso[iso]; // 0..3

          // בוליאנים ברורים - פותרים TS2367
          const isPending = status === "pending";
          const isLoading = status === "loading";
          const isIdleOrError = status === "idle" || status === "error";
          const canCancel = isPending && myStatus === 0;

          // כפתורי בחירה פעילים רק כשיש משמרת, היום פנוי להצגה, ואנחנו בחלון
          const buttonsEnabled = hasShiftId && isIdleOrError && withinWindow;
          console.log({
            iso,
            dayStatus: dayStatus[iso],
            myStatus: myStatusByIso[iso],
            withinWindow,
            hasShiftId: !!shiftIdByIso[iso],
          });

          return (
            <article
              key={iso}
              className={`rounded-2xl border p-5 shadow-sm transition ${
                sel
                  ? "border-emerald-300 ring-2 ring-emerald-200 bg-emerald-50/40"
                  : "border-slate-200 bg-white hover:shadow-md"
              }`}
            >
              <header className="mb-4">
                <div className="flex items-center justify-between">
                  <span className="text-xs text-slate-500">
                    {ilWeekdayShort(d)}
                  </span>
                  {!hasShiftId && (
                    <span className="text-[10px] text-red-500">
                      אין משמרת ליום זה
                    </span>
                  )}
                </div>
                <div className="mt-1 text-lg font-semibold text-slate-800">
                  {display}
                </div>

                {canCancel && (
                  <div className="mt-2 flex items-center gap-2">
                    <div className="inline-flex items-center gap-2 rounded-full border border-amber-300 bg-amber-50 px-2.5 py-1 text-xs text-amber-700">
                      כבר נרשמת - ממתין לאישור
                    </div>
                    <button
                      onClick={() => handleCancel(iso)}
                      className="px-2.5 py-1 rounded-lg bg-red-600 text-white text-xs hover:opacity-90 disabled:opacity-50"
                      aria-label="בטל הרשמה"
                      title="בטל הרשמה"
                      disabled={isLoading}
                    >
                      בטל הרשמה
                    </button>
                  </div>
                )}

                {isPending && myStatus === 1 && (
                  <div className="mt-2 inline-flex items-center gap-2 rounded-full border border-emerald-300 bg-emerald-50 px-2.5 py-1 text-xs text-emerald-700">
                    נרשמת - אושר
                  </div>
                )}

                {status === "error" && (
                  <div className="mt-2 inline-flex items-center gap-2 rounded-full border border-red-300 bg-red-50 px-2.5 py-1 text-xs text-red-700">
                    שגיאה בשליחה - נסה שוב
                  </div>
                )}
                {!withinWindow && !afterWindow && (
                  <div className="mt-2 text-[11px] text-slate-500">
                    ההרשמה תיפתח בקרוב
                  </div>
                )}
                {afterWindow && (
                  <div className="mt-2 text-[11px] text-slate-500">
                    ההרשמה נסגרה לשבוע זה
                  </div>
                )}

                {isLoading && (
                  <div className="mt-2 text-[11px] text-slate-500">טוען...</div>
                )}
              </header>

              <div className="flex flex-col gap-3">
                <button
                  onClick={() => select(iso, "early")}
                  disabled={!buttonsEnabled}
                  className={`group rounded-xl border px-4 py-3 text-sm font-medium transition flex items-center justify-between
                    ${
                      sel === "early"
                        ? "border-emerald-400 bg-white ring-2 ring-emerald-300"
                        : "border-slate-300 bg-slate-50 hover:bg-white"
                    }
                    ${!buttonsEnabled ? "opacity-50 cursor-not-allowed" : ""}`}
                >
                  <span>Early</span>
                  {sel === "early" ? (
                    <Check className="h-4 w-4" aria-hidden />
                  ) : (
                    <span className="text-[10px] text-slate-400">בחר</span>
                  )}
                </button>

                <button
                  onClick={() => select(iso, "regular")}
                  disabled={!buttonsEnabled}
                  className={`group rounded-xl border px-4 py-3 text-sm font-medium transition flex items-center justify-between
                    ${
                      sel === "regular"
                        ? "border-sky-400 bg-white ring-2 ring-sky-300"
                        : "border-slate-300 bg-slate-50 hover:bg-white"
                    }
                    ${!buttonsEnabled ? "opacity-50 cursor-not-allowed" : ""}`}
                >
                  <span>Regular</span>
                  {sel === "regular" ? (
                    <Check className="h-4 w-4" aria-hidden />
                  ) : (
                    <span className="text-[10px] text-slate-400">בחר</span>
                  )}
                </button>
              </div>
            </article>
          );
        })}
      </section>

      {/* פעולה בתחתית - הרשמה, איפוס, התנתק יחד */}
      <div className="mt-8 flex itemsקרים justify-end gap-2">
        <button
          className="rounded-xl border border-slate-300 bg-white px-4 py-2 text-sm hover:bg-slate-50"
          onClick={resetSelections}
        >
          איפוס
        </button>
        <button
          onClick={handleRegister}
          disabled={submitting || draftRequests.length === 0 || !withinWindow}
          className={`rounded-xl px-5 py-2 text-sm text-white ${
            submitting || draftRequests.length === 0 || !withinWindow
              ? "bg-slate-400 cursor-not-allowed"
              : "bg-slate-900 hover:bg-slate-800"
          }`}
          aria-label="שליחת הרשמות לשבוע הנבחר"
        >
          {submitting ? "שולח..." : "הרשמה"}
        </button>
        <button
          onClick={logout}
          className="inline-flex items-center gap-2 rounded-xl border border-red-200 bg-red-50 px-4 py-2 text-sm font-medium
               text-red-800 hover:bg-red-100 hover:border-red-300 shadow-sm transition
               focus:outline-none focus:ring-2 focus:ring-red-300"
          aria-label="התנתק"
        >
          <svg
            width="16"
            height="16"
            viewBox="0 0 24 24"
            className="opacity-80"
          >
            <path
              fill="currentColor"
              d="M16 17v-2H8V9h8V7l4 4l-4 4ZM4 5h8V3H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h8v-2H4V5Z"
            />
          </svg>
          התנתק
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
