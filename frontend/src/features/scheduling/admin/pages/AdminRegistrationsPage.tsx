import { useMemo, useState } from "react";
import {
  useRegistrations,
  type RegistrationStatusFilter,
} from "../hooks/useRegistrations";
import { Link } from "react-router-dom";

/** התחלה נקיה של היום (לא נדרש אזור זמן מיוחד לניווט שבועי) */
function startOfToday() {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
}

/** ממיר "YYYY-MM-DD" ל-Date בצורה בטוחה */
function parseYmd(ymd: string | undefined | null): Date | null {
  if (!ymd) return null;
  const parts = ymd.split("-");
  if (parts.length !== 3) return null;
  const [y, m, d] = parts.map(Number);
  if (!y || !m || !d) return null;
  return new Date(Date.UTC(y, m - 1, d));
}

/** מחזיר יום בשבוע + תאריך יפה להצגה */
function getDayAndDateLabel(ymd: string) {
  const d = parseYmd(ymd);
  if (!d) {
    return {
      day: "",
      dateLabel: ymd,
    };
  }

  const day = new Intl.DateTimeFormat("he-IL", {
    weekday: "long",
    timeZone: "Asia/Jerusalem",
  }).format(d);

  const dateLabel = new Intl.DateTimeFormat("he-IL", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    timeZone: "Asia/Jerusalem",
  }).format(d);

  return { day, dateLabel };
}

const STATUS_LABELS: RegistrationStatusFilter[] = [
  "All",
  "Pending",
  "Approved",
  "Rejected",
  "Cancelled",
];

export default function AdminRegistrationsPage() {
  // נקודת ציר לשבוע - ברירת מחדל היום
  const [pivot, setPivot] = useState<Date>(startOfToday());

  const {
    from,
    to,
    daysPills,
    selectedDate,
    selectDay,
    selectedDaySummary,
    names,
    status,
    changeStatus,
    hasMore,
    loadMore,
    loadingSnapshot,
    loadingNames,
    error,
    reloadSnapshot,
    refreshDay,
  } = useRegistrations(pivot);

  // ניווט שבועי
  const prevWeek = () =>
    setPivot(d => new Date(d.getTime() - 7 * 24 * 60 * 60 * 1000));
  const nextWeek = () =>
    setPivot(d => new Date(d.getTime() + 7 * 24 * 60 * 60 * 1000));
  const thisWeek = () => setPivot(startOfToday());

  // תווית טווח תאריכים יפה: 09.11.2025 - 13.11.2025
  const dateRangeLabel = useMemo(() => {
    const fromLabel = getDayAndDateLabel(from).dateLabel;
    const toLabel = getDayAndDateLabel(to).dateLabel;
    return `${fromLabel} - ${toLabel}`;
  }, [from, to]);

  return (
    <div dir="rtl" className="mx-auto max-w-6xl p-6">
      {/* Header */}
      <Link
        to="/admin"
        className="rounded-xl border border-sky-200 bg-sky-50 px-3 py-1.5 text-sm text-sky-800 hover:bg-sky-100"
      >
        חזרה למרכז ניהול
      </Link>
      <header className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-extrabold text-slate-900">נרשמים</h1>

          <p className="text-xs text-slate-500 mt-1">{dateRangeLabel}</p>

          <p className="text-slate-600 text-sm mt-1">
            בחר יום כדי לראות את מצב הנרשמים ואת השמות
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={prevWeek}
            className="rounded-xl border px-3 py-1.5 text-sm hover:bg-slate-50"
          >
            שבוע קודם
          </button>
          <button
            onClick={thisWeek}
            className="rounded-xl border px-3 py-1.5 text-sm hover:bg-slate-50"
          >
            השבוע
          </button>
          <button
            onClick={nextWeek}
            className="rounded-xl border px-3 py-1.5 text-sm hover:bg-slate-50"
          >
            שבוע הבא
          </button>
          <button
            onClick={reloadSnapshot}
            className="rounded-xl border px-3 py-1.5 text-sm hover:bg-slate-50"
            title="רענן תמונת מצב שבועית"
          >
            רענון
          </button>
        </div>
      </header>

      {/* Pills - ימים א עד ה */}
      <section className="mb-6">
        {loadingSnapshot && (
          <div className="text-sm text-slate-500">טוען תמונת מצב...</div>
        )}
        {!loadingSnapshot && daysPills.length === 0 && (
          <div className="text-sm text-slate-500">
            אין משמרות בטווח השבוע שנבחר.
          </div>
        )}

        <div className="flex flex-wrap gap-2">
          {daysPills.map(d => {
            const { day, dateLabel } = getDayAndDateLabel(d.date);
            return (
              <button
                key={d.date}
                onClick={() => selectDay(d.date)}
                className={[
                  "rounded-2xl border px-4 py-2 text-right transition",
                  d.isSelected
                    ? "bg-sky-50 border-sky-300"
                    : "bg-white border-slate-200 hover:bg-slate-50",
                ].join(" ")}
              >
                <div className="text-xs text-slate-500">{day}</div>
                <div className="text-sm font-semibold text-slate-800">
                  {dateLabel}
                </div>
              </button>
            );
          })}
        </div>
      </section>

      {/* Status filter + summary */}
      <section className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-2">
          {STATUS_LABELS.map(s => (
            <button
              key={s}
              onClick={() => changeStatus(s)}
              className={[
                "rounded-xl border px-3 py-1.5 text-sm transition",
                status === s
                  ? "bg-slate-900 text-white border-slate-900"
                  : "bg-white text-slate-700 border-slate-200 hover:bg-slate-50",
              ].join(" ")}
            >
              {s === "All" ? "הכל" : s}
            </button>
          ))}
        </div>

        {/* כרטיס תקציר מסודר */}
        <div className="flex-1">
          {selectedDaySummary ? (
            <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                {/* תאריך + יום */}
                <div>
                  {(() => {
                    const { day, dateLabel } = getDayAndDateLabel(
                      selectedDaySummary.date
                    );
                    return (
                      <>
                        <div className="text-sm text-slate-500">{day}</div>
                        <div className="text-base font-semibold text-slate-900">
                          {dateLabel}
                        </div>
                      </>
                    );
                  })()}
                </div>

                {/* המספרים עצמם */}
                <div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-slate-700">
                  <SummaryItem
                    label="נדרשים"
                    value={selectedDaySummary.required}
                  />
                  <SummaryItem
                    label="נרשמו"
                    value={selectedDaySummary.registered}
                  />
                  <SummaryItem
                    label="ממתינים"
                    value={selectedDaySummary.pending}
                  />
                  <SummaryItem
                    label="אושרו"
                    value={selectedDaySummary.approved}
                  />
                  <SummaryItem
                    label="נדחו"
                    value={selectedDaySummary.rejected}
                  />
                  <SummaryItem
                    label="בוטלו"
                    value={selectedDaySummary.cancelled}
                  />
                </div>
              </div>
            </div>
          ) : (
            <div className="text-sm text-slate-600">
              בחר יום כדי לראות תקציר
            </div>
          )}
        </div>
      </section>

      {/* Names list */}
      <section className="rounded-2xl border border-slate-200 bg-white p-4">
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-lg font-bold text-slate-900">
            רשימת נרשמים {selectedDate ? `• ${selectedDate}` : ""}
          </h2>

          <div className="flex items-center gap-2">
            <button
              onClick={refreshDay}
              className="rounded-xl border px-3 py-1.5 text-sm hover:bg-slate-50"
              title="רענן את רשימת השמות ליום הנבחר"
              disabled={!selectedDate || loadingNames}
            >
              רענן יום
            </button>
          </div>
        </div>

        {error && (
          <div className="mb-3 rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            {error}
          </div>
        )}

        {loadingNames && names.length === 0 && (
          <div className="text-sm text-slate-500">טוען שמות...</div>
        )}

        {!loadingNames && names.length === 0 && (
          <div className="text-sm text-slate-500">אין נרשמים להצגה.</div>
        )}

        <ul className="divide-y divide-slate-200">
          {names.map(n => (
            <li
              key={`${n.employeeId}-${n.shiftDate}-${n.status}`}
              className="py-2 flex items-center justify-between"
            >
              <div>
                <div className="font-medium text-slate-800">
                  {n.firstName} {n.lastName}
                </div>
                <div className="text-xs text-slate-500">
                  {n.shiftDate} •{" "}
                  {n.shiftArrivalType === 2 ? "Early" : "Regular"}
                </div>
              </div>

              <span
                className={[
                  "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
                  n.status === "Approved"
                    ? "bg-emerald-50 text-emerald-700 border border-emerald-200"
                    : n.status === "Pending"
                    ? "bg-amber-50 text-amber-700 border border-amber-200"
                    : n.status === "Rejected"
                    ? "bg-rose-50 text-rose-700 border border-rose-200"
                    : "bg-slate-50 text-slate-700 border border-slate-200",
                ].join(" ")}
              >
                {n.status}
              </span>
            </li>
          ))}
        </ul>

        {/* Load more */}
        <div className="mt-3 flex items-center justify-center">
          {hasMore && (
            <button
              onClick={loadMore}
              disabled={loadingNames}
              className="rounded-xl border px-4 py-2 text-sm hover:bg-slate-50"
            >
              {loadingNames ? "טוען..." : "טען עוד"}
            </button>
          )}
        </div>
      </section>
    </div>
  );
}

/* ===== קומפוננטה קטנה לשורת תקציר ===== */
function SummaryItem({ label, value }: { label: string; value: number }) {
  return (
    <span>
      {label} <span className="font-semibold">{value}</span>
    </span>
  );
}
