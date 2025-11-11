// src/features/scheduling/admin/components/CreateShiftForm.tsx
import { useEffect, useState } from "react";
import { adminSchedulingApi } from "../api/adminSchedulingApi";

function toUtcIso(date: string, time: string) {
  const [h, m] = time.split(":").map(Number);
  const d = new Date(date + "T00:00:00");
  d.setHours(h ?? 0, m ?? 0, 0, 0);
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString();
}

// מחזיר תאריך יום אחרי בפורמט yyyy-MM-dd
function nextDate(date: string): string {
  const d = new Date(date + "T00:00:00");
  d.setDate(d.getDate() + 1);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export default function CreateShiftForm({
  defaultDate,
  defaultTime = "16:30",
}: {
  defaultDate: string;
  defaultTime?: string;
}) {
  const [name, setName] = useState("אירוע ערב");
  const [date, setDate] = useState(defaultDate);
  const [time, setTime] = useState(defaultTime);
  const [required, setRequired] = useState(4);
  const [minimum, setMinimum] = useState(3);
  const [earlyMin, setEarlyMin] = useState(1);
  const [skill, setSkill] = useState(2);
  const [description, setDescription] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const [checkingDate, setCheckingDate] = useState(false);
  const [dateHasShift, setDateHasShift] = useState(false);

  const baseValid =
    name.trim().length > 0 &&
    date &&
    time &&
    required >= 1 &&
    minimum >= 0 &&
    earlyMin >= 0 &&
    minimum <= required &&
    earlyMin <= required &&
    skill >= 1 &&
    skill <= 10 &&
    description.trim().length > 0;

  const canSubmit = baseValid && !dateHasShift && !submitting;

  // בדיקה מול השרת: האם כבר קיימת משמרת בתאריך שנבחר
  useEffect(() => {
    if (!date) {
      setDateHasShift(false);
      return;
    }

    let cancelled = false;

    async function check() {
      try {
        setCheckingDate(true);
        setDateHasShift(false);

        const end = nextDate(date);
        const shifts = await adminSchedulingApi.getShiftsInRange(date, end);

        if (!cancelled) {
          const has = Array.isArray(shifts) && shifts.length > 0;
          setDateHasShift(has);

          // אם היום תפוס - מראים הודעה מתאימה
          if (has) {
            setErrorMsg("כבר קיימת משמרת בתאריך זה. לא ניתן ליצור עוד משמרת לאותו יום.");
          } else if (errorMsg && errorMsg.startsWith("כבר קיימת משמרת בתאריך זה")) {
            setErrorMsg(null);
          }
        }
      } catch (err) {
        console.error("שגיאה בבדיקת משמרת קיימת לתאריך", err);
        // במקרה של שגיאה - לא ננעל בכוח, השרת עדיין מגן
      } finally {
        if (!cancelled) setCheckingDate(false);
      }
    }

    check();
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [date]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!canSubmit) return;

    setSubmitting(true);
    setSuccessMsg(null);
    setErrorMsg(null);

    try {
      const res = await adminSchedulingApi.createShift({
        name,
        startTime: toUtcIso(date, time),
        requiredEmployeeCount: required,
        minimumEmployeeCount: minimum,
        minimumEarlyEmployees: earlyMin,
        skillLevelRequired: skill,
        description,
      });

      if (res?.success === false) {
        // כולל המקרה שהשרת זיהה כפילות תאריך
        setErrorMsg(res.message || "יצירת המשמרת נכשלה.");
      } else {
        setSuccessMsg("המשמרת נוצרה בהצלחה.");
      }
    } catch (err) {
      console.error(err);
      setErrorMsg("אירעה שגיאה ביצירת המשמרת. נסה שוב או בדוק לוגים.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <>
      <form
        onSubmit={submit}
        className="space-y-5 rounded-2xl border bg-white p-5 shadow-sm"
        dir="rtl"
      >
        {/* שם / תאריך / שעה */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium">שם המשמרת</label>
            <p className="text-xs text-slate-500">שם קצר לזיהוי.</p>
            <input
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="למשל: חתונה"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium">תאריך</label>
            <p className="text-xs text-slate-500">
              יום המשמרת. אם כבר קיימת משמרת בתאריך זה, לא ניתן ליצור נוספת.
            </p>
            <input
              type="date"
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              required
            />
            {checkingDate && (
              <p className="mt-1 text-[10px] text-slate-400">בודק משמרות קיימות...</p>
            )}
            {dateHasShift && !checkingDate && (
              <p className="mt-1 text-[11px] text-red-600">
                כבר קיימת משמרת בתאריך זה. בחר תאריך אחר.
              </p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium">שעת התחלה</label>
            <p className="text-xs text-slate-500">שעת תחילת המשמרת.</p>
            <input
              type="time"
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={time}
              onChange={(e) => setTime(e.target.value)}
              required
            />
          </div>
        </div>

        {/* נדרש / מינימום / מוקדמים / מיומנות */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label className="block text-sm font-medium">נדרש</label>
            <p className="text-xs text-slate-500">מספר עובדים מלא למשמרת.</p>
            <input
              type="number"
              min={1}
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={required}
              onChange={(e) => setRequired(+e.target.value)}
              placeholder="לדוגמה: 6"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium">מינימום</label>
            <p className="text-xs text-slate-500">מספר מינימלי לקיום המשמרת.</p>
            <input
              type="number"
              min={0}
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={minimum}
              onChange={(e) => setMinimum(+e.target.value)}
              placeholder="לדוגמה: 4"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium">מוקדמים</label>
            <p className="text-xs text-slate-500">מספר מינימלי לצוות הקמה מוקדם.</p>
            <input
              type="number"
              min={0}
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={earlyMin}
              onChange={(e) => setEarlyMin(+e.target.value)}
              placeholder="לדוגמה: 2"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium">מיומנות</label>
            <p className="text-xs text-slate-500">רמת ניסיון נדרשת (1 עד 10).</p>
            <input
              type="number"
              min={1}
              max={10}
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={skill}
              onChange={(e) => setSkill(+e.target.value)}
              placeholder="1–10"
              required
            />
          </div>
        </div>

        {/* תיאור */}
        <div>
          <label className="block text-sm font-medium">תיאור</label>
          <p className="text-xs text-slate-500">פרטים חשובים על המשמרת.</p>
          <textarea
            className="mt-1 w-full rounded-xl border px-3 py-2"
            rows={3}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="למשל: סוג אירוע, מיקום, קוד לבוש, הערות מיוחדות..."
            required
          />
        </div>

        {/* הודעות ולידציה */}
        {!baseValid && (
          <div className="text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded-lg px-3 py-2">
            ודא: שם למשמרת, לפחות עובד אחד, מינימום ומוקדמים לא גדולים מהנדרש,
            רמת מיומנות בין 1 ל-10 ותיאור מלא.
          </div>
        )}

        {errorMsg && (
          <div className="text-xs text-red-700 bg-red-50 border border-red-200 rounded-lg px-3 py-2">
            {errorMsg}
          </div>
        )}

        <div className="flex items-center justify-end">
          <button
            disabled={!canSubmit}
            className={`rounded-xl px-4 py-2 text-sm text-white ${
              !canSubmit
                ? "bg-slate-400"
                : "bg-emerald-600 hover:bg-emerald-500"
            }`}
          >
            {submitting ? "שומר..." : "צור משמרת"}
          </button>
        </div>
      </form>

      {/* מודאל הצלחה במרכז המסך */}
      {successMsg && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-2xl text-center">
            <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-emerald-100">
              <span className="text-2xl">✅</span>
            </div>
            <h2 className="text-lg font-semibold text-slate-900 mb-1">
              המשמרת נשמרה בהצלחה
            </h2>
            <p className="text-sm text-slate-600 mb-4">
              הפרטים נשמרו במערכת. תוכל לראות את המשמרת במסך ניהול המשמרות.
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
    </>
  );
}
