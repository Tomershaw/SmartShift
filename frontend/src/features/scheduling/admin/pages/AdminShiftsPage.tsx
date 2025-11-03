// src/features/scheduling/admin/pages/AdminShiftsPage.tsx
import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import WeekGrid from "../components/assignments/WeekGrid";
import type { WeeklyAssignment } from "../types/assignments";
import { useLoading } from "../../../appLoading/context/useLoading";
import { adminSchedulingApi } from "../api/adminSchedulingApi";
import { mapProcessToWeekly } from "../utils/mapProcessToWeekly";
import type { ProcessShiftServerItem } from "../api/adminSchedulingApi";

/* ---------- תאריכים ---------- */
function nextSunday(d = new Date()) {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  const dow = x.getDay(); // 0 - Sunday
  const daysToNextSunday = (7 - dow) % 7 || 7; // תמיד הבא
  x.setDate(x.getDate() + daysToNextSunday);
  return x;
}
function toISO(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(
    2,
    "0"
  )}-${String(date.getDate()).padStart(2, "0")}`;
}

/* ---------- עזרי זמן בטוחים ---------- */
function normalizeDateLike(input?: string | null): string | null {
  if (!input) return null;
  let s = String(input).trim();
  if (s.includes(" ") && !s.includes("T")) s = s.replace(" ", "T");
  // לקצר שבר שניות ארוך ל-3 ספרות
  s = s.replace(/\.(\d{3})\d+([+-]\d{2}:\d{2}|Z)?$/, ".$1$2");
  return s;
}
function safeDate(dateLike?: string | null): Date | null {
  const norm = normalizeDateLike(dateLike);
  if (!norm) return null;
  const d = new Date(norm);
  return Number.isNaN(d.getTime()) ? null : d;
}
function toIsoIL(dateLike?: string | null): string | null {
  const d = safeDate(dateLike);
  if (!d) return null;
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Jerusalem",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(d);
}

/* ---------- קאש ב-sessionStorage ---------- */
const CACHE_KEY = "admin_shifts_cache_v1";
type CachePayload = {
  range: { start: string; end: string };
  processed: ProcessShiftServerItem[];
  weekly: WeeklyAssignment;
};

function saveCache(payload: CachePayload) {
  try {
    sessionStorage.setItem(CACHE_KEY, JSON.stringify(payload));
  } catch (err) {
    console.warn("saveCache failed", err);
  }
}
function loadCache(): CachePayload | null {
  try {
    const raw = sessionStorage.getItem(CACHE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as CachePayload;
    if (!parsed?.weekly || !parsed?.processed) return null;
    return parsed;
  } catch (err) {
    console.warn("loadCache failed", err);
    return null;
  }
}
function clearCache() {
  try {
    sessionStorage.removeItem(CACHE_KEY);
  } catch (err) {
    console.warn("clearCache failed", err);
  }
}

export default function AdminShiftsPage() {
  const [grid, setGrid] = useState<WeeklyAssignment | null>(null);
  const [processed, setProcessed] = useState<ProcessShiftServerItem[] | null>(
    null
  );
  const [confirming, setConfirming] = useState(false);
  const { show, hide } = useLoading();
  const [searchParams, setSearchParams] = useSearchParams();

  // טווח ברירת מחדל - שבוע הבא: ראשון עד חמישי
  const defaultStart = useMemo(() => toISO(nextSunday()), []);
  const defaultEnd = useMemo(() => {
    const t = new Date(defaultStart);
    t.setDate(t.getDate() + 4);
    return toISO(t);
  }, [defaultStart]);

  // טווח מה-URL אם יש
  const startISO = searchParams.get("start") || defaultStart;
  const endISO = searchParams.get("end") || defaultEnd;

  // ריהיידרציה מהקאש כשחוזרים לעמוד
  useEffect(() => {
    const cache = loadCache();
    if (!cache) return;
    if (cache.range.start === startISO && cache.range.end === endISO) {
      setProcessed(cache.processed);
      setGrid(cache.weekly); // כולל השינויים בגרירה
    }
  }, [startISO, endISO]);

  // שמירה אוטומטית בקאש בכל שינוי של grid
  useEffect(() => {
    if (!grid || !processed) return;
    saveCache({
      range: { start: startISO, end: endISO },
      processed,
      weekly: grid,
    });
  }, [grid, processed, startISO, endISO]);

  async function handleCreateWeekShifts() {
    show("טוען נתונים מהשרת...");
    try {
      setSearchParams({ start: startISO, end: endISO });

      const data = await adminSchedulingApi.processShifts({
        startDate: startISO,
        endDate: endISO,
      });

      if (Array.isArray(data)) {
        const items: ProcessShiftServerItem[] = data;
        const weekly = mapProcessToWeekly(items, startISO);

        setProcessed(items);
        setGrid(weekly);

        saveCache({
          range: { start: startISO, end: endISO },
          processed: items,
          weekly,
        });
        console.log("mapped to WeeklyAssignment:", weekly);
      } else {
        alert(data.message || "אין משמרות בטווח");
        setGrid(null);
        setProcessed(null);
        clearCache();
      }
    } catch (err) {
      console.error("שגיאה ביצירת משמרות:", err);
      alert("שגיאה ביצירת משמרות מהשרת. בדוק לוגים.");
    } finally {
      hide();
    }
  }

  // בונה payload לאישור מתוך מצב WeekGrid ומצליב עם processed כדי להביא shiftId לכל יום
  function buildApprovalsFromGrid(
    currentGrid: WeeklyAssignment,
    items: ProcessShiftServerItem[]
  ): Array<{ shiftId: string; employeeIds: string[] }> {
    // מיפוי: YYYY-MM-DD -> shiftId
    const shiftIdByIso = new Map<string, string>();
    for (const it of items) {
      const iso = toIsoIL(it.startTime);
      if (!iso) continue;
      shiftIdByIso.set(iso, it.shiftId);
    }

    const approvals: Array<{ shiftId: string; employeeIds: string[] }> = [];
    for (const day of currentGrid.days) {
      const shiftId = shiftIdByIso.get(day.iso);
      if (!shiftId) continue;

      const employeeIds = [
        ...day.early.map(e => e.id),
        ...day.regular.map(e => e.id),
      ];

      if (employeeIds.length > 0) {
        approvals.push({ shiftId, employeeIds });
      }
    }
    return approvals;
  }

  async function handleConfirm() {
    if (!grid || !processed) {
      alert("אין נתונים לאישור. צור משמרות קודם.");
      return;
    }

    const approvals = buildApprovalsFromGrid(grid, processed);
    if (approvals.length === 0) {
      alert("אין עובדים מאוכלסים לאישור.");
      return;
    }

    console.log("approvals to send:", approvals);

    show("מאשר שיבוצים...");
    setConfirming(true);
    try {
      let totalApproved = 0;

      for (const a of approvals) {
        const requestedIds = a.employeeIds;
        console.log("→ sending approve payload", {
          shiftId: a.shiftId,
          requestedCount: requestedIds.length,
          requestedIds,
        });

        const res = await adminSchedulingApi.approveShiftEmployees(
          a.shiftId,
          requestedIds
        );

        // 🔎 לוג השוואה פר־Shift
        console.log("← approve result", {
          shiftId: a.shiftId,
          approvedCount: res.approvedCount,
          requestedCount: requestedIds.length,
        });

        if (res.approvedCount !== requestedIds.length) {
          console.warn(
            `Mismatch on shift ${a.shiftId}: requested ${requestedIds.length}, approved ${res.approvedCount}`
          );
        }

        totalApproved += res.approvedCount;
      }

      console.log("✅ total approved", totalApproved);
      alert(`הצלחה. אושרו ${totalApproved} הרשמות Pending.`);
    } catch (e) {
      console.error("❌ שגיאה באישור:", e);
      alert("שגיאה באישור. בדוק לוגים.");
    } finally {
      setConfirming(false);
      hide();
    }
  }

  // קיבוץ פריטי השרת לפי יום ישראלי (לכפתור-כרטיס מתחת לכל יום)
  const processedByDay = useMemo(() => {
    const map = new Map<string, ProcessShiftServerItem[]>();
    if (!processed) return map;
    for (const it of processed) {
      const iso = toIsoIL(it.startTime);
      if (!iso) continue;
      if (!map.has(iso)) map.set(iso, []);
      map.get(iso)!.push(it);
    }
    // מיון לפי "שעה" בתוך כל יום (אם תהיה רלוונטית בהמשך)
    for (const [, arr] of map.entries()) {
      arr.sort((a, b) => {
        const da = safeDate(a.startTime)?.getTime() ?? 0;
        const db = safeDate(b.startTime)?.getTime() ?? 0;
        return da - db;
      });
    }
    return map;
  }, [processed]);

  // כשמשנים את הטבלה בגרירה - נשמור גם לקאש
  function handleGridChange(next: WeeklyAssignment) {
    setGrid(next);
    if (processed) {
      saveCache({
        range: { start: startISO, end: endISO },
        processed,
        weekly: next,
      });
    }
  }

  return (
    <div dir="rtl" className="mx-auto max-w-7xl p-6">
      <header className="mb-4 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-extrabold text-slate-900">
            ניהול משמרות
          </h1>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleCreateWeekShifts}
            className="rounded-xl px-4 py-2 text-sm text-white bg-slate-900 hover:bg-slate-800"
          >
            צור משמרות לשבוע
          </button>

          <button
            onClick={handleConfirm}
            disabled={!grid || confirming}
            className={`rounded-xl px-4 py-2 text-sm text-white ${
              !grid || confirming
                ? "bg-slate-400"
                : "bg-emerald-600 hover:bg-emerald-500"
            }`}
          >
            {confirming ? "מאשר..." : "אשר סופית"}
          </button>

          <Link
            to="/admin"
            className="inline-flex items-center gap-2 rounded-xl border border-sky-200 bg-sky-50 px-4 py-2 text-sm font-medium
                     text-sky-800 hover:bg-sky-100 hover:border-sky-300 shadow-sm transition
                     focus:outline-none focus:ring-2 focus:ring-sky-300"
          >
            <svg
              width="16"  
              height="16"
              viewBox="0 0 24 24"
              className="opacity-80"
            >
              <path fill="currentColor" d="M10 19l-7-7l7-7v4h8v6h-8v4z" />
            </svg>
            חזרה למרכז ניהול
          </Link>
        </div>
      </header>

      <p className="text-slate-600 text-sm mb-2">
        שבוע החל מ-{startISO} עד {endISO}
      </p>

      {!grid && (
        <div className="rounded-xl border border-dashed p-6 text-center text-slate-500">
          לחץ על "צור משמרות לשבוע" כדי לטעון מהשרת
        </div>
      )}

      {grid && <WeekGrid value={grid} onChange={handleGridChange} />}

      {/* כרטיסי תקציר לבנים, קטנים ומתחת לכל עמודת יום - ללא שעה */}
      {grid && (
        <section className="mt-3">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-5">
            {grid.days.map(day => {
              const items = processedByDay.get(day.iso) ?? [];
              return (
                <div
                  key={`summary-${day.iso}`}
                  className="flex flex-col items-stretch"
                >
                  {items.length > 0 ? (
                    <div className="flex flex-col gap-3">
                      {items.map(it => {
                        const meets = it.meetsMinimumEarly;
                        const date = toIsoIL(it.startTime) || day.iso;
                        return (
                          <Link
                            key={it.shiftId}
                            to={`/admin/shifts/${it.shiftId}/summary?start=${startISO}&end=${endISO}`}
                            state={{ item: it }}
                            className="block rounded-xl border border-slate-200 bg-white p-3 text-start shadow-sm hover:shadow-md transition-shadow"
                          >
                            {/* Header row: status + date בלבד */}
                            <div className="flex items-center justify-between">
                              <span
                                className={`text-[11px] px-2.5 py-1 rounded-full border ${
                                  meets
                                    ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                                    : "bg-amber-50 text-amber-700 border-amber-200"
                                }`}
                              >
                                {meets ? "עומד במינימום Early" : "חסר מוקדמים"}
                              </span>

                              <div className="flex items-center gap-2 text-xs text-slate-700">
                                <span>{date}</span>
                                <svg
                                  width="14"
                                  height="14"
                                  viewBox="0 0 24 24"
                                  className="opacity-60"
                                >
                                  <path
                                    fill="currentColor"
                                    d="M7 2h2v2h6V2h2v2h3v18H4V4h3V2Zm13 8H4v10h16V10Z"
                                  />
                                </svg>
                              </div>
                            </div>

                            {/* Chips row */}
                            <div className="mt-2 flex flex-wrap gap-1.5">
                              <span className="text-[11px] px-2 py-1 rounded-full bg-slate-100 text-slate-700">
                                דרושים {it.required}
                              </span>
                              <span className="text-[11px] px-2 py-1 rounded-full bg-slate-100 text-slate-700">
                                מינימום {it.minimum}
                              </span>
                              <span className="text-[11px] px-2 py-1 rounded-full bg-slate-100 text-slate-700">
                                מוקדמים {it.plannedEarlyCount}
                              </span>
                              <span className="text-[11px] px-2 py-1 rounded-full bg-slate-100 text-slate-700">
                                רגיל {it.plannedRegularCount}
                              </span>
                            </div>

                            {/* CTA לבן קטן */}
                            <div className="mt-3">
                              <div className="inline-flex items-center justify-center rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700">
                                תקציר
                                <svg
                                  width="14"
                                  height="14"
                                  viewBox="0 0 24 24"
                                  className="ms-1.5"
                                >
                                  <path
                                    fill="currentColor"
                                    d="M9 18l6-6l-6-6v12z"
                                  />
                                </svg>
                              </div>
                            </div>
                          </Link>
                        );
                      })}
                    </div>
                  ) : (
                    <div className="text-center text-xs text-slate-400 py-2 border rounded-lg bg-slate-50">
                      אין תקציר
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </section>
      )}
    </div>
  );
}
