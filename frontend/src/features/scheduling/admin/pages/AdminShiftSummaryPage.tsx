// src/features/scheduling/admin/pages/AdminShiftSummaryPage.tsx
import { useLocation, useParams, Link } from "react-router-dom";
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
  const item = (location.state as { item?: ProcessShiftServerItem } | null)?.item;

  if (!shiftId) {
    return <div className="p-6">חסר מזהה משמרת בנתיב.</div>;
  }

  if (!item) {
    return (
      <div dir="rtl" className="mx-auto max-w-3xl p-6">
        <header className="mb-4 flex items-center justify-between">
          <div>
            <h1 className="text-xl font-extrabold text-slate-900">תקציר משמרת</h1>
            <p className="text-slate-600 text-sm">ID - {shiftId}</p>
          </div>
          <Link to="/admin/shifts" className="text-sm underline text-sky-700">חזרה</Link>
        </header>

        <section className="rounded-2xl border bg-white p-4">
          <p className="text-slate-700">
            אין נתוני תקציר טעונים לדף הזה. חזור לעמוד הניהול ולחץ על “תקציר” במשמרת הרצויה.
          </p>
        </section>
      </div>
    );
  }

  const day = toIsoIL(item.startTime);
  const time = toTimeIL(item.startTime);
  const hasSummary = typeof item.summary === "string" && item.summary.trim().length > 0;
  const hasAnalysis = typeof item.analysis === "string" && item.analysis.trim().length > 0;

  return (
    <div dir="rtl" className="mx-auto max-w-3xl p-6">
      <header className="mb-4 flex items-center justify-between">
        <div>
          <h1 className="text-xl font-extrabold text-slate-900">תקציר משמרת</h1>
          <p className="text-slate-600 text-sm">
            תאריך {day} • שעה {time} • ID: {shiftId}
          </p>
        </div>
        <Link to="/admin/shifts" className="text-sm underline text-sky-700">חזרה</Link>
      </header>

      <section className="rounded-2xl border bg-white p-4">
        <h2 className="text-sm font-semibold text-slate-800 mb-2">סיכום</h2>
        <p className="whitespace-pre-line text-slate-800">
          {hasSummary
            ? String(item.summary)
            : `אין תקציר מה-AI. דרושים ${item.required}, מינימום ${item.minimum}, מוקדמים נדרש ${item.minimumEarly}. מתוכננים ${item.plannedCount} (מוקדמים ${item.plannedEarlyCount} / רגיל ${item.plannedRegularCount}).`}
        </p>

        {hasAnalysis && (
          <>
            <div className="h-px my-4 bg-slate-200" />
            <h3 className="text-sm font-semibold text-slate-800 mb-2">ניתוח מפורט</h3>
            <p className="whitespace-pre-line text-slate-700">{String(item.analysis)}</p>
          </>
        )}
      </section>
    </div>
  );
}
