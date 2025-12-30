import { useMemo, useState } from "react";
import {
  useRegistrations,
  type RegistrationStatusFilter,
} from "../hooks/useRegistrations";
import { Link } from "react-router-dom";

// --- אייקונים לשיפור המראה ---
const Icons = {
  Back: () => (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="m9 18 6-6-6-6" />
    </svg>
  ),
  ChevronRight: () => (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="m9 18 6-6-6-6" />
    </svg>
  ),
  ChevronLeft: () => (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="m15 18-6-6 6-6" />
    </svg>
  ),
  Refresh: () => (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="14"
      height="14"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M21 12a9 9 0 0 0-9-9 9.75 9.75 0 0 0-6.74 2.74L3 8" />
      <path d="M3 3v5h5" />
      <path d="M3 12a9 9 0 0 0 9 9 9.75 9.75 0 0 0 6.74-2.74L21 16" />
      <path d="M16 21h5v-5" />
    </svg>
  ),
};

// --- פונקציות עזר ---
function startOfToday() {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
}

function parseYmd(ymd: string | undefined | null): Date | null {
  if (!ymd) return null;
  const parts = ymd.split("-");
  if (parts.length !== 3) return null;
  const [y, m, d] = parts.map(Number);
  if (!y || !m || !d) return null;
  return new Date(Date.UTC(y, m - 1, d));
}

function getDayAndDateLabel(ymd: string) {
  const d = parseYmd(ymd);
  if (!d) return { day: "", dateLabel: ymd, shortDate: ymd };
  const day = new Intl.DateTimeFormat("he-IL", {
    weekday: "long",
    timeZone: "Asia/Jerusalem",
  }).format(d);
  const dateLabel = new Intl.DateTimeFormat("he-IL", {
    year: "numeric",
    month: "long",
    day: "numeric",
    timeZone: "Asia/Jerusalem",
  }).format(d);
  const shortDate = new Intl.DateTimeFormat("he-IL", {
    day: "2-digit",
    month: "2-digit",
    timeZone: "Asia/Jerusalem",
  }).format(d);
  return { day, dateLabel, shortDate };
}

const STATUS_LABELS: RegistrationStatusFilter[] = [
  "All",
  "Pending",
  "Approved",
  "Rejected",
  "Cancelled",
];

const statusConfig: Record<
  string,
  { label: string; color: string; bg: string }
> = {
  All: { label: "הכל", color: "text-slate-600", bg: "bg-slate-100" },
  Pending: { label: "ממתינים", color: "text-amber-700", bg: "bg-amber-50" },
  Approved: { label: "אושרו", color: "text-emerald-700", bg: "bg-emerald-50" },
  Rejected: { label: "נדחו", color: "text-rose-700", bg: "bg-rose-50" },
  Cancelled: { label: "בוטלו", color: "text-slate-500", bg: "bg-slate-50" },
};

export default function AdminRegistrationsPage() {
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

  const prevWeek = () =>
    setPivot(d => new Date(d.getTime() - 7 * 24 * 60 * 60 * 1000));
  const nextWeek = () =>
    setPivot(d => new Date(d.getTime() + 7 * 24 * 60 * 60 * 1000));
  const thisWeek = () => setPivot(startOfToday());

  const dateRangeLabel = useMemo(() => {
    const fromLabel = getDayAndDateLabel(from).shortDate;
    const toLabel = getDayAndDateLabel(to).shortDate;
    return `${fromLabel} - ${toLabel}`;
  }, [from, to]);

  // פונקציית עזר לשינוי סטטוס שמבטיחה איפוס ויזואלי מהיר (אם ה-hook לא עושה את זה לבד)
  const handleStatusChange = (newStatus: RegistrationStatusFilter) => {
    if (newStatus === status) return;
    changeStatus(newStatus);
    // ה-Hook שלך אמור לטפל בטעינה, אבל ה-UI יגיב ל-loadingNames
  };

  return (
    <div dir="rtl" className="min-h-screen bg-slate-50/50 pb-20">
      <div className="mx-auto max-w-4xl p-4 space-y-4">
        {/* --- כותרת וכפתור חזרה --- */}
        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <h1 className="text-xl font-extrabold text-slate-900">
              ניהול שיבוצים
            </h1>
            <Link
              to="/admin"
              className="text-sm text-slate-500 hover:text-sky-600 flex items-center gap-1"
            >
              <Icons.Back /> חזרה
            </Link>
          </div>

          {/* שורת ניווט שבועי */}
          <div className="flex items-center justify-between bg-white p-2 rounded-xl shadow-sm border border-slate-200">
            <button
              onClick={prevWeek}
              className="p-2 text-slate-500 hover:bg-slate-50 rounded-lg"
            >
              <Icons.ChevronRight />
            </button>

            <div className="flex flex-col items-center">
              <span className="text-[10px] text-slate-400 font-medium">
                שבוע נוכחי
              </span>
              <span className="text-sm font-bold text-slate-800">
                {dateRangeLabel}
              </span>
            </div>

            <button
              onClick={nextWeek}
              className="p-2 text-slate-500 hover:bg-slate-50 rounded-lg"
            >
              <Icons.ChevronLeft />
            </button>

            <div className="border-r border-slate-200 h-6 mx-1"></div>

            <button
              onClick={thisWeek}
              className="text-xs font-bold text-sky-600 bg-sky-50 px-3 py-1.5 rounded-lg"
            >
              היום
            </button>
            <button
              onClick={reloadSnapshot}
              className="mr-1 p-2 text-slate-400 hover:text-sky-600"
              title="רענן נתונים"
            >
              <Icons.Refresh />
            </button>
          </div>
        </div>

        {/* --- הצגת שגיאות כלליות --- */}
        {error && (
          <div className="bg-red-50 text-red-600 p-3 rounded-xl text-sm border border-red-100">
            שגיאה: {error}
          </div>
        )}

        {/* --- סרגל ימים (גלילה אופקית) --- */}
        <section>
          {!loadingSnapshot && daysPills.length > 0 && (
            <div className="flex overflow-x-auto gap-2 pb-2 hide-scrollbar snap-x">
              {daysPills.map(d => {
                const { day, shortDate } = getDayAndDateLabel(d.date);
                const isSelected = d.isSelected;
                return (
                  <button
                    key={d.date}
                    onClick={() => selectDay(d.date)}
                    className={`
                      snap-center shrink-0 w-[22%] min-w-[80px] flex flex-col items-center justify-center p-3 rounded-xl border transition-all
                      ${
                        isSelected
                          ? "bg-sky-600 border-sky-600 text-white shadow-md scale-105"
                          : "bg-white border-slate-200 text-slate-600"
                      }
                    `}
                  >
                    <span
                      className={`text-[10px] ${
                        isSelected ? "text-sky-100" : "text-slate-400"
                      }`}
                    >
                      {day}
                    </span>
                    <span className="text-sm font-bold">{shortDate}</span>
                  </button>
                );
              })}
            </div>
          )}
          {!loadingSnapshot && daysPills.length === 0 && (
            <div className="text-center text-slate-500 py-4 bg-white rounded-xl border border-dashed">
              אין ימים להצגה
            </div>
          )}
        </section>

        {/* --- פילטרים וסטטיסטיקה --- */}
        <div className="space-y-4">
          {/* פילטרים - גלילה אופקית */}
          <div className="bg-white rounded-xl border border-slate-200 p-2 shadow-sm">
            <div className="flex overflow-x-auto gap-2 hide-scrollbar">
              {STATUS_LABELS.map(s => {
                const config = statusConfig[s] || statusConfig.All;
                const isActive = status === s;
                return (
                  <button
                    key={s}
                    onClick={() => handleStatusChange(s)} // שימוש בפונקציה העוטפת
                    className={`
                                            whitespace-nowrap flex-shrink-0 px-3 py-1.5 rounded-lg text-xs font-bold transition-all
                                            ${
                                              isActive
                                                ? "bg-slate-800 text-white shadow-sm"
                                                : "bg-slate-50 text-slate-600 border border-slate-100"
                                            }
                                        `}
                  >
                    {config.label}
                  </button>
                );
              })}
            </div>
          </div>

          {/* סטטיסטיקה */}
          {selectedDaySummary && (
            <div className="bg-white rounded-xl border border-slate-200 p-3 shadow-sm">
              <div className="flex justify-between items-center mb-2">
                <span className="text-xs font-bold text-slate-400">
                  סיכום יום
                </span>
                <span className="text-[10px] bg-slate-100 px-2 py-0.5 rounded text-slate-600">
                  {getDayAndDateLabel(selectedDaySummary.date).shortDate}
                </span>
              </div>
              <div className="grid grid-cols-3 gap-2">
                <StatBox
                  label="נדרשים"
                  value={selectedDaySummary.required}
                  color="bg-slate-100 text-slate-700"
                />
                <StatBox
                  label="נרשמו"
                  value={selectedDaySummary.pending}
                  color="bg-sky-50 text-sky-700"
                />
                <StatBox
                  label="ממתינים"
                  value={selectedDaySummary.pending}
                  color="bg-amber-50 text-amber-700"
                />
                <StatBox
                  label="אושרו"
                  value={selectedDaySummary.approved}
                  color="bg-emerald-50 text-emerald-700"
                />
                <StatBox
                  label="נדחו"
                  value={selectedDaySummary.rejected}
                  color="bg-rose-50 text-rose-700"
                />
                <StatBox
                  label="בוטלו"
                  value={selectedDaySummary.cancelled}
                  color="bg-gray-50 text-gray-500"
                />
              </div>
            </div>
          )}

          {/* --- רשימת נרשמים --- */}
          <div className="bg-white rounded-xl border border-slate-200 shadow-sm min-h-[300px] flex flex-col">
            <div className="p-3 border-b border-slate-100 flex items-center justify-between bg-slate-50/50 rounded-t-xl">
              <div className="flex items-center gap-2">
                <h2 className="text-sm font-bold text-slate-800">
                  רשימת נרשמים
                </h2>
                {selectedDate && (
                  <span className="text-xs text-slate-500 font-normal bg-white px-1.5 py-0.5 rounded border border-slate-200">
                    {getDayAndDateLabel(selectedDate).shortDate}
                  </span>
                )}
              </div>

              <button
                onClick={refreshDay}
                disabled={loadingNames}
                className="text-xs flex items-center gap-1 text-sky-600 bg-white border border-sky-100 px-2 py-1 rounded-lg"
              >
                <Icons.Refresh /> רענן
              </button>
            </div>

            <div className="flex-1 p-2">
              {/* טיפול בטעינה: אם טוען, מציג ספינר ומסתיר את הרשימה הישנה */}
              {loadingNames ? (
                <div className="text-center py-10 text-slate-400 text-sm flex flex-col items-center animate-pulse">
                  <Icons.Refresh />
                  <span className="mt-2">טוען נתונים...</span>
                </div>
              ) : (
                // רק אם לא טוען - מציג את הרשימה (ומונע סלט)
                <>
                  {names.length === 0 && (
                    <div className="text-center py-10 text-slate-400 text-sm flex flex-col items-center">
                      <span>אין נרשמים להצגה בסטטוס זה</span>
                    </div>
                  )}

                  <ul className="space-y-2">
                    {names.map(n => {
                      const statusInfo =
                        statusConfig[n.status] || statusConfig.All;

                      const isEarly = n.shiftArrivalType === 2;
                      const arrivalLabel = isEarly
                        ? "הקדמה מוקדמת"
                        : "הגעה רגילה";
                      const arrivalColor = isEarly
                        ? "text-amber-600 font-medium"
                        : "text-slate-400";

                      return (
                        <li
                          key={`${n.employeeId}-${n.shiftDate}-${n.status}`}
                          className="flex items-center justify-between p-2 rounded-lg border border-slate-100 bg-white shadow-sm"
                        >
                          <div className="flex items-center gap-3">
                            <div className="w-9 h-9 rounded-full bg-gradient-to-br from-slate-100 to-slate-200 flex items-center justify-center text-slate-600 font-bold text-xs shadow-inner">
                              {n.firstName[0]}
                              {n.lastName[0]}
                            </div>

                            <div>
                              <div className="font-bold text-slate-800 text-sm">
                                {n.firstName} {n.lastName}
                              </div>
                              <div className="text-[10px] text-slate-500 flex items-center gap-2">
                                <span>{n.shiftDate}</span>
                                <span className="text-slate-300">•</span>
                                <span className={arrivalColor}>
                                  {arrivalLabel}
                                </span>
                              </div>
                            </div>
                          </div>

                          <span
                            className={`px-2 py-0.5 rounded text-[10px] font-bold border ${statusInfo.bg} ${statusInfo.color} border-transparent`}
                          >
                            {statusInfo.label}
                          </span>
                        </li>
                      );
                    })}
                  </ul>

                  {hasMore && (
                    <button
                      onClick={loadMore}
                      className="w-full mt-4 py-3 text-sm font-medium text-slate-600 bg-slate-50 rounded-xl active:bg-slate-100"
                    >
                      טען עוד
                    </button>
                  )}
                </>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// קומפוננטה קטנה לסטטיסטיקה
function StatBox({
  label,
  value,
  color,
}: {
  label: string;
  value: number;
  color: string;
}) {
  return (
    <div
      className={`flex flex-col items-center justify-center p-2 rounded-lg ${color} bg-opacity-60 border border-white/50`}
    >
      <span className="text-lg font-bold leading-none">{value}</span>
      <span className="text-[10px] opacity-80 mt-1">{label}</span>
    </div>
  );
}
