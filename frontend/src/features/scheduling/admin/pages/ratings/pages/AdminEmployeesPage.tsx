// src/features/scheduling/admin/ratings/pages/AdminEmployeesPage.tsx
import { useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useLoading } from "../../../../../appLoading/context/useLoading";
import { schedulingApi } from "../../../../api/schedulingApi"; // הייבוא של ה-API שלך
import type { Employee } from "../../../../types";

export default function AdminEmployeesPage() {
  const { show, hide } = useLoading();
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [query, setQuery] = useState("");
  const [error, setError] = useState<string | null>(null);

  // Infinite scroll controls
  const [visibleCount, setVisibleCount] = useState(10);
  const [loadingMore, setLoadingMore] = useState(false);
  const scrollRef = useRef<HTMLDivElement | null>(null);

  // Load employees once
  useEffect(() => {
    let mounted = true;
    (async () => {
      show("טוען עובדים...");
      try {
        const list = await schedulingApi.getEmployees();
        if (!mounted) return;
        setEmployees(Array.isArray(list) ? list : []);
      } catch (e: unknown) {
        const msg = e instanceof Error ? e.message : String(e);
        setError(msg || "שגיאה בטעינת עובדים");
        setEmployees([]);
      } finally {
        hide();
      }
    })();
    return () => {
      mounted = false;
    };
  }, [show, hide]);

  // --- 🔥 הפונקציה המתוקנת ---
// --- 🔥 הפונקציה המתוקנת עם ה-LOGS ---
const handleDelete = async (employee: Employee) => {
  const confirmed = window.confirm(
    `האם אתה בטוח שברצונך להפוך את ${employee.name} ללא פעיל?\n(העובד יסומן כ"עזב" ולא יופיע בשיבוצים עתידיים)`
  );
  if (!confirmed) return;

  // 👇👇👇 הנה ה-Console Log שאתה צריך להוסיף כאן 👇👇👇
  const idToSend = employee.userId || employee.id;
  
  console.log("🚀 DEBUG DELETE USER:", {
      EmployeeName: employee.name,
      FullEmployeeObject: employee,   // כדי לראות את כל האובייקט
      HasUserId: employee.userId,     // האם קיים userId?
      HasId: employee.id,             // האם קיים id?
      FINAL_ID_BEING_SENT: idToSend   // מה *בפועל* נשלח לשרת
  });
  // ☝️☝️☝️ סוף ה-Log

  try {
    show("מעדכן סטטוס עובד...");

    // שיניתי כאן שישתמש במשתנה שהדפסנו, כדי שנהיה בטוחים ב-100% שמה שהודפס זה מה שנשלח
    await schedulingApi.deleteUser(idToSend);

    // עדכון ה-State המקומי
    setEmployees((prev) =>
      prev.map((emp) =>
        emp.id === employee.id ? { ...emp, isActive: false } : emp
      )
    );
    
    console.log("✅ User deleted successfully in Frontend State");

  } catch (e: unknown) {
    console.error("❌ Error in handleDelete:", e); // גם כאן כדאי שיהיה לוג
    const serverMsg = (e as { response?: { data?: { Message?: string; detail?: string } } }).response?.data?.Message || (e as { response?: { data?: { Message?: string; detail?: string } } }).response?.data?.detail;
    const finalMsg = serverMsg || (e as { message?: string }).message || "שגיאה במחיקת העובד";
    alert(`נכשל: ${finalMsg}`);
  } finally {
    hide();
  }
};

  // Filter by query
  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return employees;
    return employees.filter(
      (e) =>
        (e.name || "").toLowerCase().includes(q) ||
        (e.email || "").toLowerCase().includes(q) ||
        (e.id || "").toLowerCase().includes(q)
    );
  }, [employees, query]);

  // Slice to visible items
  const visibleEmployees = useMemo(
    () => filtered.slice(0, visibleCount),
    [filtered, visibleCount]
  );

  // Reset visible count on search change
  useEffect(() => {
    setVisibleCount(10);
  }, [query]);

  // Scroll handler
  function handleScroll(e: React.UIEvent<HTMLDivElement>) {
    if (loadingMore) return;
    if (visibleCount >= filtered.length) return;

    const el = e.currentTarget;
    const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight;

    if (distanceFromBottom <= 48) {
      setLoadingMore(true);
      setVisibleCount((prev) => Math.min(prev + 10, filtered.length));
      setTimeout(() => setLoadingMore(false), 150);
    }
  }

  // Ensure scrollable
  useEffect(() => {
    function ensureScrollable(tries = 0) {
      const node = scrollRef.current;
      if (!node) return;

      const noScrollbar = node.scrollHeight <= node.clientHeight;
      const canGrow = visibleCount < filtered.length;
      const canRetry = tries < 6;

      if (noScrollbar && canGrow && canRetry) {
        setVisibleCount((prev) => Math.min(prev + 10, filtered.length));
        requestAnimationFrame(() => ensureScrollable(tries + 1));
      }
    }
    requestAnimationFrame(() => ensureScrollable());
  }, [filtered.length, visibleCount]);

  return (
    <div dir="rtl" className="mx-auto max-w-5xl p-6 space-y-6">
      {/* Header */}
      <header className="mb-2 flex items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-extrabold text-slate-900">
            ניהול עובדים
          </h1>
        </div>

        <Link
          to="/admin"
          className="inline-flex items-center gap-2 rounded-xl border border-sky-200 bg-sky-50 px-4 py-2 text-sm font-medium
                     text-sky-800 hover:bg-sky-100 hover:border-sky-300 shadow-sm transition"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" className="opacity-80">
            <path fill="currentColor" d="M10 19l-7-7l7-7v4h8v6h-8v4z" />
          </svg>
          חזרה למרכז ניהול
        </Link>
      </header>

      {/* Search + stats */}
      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="flex items-center gap-3">
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="חפש לפי שם / אימייל / מזהה…"
            className="flex-1 border rounded-lg px-3 py-2 focus:ring-2 focus:ring-sky-500 outline-none"
          />
          <span className="text-xs text-slate-500">
            סה״כ תואמים: {filtered.length} &nbsp;|&nbsp; מוצגים עכשיו: {visibleEmployees.length}
          </span>
        </div>
      </section>

      {/* Error banner */}
      {error && (
        <div className="rounded-2xl border border-amber-200 bg-amber-50 text-amber-800 p-4">
          {error}
        </div>
      )}

      {/* List */}
      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div
          ref={scrollRef}
          onScroll={handleScroll}
          className="h-[65vh] overflow-y-auto pr-1"
        >
          {visibleEmployees.length === 0 ? (
            <div className="flex items-center justify-center h-full text-slate-500 text-sm">
              {filtered.length === 0
                ? employees.length === 0
                  ? "אין נתונים להצגה."
                  : "לא נמצאו תוצאות לחיפוש."
                : "טוען…"}
            </div>
          ) : (
            <>
              <ul className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
                {visibleEmployees.map((e) => {
                  // אם המשתנה לא קיים בכלל, נניח שהעובד פעיל. אם הוא false - הוא לא פעיל.
                  const isActive = e.isActive !== false;

                  return (
                    <li
                      key={e.id}
                      className={`
                        relative border rounded-2xl p-5 shadow-sm transition
                        ${isActive 
                            ? 'bg-white hover:shadow-md border-slate-200' 
                            : 'bg-red-50 border-red-200 shadow-none'} 
                      `}
                    >
                      {/* תגית אדומה אם לא פעיל */}
                      {!isActive && (
                        <div className="absolute top-3 left-3 bg-red-100 text-red-700 text-[10px] font-bold px-2 py-0.5 rounded-full border border-red-200">
                          לא פעיל / עזב
                        </div>
                      )}

                      <div className={`font-semibold ${isActive ? 'text-slate-900' : 'text-slate-500'}`}>
                        {e.name || e.email || "ללא שם"}
                      </div>
                      <div className="text-xs text-slate-500 mt-0.5 break-all">
                        {e.email}
                      </div>

                      <div className="mt-4 flex flex-wrap gap-2">
                        <Link
                          to={`/admin/employees/${e.id}/parameters`}
                          className="inline-flex items-center justify-center rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
                        >
                          עדכון פרמטרים
                        </Link>

                        {/* כפתור הפוך ללא פעיל - מוצג רק אם העובד פעיל */}
                        {isActive ? (
                            <button
                                onClick={() => handleDelete(e)}
                                className="inline-flex items-center justify-center rounded-lg border border-red-200 bg-white px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 hover:border-red-300"
                            >
                                הפוך ללא פעיל
                            </button>
                        ) : (
                             // כפתור חיווי כשהעובד כבר מחוק
                             <span className="inline-flex items-center px-2 text-xs text-red-400 select-none">
                                הוסר מהמערכת
                             </span>
                        )}
                      </div>
                    </li>
                  );
                })}
              </ul>

              {/* Footer */}
              <div className="h-12 flex items-center justify-center gap-3 text-slate-500 text-xs mt-4">
                {visibleCount < filtered.length ? (
                  <>
                    <span>{loadingMore ? "טוען עוד…" : "גלול למטה לטעינת עוד"}</span>
                    <button
                      type="button"
                      onClick={() =>
                        setVisibleCount((prev) => Math.min(prev + 10, filtered.length))
                      }
                      className="px-3 py-1.5 rounded-lg border hover:bg-slate-50"
                    >
                      טען עוד
                    </button>
                  </>
                ) : (
                  <span>הגעת לסוף הרשימה</span>
                )}
              </div>
            </>
          )}
        </div>
      </section>
    </div>
  );
}