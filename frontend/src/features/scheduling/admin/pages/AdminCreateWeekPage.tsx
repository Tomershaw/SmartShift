import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  adminSchedulingApi,
  type ShiftSummaryDto,
} from "../api/adminSchedulingApi";

type DayCfg = {
  active: boolean;
  date: string; // YYYY-MM-DD
  time: string; // HH:MM
  name: string;
  required: number;
  minimum: number;
  earlyMin: number;
  skill: number;
  description: string;
};

const HEB_DAYS = ["ראשון", "שני", "שלישי", "רביעי", "חמישי"];

/* ראשון הבא */
function nextSunday(d = new Date()) {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  const dow = x.getDay(); // 0 = ראשון
  const daysToNextSunday = (7 - dow) % 7 || 7;
  x.setDate(x.getDate() + daysToNextSunday);
  return x;
}

/* YYYY-MM-DD */
function fmtDate(d: Date) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${dd}`;
}

/* תאריך הבא כ YYYY-MM-DD */
function nextDate(date: string): string {
  const d = new Date(date + "T00:00:00");
  d.setDate(d.getDate() + 1);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${dd}`;
}

/* ל UTC ISO */
function toUtcIso(date: string, time: string) {
  const [h, m] = time.split(":").map(Number);
  const d = new Date(date + "T00:00:00");
  d.setHours(h ?? 0, m ?? 0, 0, 0);
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString();
}

/* YYYY-MM-DD מתוך startTime */
function shiftToDateIso(shift: ShiftSummaryDto): string {
  return shift.startTime.substring(0, 10);
}

/* Stepper מספרי כללי */
type NumberStepperProps = {
  label: string;
  value: number;
  onChange: (value: number) => void;
  min: number;
  max: number;
  disabled?: boolean;
};

function NumberStepper({
  label,
  value,
  onChange,
  min,
  max,
  disabled,
}: NumberStepperProps) {
  const dec = () => {
    if (disabled) return;
    if (value > min) onChange(value - 1);
  };

  const inc = () => {
    if (disabled) return;
    if (value < max) onChange(value + 1);
  };

  return (
    <div>
      <label className="block text-xs sm:text-sm font-medium text-slate-700 mb-1">
        {label}
      </label>
      <div
        className={[
          "mt-1 flex items-center rounded-xl border px-2 py-1.5 bg-white shadow-sm",
          disabled
            ? "border-slate-200 bg-slate-50 opacity-70 cursor-not-allowed"
            : "border-slate-300",
        ].join(" ")}
      >
        <button
          type="button"
          onClick={dec}
          disabled={disabled || value <= min}
          className="px-2 py-1 text-sm font-semibold text-slate-700 disabled:text-slate-300 disabled:cursor-not-allowed"
        >
          -
        </button>
        <div className="flex-1 text-center text-sm font-semibold text-slate-900">
          {value}
        </div>
        <button
          type="button"
          onClick={inc}
          disabled={disabled || value >= max}
          className="px-2 py-1 text-sm font-semibold text-slate-700 disabled:text-slate-300 disabled:cursor-not-allowed"
        >
          +
        </button>
      </div>
    </div>
  );
}

export default function AdminCreateWeekPage() {
  // ברירת מחדל: ראשון עד חמישי של השבוע הבא
  const defaults = useMemo(() => {
    const sun = nextSunday();
    return Array.from({ length: 5 }, (_, i) => {
      const d = new Date(sun);
      d.setDate(d.getDate() + i);
      return fmtDate(d);
    });
  }, []);

  const [days, setDays] = useState<DayCfg[]>(
    defaults.map((date) => ({
      active: false,
      date,
      time: "16:30",
      name: "אירוע ערב",
      required: 4,
      minimum: 3,
      earlyMin: 1,
      skill: 2,
      description: "אירוע ערב",
    }))
  );

  const [selected, setSelected] = useState(0);
  const [submitting, setSubmitting] = useState(false);

  const [existingByDate, setExistingByDate] = useState<
    Record<string, ShiftSummaryDto | undefined>
  >({});

  const [loadingExisting, setLoadingExisting] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [deletingShiftId, setDeletingShiftId] = useState<string | null>(null);

  const activeCount = days.filter(
    (d) => d.active && !existingByDate[d.date]
  ).length;

  // טעינת משמרות קיימות לשבוע
  useEffect(() => {
    async function load() {
      try {
        setLoadingExisting(true);
        setErrorMsg(null);

        const start = defaults[0];
        const endExclusive = nextDate(defaults[defaults.length - 1]);
        const shifts = await adminSchedulingApi.getShiftsInRange(
          start,
          endExclusive
        );

        const map: Record<string, ShiftSummaryDto> = {};
        for (const s of shifts) {
          const iso = shiftToDateIso(s);
          if (!map[iso]) {
            map[iso] = s;
          }
        }

        setExistingByDate(map);

        setDays((prev) =>
          prev.map((d) => (map[d.date] ? { ...d, active: false } : d))
        );
      } catch (err) {
        console.error("שגיאה בטעינת משמרות קיימות לשבוע:", err);
        setErrorMsg("לא הצלחנו לבדוק את הימים. השרת עדיין מגן מפני כפילות.");
      } finally {
        setLoadingExisting(false);
      }
    }

    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function update(idx: number, patch: Partial<DayCfg>) {
    setDays((prev) =>
      prev.map((d, i) => (i === idx ? { ...d, ...patch } : d))
    );
  }

  function clearCurrent() {
    const cur = days[selected];
    const locked = !!existingByDate[cur.date];
    if (locked) return;

    setDays((prev) =>
      prev.map((d, i) =>
        i === selected
          ? {
              ...d,
              active: false,
              time: "16:30",
              name: "",
              required: 1,
              minimum: 0,
              earlyMin: 0,
              skill: 1,
              description: "",
            }
          : d
      )
    );
  }

  function handleDateChange(newDate: string) {
    if (!newDate) return;
    const cur = days[selected];
    const locked = !!existingByDate[cur.date];
    if (locked) return;

    if (existingByDate[newDate]) {
      alert("כבר קיימת משמרת בתאריך שנבחר. בחר תאריך אחר.");
      return;
    }

    setDays((prev) =>
      prev.map((d, i) => (i === selected ? { ...d, date: newDate } : d))
    );
  }

  async function handleDeleteShiftForDate(date: string) {
    const shift = existingByDate[date];
    if (!shift) return;

    const ok = window.confirm(`לבטל (למחוק) את המשמרת בתאריך ${date}?`);
    if (!ok) return;

    try {
      setDeletingShiftId(shift.id);
      setErrorMsg(null);
      setSuccessMsg(null);

      const res = await adminSchedulingApi.deleteShift(shift.id);

      if (!res.success) {
        setErrorMsg(res.message || "לא ניתן לבטל את המשמרת.");
        return;
      }

      setExistingByDate((prev) => {
        const copy = { ...prev };
        delete copy[date];
        return copy;
      });

      setDays((prev) =>
        prev.map((d) =>
          d.date === date
            ? {
                ...d,
                active: false,
              }
            : d
        )
      );

      setSuccessMsg("המשמרת ליום שנבחר בוטלה בהצלחה. ניתן ליצור חדשה.");
    } catch (err) {
      console.error("שגיאה במחיקת משמרת:", err);
      setErrorMsg("שגיאה בביטול המשמרת. נסה שוב.");
    } finally {
      setDeletingShiftId(null);
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setErrorMsg(null);
    setSuccessMsg(null);

    if (activeCount === 0) {
      setErrorMsg("בחר לפחות יום אחד פנוי ליצירת משמרת.");
      return;
    }

    setSubmitting(true);
    try {
      let created = 0;
      const nextExisting = { ...existingByDate };

      for (const d of days) {
        if (!d.active) continue;
        if (nextExisting[d.date]) continue;

        const res = await adminSchedulingApi.createShift({
          name: d.name,
          startTime: toUtcIso(d.date, d.time),
          requiredEmployeeCount: d.required,
          minimumEmployeeCount: d.minimum,
          minimumEarlyEmployees: d.earlyMin,
          skillLevelRequired: d.skill,
          description: d.description,
        });

        if (res?.success) {
          created += 1;
          nextExisting[d.date] = {
            id: res.shiftId || "",
            startTime: res.startTime || toUtcIso(d.date, d.time),
          } as ShiftSummaryDto;
        }
      }

      if (created > 0) {
        setExistingByDate(nextExisting);

        setDays((prev) =>
          prev.map((d) =>
            nextExisting[d.date] ? { ...d, active: false } : d
          )
        );

        setSuccessMsg(`נוצרו ${created} משמרות בהצלחה.`);
      } else {
        setErrorMsg("לא נוצרו משמרות. ייתכן שכל הימים שנבחרו כבר תפוסים.");
      }
    } catch (err) {
      console.error(err);
      setErrorMsg("שגיאה ביצירת המשמרות. נסה שוב או בדוק לוגים.");
    } finally {
      setSubmitting(false);
    }
  }

  const cur = days[selected];
  const lockedShift = existingByDate[cur.date];
  const isLocked = !!lockedShift;

  return (
    <div dir="rtl" className="mx-auto max-w-5xl p-4 sm:p-6">
      {/* כותרת */}
      <header className="mb-4 sm:mb-6 flex items-center justify-between gap-3">
        <div>
          <h1 className="text-xl sm:text-2xl font-extrabold text-slate-900">
            יצירת משמרות לשבוע
          </h1>
        </div>
        <Link
          to="/admin"
          className="rounded-xl border border-sky-200 bg-sky-50 px-3 py-1.5 text-xs sm:text-sm text-sky-800 hover:bg-sky-100"
        >
          חזרה למרכז ניהול
        </Link>
      </header>

      {/* סרגל ימים */}
      <div className="mb-4 -mx-2 flex flex-nowrap gap-2 overflow-x-auto pb-1 sm:mx-0 sm:flex-wrap sm:justify-start">
        {days.map((d, idx) => {
          const locked = !!existingByDate[d.date];
          const isActiveTab = idx === selected;

          return (
            <button
              key={d.date}
              type="button"
              onClick={() => setSelected(idx)}
              className={[
                "flex min-w-[90px] items-center justify-center gap-2 rounded-full px-4 py-2 text-xs sm:text-sm border transition",
                isActiveTab
                  ? "bg-slate-900 text-white border-slate-900"
                  : "bg-white text-slate-800 border-slate-300 hover:bg-slate-50",
              ].join(" ")}
            >
              <span className="font-semibold">{HEB_DAYS[idx]}</span>
              <span className="text-[10px] text-slate-500 hidden sm:inline">
                {d.date}
              </span>
              {locked ? (
                <span className="text-[9px] rounded-full bg-red-50 text-red-700 border border-red-200 px-2 py-0.5 hidden sm:inline">
                  קיימת משמרת
                </span>
              ) : d.active ? (
                <span className="text-[9px] rounded-full bg-emer
ald-50 text-emerald-700 border border-emerald-200 px-2 py-0.5 hidden sm:inline">
                  נבחר
                </span>
              ) : (
                <span className="text-[9px] rounded-full bg-slate-50 text-slate-500 border border-slate-200 px-2 py-0.5 hidden sm:inline">
                  כבוי
                </span>
              )}
            </button>
          );
        })}
      </div>

      {/* שורת סטטוס יום נבחר */}
      <div className="mb-2 flex flex-wrap items-center justify-between gap-2 text-slate-700 text-xs sm:text-sm">
        <div>
          <span className="font-semibold">{HEB_DAYS[selected]}</span> •{" "}
          {cur.date}
          {isLocked && (
            <span className="ml-2 text-red-600 text-[10px] sm:text-xs">
              קיימת משמרת ליום זה. לא ניתן ליצור נוספת.
            </span>
          )}
        </div>

        {isLocked && lockedShift && (
          <button
            type="button"
            onClick={() => handleDeleteShiftForDate(cur.date)}
            disabled={deletingShiftId === lockedShift.id}
            className="text-[9px] sm:text-xs rounded-full border border-red-300 bg-red-50 px-3 py-1 text-red-700 hover:bg-red-100"
          >
            {deletingShiftId === lockedShift.id ? "מבטל..." : "בטל משמרת ליום זה"}
          </button>
        )}
      </div>

      {/* טופס יום נבחר */}
      <form
        onSubmit={handleSubmit}
        className="space-y-4 rounded-2xl border bg-white p-4 sm:p-5 shadow-sm"
      >
        {/* צקבוקס + נקה */}
        <div className="flex flex-wrap items-center justify-between gap-3">
          <label className="inline-flex items-center gap-2 text-xs sm:text-sm">
            <input
              type="checkbox"
              checked={cur.active && !isLocked}
              onChange={(e) => {
                if (isLocked) return;
                update(selected, { active: e.target.checked });
              }}
              disabled={isLocked}
            />
            ליצור משמרת ביום זה
          </label>

          <button
            type="button"
            onClick={clearCurrent}
            className="text-[9px] sm:text-xs rounded-lg border px-2.5 py-1.5 text-slate-700 hover:bg-slate-50"
            disabled={isLocked}
          >
            נקה יום
          </button>
        </div>

        {/* שם המשמרת + תאריך */}
        <div className="grid grid-cols-1 sm:grid-cols-[2fr,1.2fr] gap-3">
          <div>
            <label className="block text-xs sm:text-sm font-medium">
              שם המשמרת
            </label>
            <input
              className="mt-1 w-full rounded-xl border px-3 py-2 text-sm"
              value={cur.name}
              onChange={(e) => update(selected, { name: e.target.value })}
              placeholder="למשל: אירוע ערב"
              disabled={isLocked || !cur.active}
            />
          </div>

          <div>
            <label className="block text-xs sm:text-sm font-medium">
              תאריך המשמרת
            </label>
            <div className="mt-1 max-w-md mx-auto sm:max-w-none sm:mx-0 rounded-xl border bg-white overflow-hidden">
              <input
                type="date"
                className="w-full px-3 py-2 text-sm text-center bg-transparent focus:outline-none"
                value={cur.date}
                onChange={(e) => handleDateChange(e.target.value)}
                disabled={isLocked}
              />
            </div>
          </div>
        </div>

        {/* שעה + מיומנות */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="block text-xs sm:text-sm font-medium">
              שעת התחלה
            </label>
            <div className="mt-1 max-w-md mx-auto sm:max-w-none sm:mx-0 rounded-xl border bg-white overflow-hidden">
              <input
                type="time"
                className="w-full px-3 py-2 text-sm text-center bg-transparent focus:outline-none"
                value={cur.time}
                onChange={(e) => update(selected, { time: e.target.value })}
                disabled={isLocked || !cur.active}
              />
            </div>
          </div>

          <NumberStepper
            label="מיומנות (1–10)"
            value={cur.skill}
            onChange={(val) => update(selected, { skill: val })}
            min={1}
            max={10}
            disabled={isLocked || !cur.active}
          />
        </div>

        {/* נדרש / מינימום / מוקדמים */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <NumberStepper
            label="נדרש"
            value={cur.required}
            onChange={(val) => update(selected, { required: val })}
            min={1}
            max={15}
            disabled={isLocked || !cur.active}
          />
          <NumberStepper
            label="מינימום"
            value={cur.minimum}
            onChange={(val) => update(selected, { minimum: val })}
            min={0}
            max={15}
            disabled={isLocked || !cur.active}
          />
          <NumberStepper
            label="מוקדמים"
            value={cur.earlyMin}
            onChange={(val) => update(selected, { earlyMin: val })}
            min={0}
            max={10}
            disabled={isLocked || !cur.active}
          />
        </div>

        {/* תיאור */}
        <div>
          <label className="block text-xs sm:text-sm font-medium">תיאור</label>
          <textarea
            className="mt-1 w-full rounded-xl border px-3 py-2 text-sm"
            rows={3}
            value={cur.description}
            onChange={(e) => update(selected, { description: e.target.value })}
            placeholder="פרטים חשובים למשמרת"
            disabled={isLocked || !cur.active}
          />
        </div>

        {loadingExisting && (
          <div className="text-[9px] sm:text-xs text-slate-500">
            בודק אילו ימים תפוסים...
          </div>
        )}

        {errorMsg && (
          <div className="text-[9px] sm:text-xs text-red-700 bg-red-50 border border-red-200 rounded-lg px-3 py-2">
            {errorMsg}
          </div>
        )}

        <div className="flex items-center justify-between">
          <div className="text-[9px] sm:text-sm text-slate-600">
            <span className="font-semibold">{activeCount}</span> ימים מסומנים
            ליצירת משמרת (פנויים בלבד).
          </div>
          <button
            disabled={submitting || activeCount === 0}
            className={`rounded-xl px-4 py-2 text-xs sm:text-sm text-white ${
              submitting || activeCount === 0
                ? "bg-slate-400"
                : "bg-emerald-600 hover:bg-emerald-500"
            }`}
          >
            {submitting ? "יוצר..." : `צור ${activeCount} משמרות`}
          </button>
        </div>
      </form>

      {/* מודאל הצלחה */}
      {successMsg && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-2xl text-center">
            <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-emerald-100">
              <span className="text-2xl">✅</span>
            </div>
            <h2 className="text-lg font-semibold text-slate-900 mb-1">
              פעולה בוצעה בהצלחה
            </h2>
            <p className="text-sm text-slate-600 mb-4">
              ניתן לראות את מצב המשמרות במסך ניהול המשמרות.
            </p>
            <button
              onClick={() => setSuccessMsg(null)}
              className="w-full rounded-xl bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-500 transition"
            >
              סגור
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
