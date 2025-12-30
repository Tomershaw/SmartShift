import { useEffect, useMemo, useState } from "react";
import {
  Check,
  Clock,
  AlertCircle,
  LogOut,
  RefreshCcw,
  Send,
  X,
  CalendarDays, // וידאתי שזה בשימוש או נמחק אם לא
} from "lucide-react";
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
  }).format(d);
}
function ilMonthName(d: Date): string {
  return new Intl.DateTimeFormat("he-IL", {
    timeZone: "Asia/Jerusalem",
    month: "long",
  }).format(d);
}
function ilDayNumber(d: Date): string {
  return new Intl.DateTimeFormat("he-IL", {
    timeZone: "Asia/Jerusalem",
    day: "numeric",
  }).format(d);
}
function ilDateTime(d: Date): string {
  return new Intl.DateTimeFormat("he-IL", {
    timeZone: "Asia/Jerusalem",
    dateStyle: "medium",
    timeStyle: "short",
  }).format(d);
}

// חלון ייצור
function getSubmissionWindow(now = new Date()) {
  const sunday = ilStartOfSunday(now);
  const wednesday = addUTCDays(sunday, 3);
  const friday = addUTCDays(sunday, 5);

  const start = new Date(wednesday);
  start.setUTCHours(0, 0, 0, 0);

  const end = new Date(friday);
  end.setUTCHours(12, 0, 0, 0);
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

function ilOffsetHoursForDate(y: number, m: number, day: number): number {
  const utcMidnight = new Date(Date.UTC(y, m - 1, day, 0, 0, 0, 0));
  const hourStr = new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Jerusalem",
    hour: "2-digit",
    hour12: false,
  }).format(utcMidnight);
  return Number(hourStr);
}

function ilTodayAtHourIL(hour: number, now = new Date()): Date {
  const { y, m, day } = ilParts(now);
  const offset = ilOffsetHoursForDate(y, m, day);
  return new Date(Date.UTC(y, m - 1, day, hour - offset, 0, 0, 0));
}

/* ========= Types ========= */
type DayISO = string;
type ShiftChoice = "early" | "regular";
type DayStatus = "idle" | "pending" | "error" | "loading";

/* ========= Main Component ========= */
export default function EmployeeSignupPage() {
  const { user, logout } = useAuth();
  const { show, hide } = useLoading();

  // State setup
  const [weekStart] = useState<Date>(ilNextSunday);
  const days = useMemo(
    () => Array.from({ length: 5 }, (_, i) => addUTCDays(weekStart, i)),
    [weekStart]
  );
  const isoDays = useMemo(() => days.map(d => ilISO(d)), [days]);

  const [localState, setLocalState] = useState<
    Record<DayISO, ShiftChoice | null>
  >({});
  const [shiftIdByIso, setShiftIdByIso] = useState<Record<string, string>>({});
  const [shiftsLoaded, setShiftsLoaded] = useState(false);
  const [registrationsLoaded, setRegistrationsLoaded] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [dayStatus, setDayStatus] = useState<Record<DayISO, DayStatus>>({});
  const [myStatusByIso, setMyStatusByIso] = useState<
    Record<string, 0 | 1 | 2 | 3>
  >({});
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
      const end = ilTodayAtHourIL(23, now);
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

  /* ===== Effects ===== */
  useEffect(() => {
    if (!dataReady) show("טוען משמרות והרשמות...");
    else hide();
    return () => hide();
  }, [dataReady, show, hide]);

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

  // Load Shifts
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

  // Load Registrations
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
            if (r.status === 0 || r.status === 1) locked.add(r.shiftDate);
          }
        }

        setMyStatusByIso(statusMap);
        const next: Record<string, DayStatus> = {};
        for (const iso of isoDays)
          next[iso] = locked.has(iso) ? "pending" : "idle";
        setDayStatus(next);

        setLocalState(prev => {
          const copy = { ...prev };
          for (const iso of locked) copy[iso] = null;
          return copy;
        });
      } catch (err) {
        // תיקון שגיאת unused var
        console.error("Error loading registrations:", err);
        if (active) {
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

  /* ===== Actions ===== */
  const draftRequests = useMemo(() => {
    const drafts: { iso: string; payload: RegisterForShiftRequest }[] = [];
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
    setSubmitMsg(null);
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
            setMyStatusByIso(p => ({ ...p, [iso]: 0 }));
            setLocalState(p => ({ ...p, [iso]: null }));
            return { ok: true, msg: res.message };
          } catch {
            setDayStatus(p => ({ ...p, [iso]: "error" }));
            return { ok: false };
          }
        })
      );
      const ok = outcomes.filter(o => o.ok).length;
      setSubmitMsg(`נשלחו ${ok} בקשות בהצלחה`);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleCancel(iso: string) {
    const shiftId = shiftIdByIso[iso];
    if (!shiftId) return;

    setDayStatus(p => ({ ...p, [iso]: "loading" }));
    try {
      const res = await schedulingApi.cancelMyRegistration(shiftId);
      if (res?.success) {
        // כאן הקסם: החזרתי את הסטטוס ל-idle (פנוי) במקום להשאיר Cancelled
        // זה גורם לכרטיס להיראות נקי ומוכן לבחירה מחדש
        setDayStatus(p => ({ ...p, [iso]: "idle" }));
        setMyStatusByIso(p => {
          const next = { ...p };
          delete next[iso]; // מסיר את הסטטוס לגמרי
          return next;
        });
        setSubmitMsg("ההרשמה בוטלה - ניתן לבחור מחדש");
      } else {
        setDayStatus(p => ({ ...p, [iso]: "error" }));
        setSubmitMsg("לא ניתן לבטל");
      }
    } catch {
      setDayStatus(p => ({ ...p, [iso]: "error" }));
      setSubmitMsg("שגיאה בביטול");
    }
  }

  // שם משתמש לתצוגה - תיקון שגיאת ה-firstName
  // אנחנו לוקחים את החלק שלפני ה-@ באימייל אם אין שם
  const displayName = user?.email?.split("@")[0] || "אורח";

  /* ========= UI Render ========= */
  return (
    <div className="min-h-screen bg-slate-50/50 pb-28" dir="rtl">
      <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
        {/* Header Hero */}
        <header className="mb-8">
          <div className="relative overflow-hidden rounded-3xl bg-slate-900 px-6 py-8 shadow-2xl sm:px-10 sm:py-10">
            {/* Background Pattern */}
            <div className="absolute inset-0 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-20"></div>
            <div className="absolute top-0 right-0 h-[300px] w-[300px] bg-gradient-to-br from-blue-500/30 to-purple-500/30 blur-[100px] rounded-full pointer-events-none" />

            <div className="relative z-10 flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
              <div className="flex-1">
                <div className="flex items-center gap-2 text-slate-400 mb-2 text-sm font-medium">
                  <CalendarDays className="w-4 h-4" />
                  <span>שבוע הבא</span>
                </div>
                <h1 className="text-3xl sm:text-4xl font-black tracking-tight text-white mb-2">
                  {ilLong(days[0])} - {ilLong(days[4])}
                </h1>
                <p className="text-slate-300">
                  שלום{" "}
                  <span className="text-white font-bold">{displayName}</span>,
                  אנא בחר את המשמרות שלך.
                </p>
              </div>

              {/* Timer Card */}
              <div
                className={`
                w-full md:w-auto flex flex-col items-center justify-center rounded-2xl border px-6 py-3 backdrop-blur-md transition-all
                ${
                  withinWindow
                    ? "bg-emerald-500/10 border-emerald-500/30 text-emerald-50 shadow-[0_0_20px_rgba(16,185,129,0.1)]"
                    : beforeWindow
                    ? "bg-blue-500/10 border-blue-500/30 text-blue-50"
                    : "bg-rose-500/10 border-rose-500/30 text-rose-50"
                }
              `}
              >
                <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider opacity-80 mb-1">
                  <Clock size={14} />
                  {beforeWindow
                    ? "ההרשמה נפתחת בעוד"
                    : withinWindow
                    ? "זמן שנותר להרשמה"
                    : "סטטוס הרשמה"}
                </div>
                <div className="text-2xl font-mono font-bold tracking-wider tabular-nums">
                  {afterWindow ? "סגור" : countdownText}
                </div>
                <div className="text-[10px] mt-1 opacity-60">
                  {beforeWindow
                    ? `פתיחה ב: ${ilDateTime(submitStart)}`
                    : `סגירה ב: ${ilDateTime(submitEnd)}`}
                </div>
              </div>
            </div>
          </div>
        </header>

        {/* Error Message */}
        {loadError && (
          <div className="mb-6 rounded-xl border border-red-200 bg-red-50 p-4 text-red-800 flex items-center gap-3 animate-in slide-in-from-top-2">
            <AlertCircle className="h-5 w-5" />
            {loadError}
          </div>
        )}

        {/* Days Grid */}
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
          {days.map(d => {
            const iso = ilISO(d);
            return (
              <DayCard
                key={iso}
                date={d}
                iso={iso}
                selection={localState[iso]}
                hasShift={!!shiftIdByIso[iso]}
                status={dayStatus[iso] ?? "loading"}
                myStatus={myStatusByIso[iso]}
                withinWindow={withinWindow}
                gender={user?.gender}
                onSelect={select}
                onCancel={handleCancel}
              />
            );
          })}
        </div>

        {/* Floating Action Bar - הלבן והעדין */}
        <div className="fixed bottom-6 left-4 right-4 z-50 mx-auto max-w-4xl">
          <div className="flex items-center justify-between gap-4 rounded-2xl border border-slate-200/50 bg-white/90 p-3 pl-4 text-slate-800 shadow-lg backdrop-blur-xl transition-all">
            {/* Status / Message */}
            <div className="flex-1 flex items-center gap-3 min-w-0">
              {submitMsg ? (
                <span
                  className={`flex items-center gap-2 text-sm font-medium animate-in slide-in-from-bottom-2 ${
                    submitMsg.includes("הצלחה")
                      ? "text-emerald-600"
                      : "text-slate-600"
                  }`}
                >
                  {submitMsg.includes("הצלחה") ? (
                    <Check size={16} />
                  ) : (
                    <AlertCircle size={16} />
                  )}
                  <span className="truncate">{submitMsg}</span>
                </span>
              ) : (
                <span className="text-sm text-slate-500 truncate">
                  {draftRequests.length > 0
                    ? `נבחרו ${draftRequests.length} משמרות לשליחה`
                    : "בחר משמרות כדי להירשם"}
                </span>
              )}
            </div>

            {/* Actions */}
            <div className="flex items-center gap-2">
              {draftRequests.length > 0 && (
                <button
                  onClick={resetSelections}
                  className="p-2.5 rounded-xl text-slate-500 hover:bg-slate-100 hover:text-slate-800 transition"
                  title="נקה בחירה"
                >
                  <RefreshCcw size={18} />
                </button>
              )}

              <button
                onClick={handleRegister}
                disabled={
                  submitting || draftRequests.length === 0 || !withinWindow
                }
                className={`
                    flex items-center gap-2 rounded-xl px-5 py-2.5 text-sm font-bold shadow-md transition-all active:scale-95 text-white
                    ${
                      submitting || draftRequests.length === 0 || !withinWindow
                        ? "bg-slate-300 cursor-not-allowed shadow-none"
                        : "bg-blue-600 hover:bg-blue-700 shadow-blue-500/20"
                    }
                  `}
              >
                {submitting ? "שולח..." : "שלח הרשמה"}
                {!submitting && <Send size={16} />}
              </button>

              <div className="h-6 w-px bg-slate-200 mx-1"></div>

              <button
                onClick={logout}
                className="p-2.5 rounded-xl text-rose-500 hover:bg-rose-50 transition"
                title="התנתק"
              >
                <LogOut size={18} />
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

/* ========= Sub-Component: Day Card ========= */
function DayCard({
  date,
  iso,
  selection,
  hasShift,
  status,
  myStatus,
  withinWindow,
  gender,
  onSelect,
  onCancel,
}: {
  date: Date;
  iso: string;
  selection: ShiftChoice | null | undefined;
  hasShift: boolean;
  status: DayStatus;
  myStatus: 0 | 1 | 2 | 3 | undefined;
  withinWindow: boolean;
  gender: string | undefined;
  onSelect: (iso: string, type: ShiftChoice) => void;
  onCancel: (iso: string) => void;
}) {
  const isPending = status === "pending";
  const isLoading = status === "loading";
  const isIdleOrError = status === "idle" || status === "error";

  const canCancel = isPending && myStatus === 0;
  const isApproved = isPending && myStatus === 1;
  const isRejected = isPending && myStatus === 2;
  // const isCancelled = myStatus === 3; // הוסר כי אנחנו מאפסים את המצב בביטול

  const buttonsEnabled = hasShift && isIdleOrError && withinWindow;
  const isActiveSelection = !!selection;

  return (
    <div
      className={`
      relative flex flex-col h-full rounded-2xl bg-white transition-all duration-300 overflow-hidden
      ${
        isActiveSelection
          ? "ring-2 ring-blue-500 shadow-xl scale-[1.02] z-10"
          : "border border-slate-200 shadow-sm hover:shadow-md hover:border-slate-300"
      }
      ${!hasShift ? "opacity-60 bg-slate-50 grayscale" : ""}
    `}
    >
      {/* Top Banner Status */}
      <div className="flex items-center justify-between px-4 pt-4">
        <span className="text-xs font-semibold text-slate-400 tracking-wide">
          {ilLong(date)}
        </span>

        {/* Badges - הסרתי את ה"בוטל" מכאן בכוונה */}
        {canCancel && (
          <StatusBadge
            color="bg-amber-100 text-amber-700"
            icon={<Clock size={10} />}
            text="ממתין"
          />
        )}
        {isApproved && (
          <StatusBadge
            color="bg-emerald-100 text-emerald-700"
            icon={<Check size={10} />}
            text="אושר"
          />
        )}
        {isRejected && (
          <StatusBadge
            color="bg-rose-100 text-rose-700"
            icon={<X size={10} />}
            text="נדחה"
          />
        )}
      </div>

      {/* Date Display - Hero Section */}
      <div className="flex flex-col items-center justify-center py-6">
        <div className="text-5xl font-black text-transparent bg-clip-text bg-gradient-to-br from-slate-800 to-slate-500 tracking-tight leading-none">
          {ilDayNumber(date)}
        </div>
        <div className="text-sm font-medium text-slate-500 mt-1">
          {ilMonthName(date)}
        </div>
      </div>

      {/* Error / Loading States */}
      {isLoading && (
        <div className="text-center pb-4 text-xs text-blue-500 animate-pulse font-medium">
          טוען נתונים...
        </div>
      )}
      {status === "error" && (
        <div className="text-center pb-4 text-xs text-rose-500 font-medium">
          שגיאה
        </div>
      )}
      {!hasShift && (
        <div className="text-center pb-4 text-xs text-slate-400">אין משמרת</div>
      )}

      {/* Actions Area */}
      <div className="mt-auto bg-slate-50/50 border-t border-slate-100 p-3 flex flex-col gap-2">
        {/* Selection Buttons */}
        {hasShift && !isPending && !isApproved && (
          <div className="grid grid-cols-1 gap-2">
            {gender !== "Female" && (
              <SelectionButton
                active={selection === "early"}
                disabled={!buttonsEnabled}
                onClick={() => onSelect(iso, "early")}
                label="הגעה מוקדמת"
              />
            )}
            <SelectionButton
              active={selection === "regular"}
              disabled={!buttonsEnabled}
              onClick={() => onSelect(iso, "regular")}
              label="רגיל"
            />
          </div>
        )}

        {/* Cancel Action */}
        {canCancel && (
          <button
            onClick={() => onCancel(iso)}
            className="w-full flex items-center justify-center gap-1.5 rounded-xl border border-red-100 bg-white py-2.5 text-xs font-bold text-red-600 hover:bg-red-50 hover:border-red-200 transition shadow-sm"
          >
            <X size={14} />
            בטל בקשה
          </button>
        )}

        {/* Closed State */}
        {!withinWindow && hasShift && !isPending && (
          <div className="flex items-center justify-center gap-1.5 py-2 text-xs text-slate-400 bg-slate-100 rounded-lg">
            <Clock size={12} />
            הרשמה סגורה
          </div>
        )}
      </div>
    </div>
  );
}

// Helper Components for Cleaner JSX
function StatusBadge({
  color,
  icon,
  text,
}: {
  color: string;
  icon: React.ReactNode;
  text: string;
}) {
  return (
    <span
      className={`flex items-center gap-1 text-[10px] font-bold px-2 py-0.5 rounded-full ${color}`}
    >
      {icon} {text}
    </span>
  );
}

function SelectionButton({
  active,
  disabled,
  onClick,
  label,
}: {
  active: boolean;
  disabled: boolean;
  onClick: () => void;
  label: string;
}) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={`
        relative w-full rounded-xl border px-3 py-2 text-sm font-medium transition-all duration-200 flex items-center justify-between
        ${
          active
            ? "bg-blue-600 border-blue-600 text-white shadow-md shadow-blue-500/20"
            : "bg-white border-slate-200 text-slate-600 hover:border-blue-300 hover:bg-blue-50/50"
        }
        ${
          disabled
            ? "cursor-not-allowed opacity-50 bg-slate-100 hover:bg-slate-100 hover:border-slate-200"
            : ""
        }
      `}
    >
      <span>{label}</span>
      {active && <Check size={14} className="animate-in zoom-in" />}
    </button>
  );
}
