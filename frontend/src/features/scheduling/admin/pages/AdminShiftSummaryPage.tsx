// src/features/scheduling/admin/pages/AdminShiftSummaryPage.tsx
import { useLocation, useParams, Link, useNavigate } from "react-router-dom";
import { useState } from "react";
import { adminSchedulingApi } from "../api/adminSchedulingApi";
import type { ProcessShiftServerItem } from "../api/adminSchedulingApi";

function toIsoIL(dateLike: string) {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Jerusalem",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(new Date(dateLike));
}

function toTimeIL(dateLike: string) {
  return new Intl.DateTimeFormat("he-IL", {
    timeZone: "Asia/Jerusalem",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(new Date(dateLike));
}

export default function AdminShiftSummaryPage() {
  const { shiftId } = useParams<{ shiftId: string }>();
  const location = useLocation();
  const navigate = useNavigate();

  const item =
    (location.state as { item?: ProcessShiftServerItem } | null)?.item;

  const [cancelLoading, setCancelLoading] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);
  const [cancelSuccess, setCancelSuccess] = useState<string | null>(null);

  // 1) אם אין shiftId בנתיב
  if (!shiftId) {
    return <div className="p-6">חסר מזהה משמרת בנתיב.</div>;
  }

  // 2) אם אין item ב-state (נווטו לפה ישירות)
  if (!item) {
    return (
      <div dir="rtl" className="mx-auto max-w-3xl p-6">
        <header className="mb-4 flex items-center justify-between">
          <div>
            <h1 className="text-xl font-extrabold text-slate-900">
              תקציר משמרת
            </h1>
            <p className="text-slate-600 text-sm">ID - {shiftId}</p>
          </div>
          <Link
            to="/admin/shifts"
            className="text-sm underline text-sky-700"
          >
            חזרה
          </Link>
        </header>

        <section className="rounded-2xl border bg-white p-4">
          <p className="text-slate-700">
            אין נתוני תקציר טעונים לדף הזה. חזור לעמוד הניהול ולחץ על
            &quot;תקציר&quot; במשמרת הרצויה.
          </p>
        </section>
      </div>
    );
  }

  // מכאן והלאה TS עדיין חושב ש-item ו-shiftId יכולים להיות undefined בתוך handleCancel,
  // לכן נטפל בזה ידנית שם.

  const day = toIsoIL(item.startTime);
  const time = toTimeIL(item.startTime);

  const hasSummary =
    typeof item.summary === "string" && item.summary.trim().length > 0;
  const hasAnalysis =
    typeof item.analysis === "string" && item.analysis.trim().length > 0;

  const startDate = new Date(item.startTime);
  const isPast = startDate <= new Date();

  async function handleCancel() {
    setCancelError(null);
    setCancelSuccess(null);

    // הגנה כפולה ל-TypeScript (בפועל כבר בדקנו למעלה)
    if (!shiftId) {
      setCancelError("חסר מזהה משמרת.");
      return;
    }

    const confirm = window.confirm(
      "לבטל את המשמרת הזו? פעולה זו תמנע הרשמות עתידיות ואינה ניתנת לשחזור."
    );
    if (!confirm) return;

    setCancelLoading(true);
    try {
      // כאן ה-non-null assertion (!) פותר את שגיאת ה-TS
      const res = await adminSchedulingApi.cancelShift(shiftId!);

      if (!res.success) {
        setCancelError(res.message || "לא ניתן היה לבטל את המשמרת.");
        return;
      }

      // item בתוך הפונקציה נתפס כ־item | undefined, אז נוסיף ? כדי להרגיע את TS
      const label =
        res.shiftName ||
        item?.summary ||
        `משמרת בתאריך ${day} שעה ${time}`;

      setCancelSuccess(`המשמרת "${label}" בוטלה בהצלחה.`);
    } catch (err) {
      console.error(err);
      setCancelError("אירעה שגיאה בביטול המשמרת. נסה שוב או בדוק לוגים.");
    } finally {
      setCancelLoading(false);
    }
  }

  return (
    <div dir="rtl" className="mx-auto max-w-3xl p-6">
      <header className="mb-4 flex items-center justify-between">
        <div>
          <h1 className="text-xl font-extrabold text-slate-900">
            תקציר משמרת
          </h1>
          <p className="text-slate-600 text-sm">
            תאריך {day} • שעה {time} • ID: {shiftId}
          </p>
        </div>
        <Link
          to="/admin/shifts"
          className="text-sm underline text-sky-700"
        >
          חזרה
        </Link>
      </header>

      <section className="rounded-2xl border bg-white p-4">
        <h2 className="text-sm font-semibold text-slate-800 mb-2">סיכום</h2>
        <p className="whitespace-pre-line text-slate-800">
          {hasSummary
            ? String(item.summary)
            : `אין תקציר מהמערכת. דרושים ${item.required}, מינימום ${item.minimum}, מוקדמים נדרש ${item.minimumEarly}. מתוכננים ${item.plannedCount} (מוקדמים ${item.plannedEarlyCount} / רגיל ${item.plannedRegularCount}).`}
        </p>

        {hasAnalysis && (
          <>
            <div className="h-px my-4 bg-slate-200" />
            <h3 className="text-sm font-semibold text-slate-800 mb-2">
              ניתוח מפורט
            </h3>
            <p className="whitespace-pre-line text-slate-700">
              {String(item.analysis)}
            </p>
          </>
        )}
      </section>

      {/* פעולות */}
      <div className="mt-6 flex items-center justify-between gap-3">
        <div className="text-xs text-slate-500">
          {isPast
            ? "המשמרת כבר החלה או הסתיימה. לא ניתן לבטל."
            : "ניתן לבטל משמרת זו לפני זמן ההתחלה."}
        </div>

        <button
          onClick={handleCancel}
          disabled={cancelLoading || isPast}
          className={`rounded-xl px-4 py-2 text-sm font-semibold text-white ${
            cancelLoading || isPast
              ? "bg-slate-400 cursor-not-allowed"
              : "bg-red-600 hover:bg-red-500"
          }`}
        >
          {cancelLoading ? "מבטל..." : "בטל משמרת"}
        </button>
      </div>

      {cancelError && (
        <div className="mt-3 text-xs text-red-700 bg-red-50 border border-red-200 rounded-lg px-3 py-2">
          {cancelError}
        </div>
      )}

      {/* מודאל הצלחה */}
      {cancelSuccess && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-2xl text-center">
            <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-emerald-100">
              <span className="text-2xl">✅</span>
            </div>
            <h2 className="text-lg font-semibold text-slate-900 mb-1">
              המשמרת בוטלה
            </h2>
            <p className="text-sm text-slate-600 mb-4">
              {cancelSuccess}
            </p>
            <button
              onClick={() => {
                setCancelSuccess(null);
                navigate("/admin/shifts");
              }}
              className="w-full rounded-xl bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-500 transition"
            >
              חזור לניהול משמרות
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
