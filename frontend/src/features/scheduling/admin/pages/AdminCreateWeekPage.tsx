import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  adminSchedulingApi,
  type ShiftSummaryDto,
} from "../api/adminSchedulingApi";
import { 
  CalendarPlus, 
  ArrowRight, 
  Clock, 
  Type, 
  AlignLeft, 
  Users, 
  Lock,
  Check,
  Calendar,
  Trash2,
  AlertTriangle,
  Save,
  Minus,
  Plus,
  Loader2
} from "lucide-react";

/* --- פונקציות עזר (ללא שינוי) --- */

type DayCfg = {
  active: boolean;
  date: string;
  time: string;
  name: string;
  required: number;
  minimum: number;
  earlyMin: number;
  skill: number;
  description: string;
};

const HEB_DAYS = ["ראשון", "שני", "שלישי", "רביעי", "חמישי"];

function nextSunday(d = new Date()) {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  const dow = x.getDay(); 
  const daysToNextSunday = (7 - dow) % 7 || 7;
  x.setDate(x.getDate() + daysToNextSunday);
  return x;
}

function fmtDate(d: Date) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${dd}`;
}

function nextDate(date: string): string {
  const d = new Date(date + "T00:00:00");
  d.setDate(d.getDate() + 1);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${dd}`;
}

function toUtcIso(date: string, time: string) {
  const [h, m] = time.split(":").map(Number);
  const d = new Date(date + "T00:00:00");
  d.setHours(h ?? 0, m ?? 0, 0, 0);
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString();
}

function shiftToDateIso(shift: ShiftSummaryDto): string {
  return shift.startTime.substring(0, 10);
}

/* --- רכיבים מעוצבים --- */

type NumberStepperProps = {
  label: string;
  value: number;
  onChange: (value: number) => void;
  min: number;
  max: number;
  disabled?: boolean;
};

function NumberStepper({ label, value, onChange, min, max, disabled }: NumberStepperProps) {
  const dec = () => { if (!disabled && value > min) onChange(value - 1); };
  const inc = () => { if (!disabled && value < max) onChange(value + 1); };

  return (
    <div>
      <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1.5">
        {label}
      </label>
      <div className={`flex items-center justify-between rounded-xl border p-1 bg-white transition-all
        ${disabled ? "border-slate-200 bg-slate-50 opacity-60" : "border-slate-200 focus-within:border-blue-400 focus-within:ring-2 focus-within:ring-blue-100"}`}>
        
        <button
          type="button"
          onClick={dec}
          disabled={disabled || value <= min}
          className="h-8 w-8 flex items-center justify-center rounded-lg bg-slate-100 text-slate-600 hover:bg-slate-200 disabled:opacity-50 disabled:hover:bg-slate-100 transition-colors"
        >
          <Minus size={14} />
        </button>
        
        <span className="font-bold text-slate-800 w-8 text-center">{value}</span>
        
        <button
          type="button"
          onClick={inc}
          disabled={disabled || value >= max}
          className="h-8 w-8 flex items-center justify-center rounded-lg bg-blue-50 text-blue-600 hover:bg-blue-100 disabled:opacity-50 disabled:hover:bg-blue-50 transition-colors"
        >
          <Plus size={14} />
        </button>
      </div>
    </div>
  );
}

export default function AdminCreateWeekPage() {
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
  const [existingByDate, setExistingByDate] = useState<Record<string, ShiftSummaryDto | undefined>>({});
  const [loadingExisting, setLoadingExisting] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [deletingShiftId, setDeletingShiftId] = useState<string | null>(null);

  const activeCount = days.filter(d => d.active && !existingByDate[d.date]).length;

  useEffect(() => {
    async function load() {
      try {
        setLoadingExisting(true);
        setErrorMsg(null);
        const start = defaults[0];
        const endExclusive = nextDate(defaults[defaults.length - 1]);
        const shifts = await adminSchedulingApi.getShiftsInRange(start, endExclusive);

        const map: Record<string, ShiftSummaryDto> = {};
        for (const s of shifts) {
          const iso = shiftToDateIso(s);
          if (!map[iso]) map[iso] = s;
        }
        setExistingByDate(map);
        setDays((prev) => prev.map((d) => (map[d.date] ? { ...d, active: false } : d)));
      } catch (err) {
        console.error("שגיאה בטעינת משמרות קיימות:", err);
        setErrorMsg("לא הצלחנו לבדוק את הימים. השרת עדיין מגן מפני כפילות.");
      } finally {
        setLoadingExisting(false);
      }
    }
    load();
  }, [defaults]);

  function update(idx: number, patch: Partial<DayCfg>) {
    setDays((prev) => prev.map((d, i) => (i === idx ? { ...d, ...patch } : d)));
  }

  function clearCurrent() {
    const cur = days[selected];
    if (existingByDate[cur.date]) return;
    setDays((prev) =>
      prev.map((d, i) =>
        i === selected
          ? { ...d, active: false, time: "16:30", name: "", required: 1, minimum: 0, earlyMin: 0, skill: 1, description: "" }
          : d
      )
    );
  }

  function handleDateChange(newDate: string) {
    if (!newDate) return;
    const cur = days[selected];
    if (existingByDate[cur.date]) return;
    if (existingByDate[newDate]) {
      alert("כבר קיימת משמרת בתאריך שנבחר. בחר תאריך אחר.");
      return;
    }
    setDays((prev) => prev.map((d, i) => (i === selected ? { ...d, date: newDate } : d)));
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
      setDays((prev) => prev.map((d) => d.date === date ? { ...d, active: false } : d));
      setSuccessMsg("המשמרת בוטלה בהצלחה.");
    } catch (err) {
      console.error(err);
      setErrorMsg("שגיאה בביטול המשמרת.");
    } finally {
      setDeletingShiftId(null);
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setErrorMsg(null);
    setSuccessMsg(null);

    if (activeCount === 0) {
      setErrorMsg("בחר לפחות יום אחד פנוי ויצר משמרת.");
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
        setDays((prev) => prev.map((d) => nextExisting[d.date] ? { ...d, active: false } : d));
        setSuccessMsg(`נוצרו ${created} משמרות בהצלחה!`);
      } else {
        setErrorMsg("לא נוצרו משמרות. ייתכן שהימים תפוסים.");
      }
    } catch (err) {
      console.error(err);
      setErrorMsg("שגיאה כללית ביצירת משמרות.");
    } finally {
      setSubmitting(false);
    }
  }

  const cur = days[selected];
  const lockedShift = existingByDate[cur.date];
  const isLocked = !!lockedShift;

  return (
    <div dir="rtl" className="min-h-screen bg-slate-50/50 pb-20 font-sans text-slate-900">
      <div className="mx-auto max-w-5xl px-4 py-8 sm:px-6 lg:px-8">
        
        {/* === Header: Title Only === */}
        <div className="mb-2">
          <h1 className="text-3xl font-black text-slate-900 tracking-tight flex items-center gap-3">
            <div className="p-2 bg-blue-100 rounded-xl text-blue-600">
              <CalendarPlus size={24} />
            </div>
            יצירת שבוע עבודה
          </h1>
          <p className="text-slate-500 mt-2 text-lg">
            הגדרת משמרות מרוכזת לימים ראשון עד חמישי.
          </p>
        </div>

        {/* Loading Indicator */}
        {loadingExisting && (
          <div className="mb-6 flex items-center gap-3 text-sm text-blue-700 bg-white p-4 rounded-xl border border-blue-100 shadow-sm animate-in fade-in">
            <Loader2 size={20} className="animate-spin text-blue-600" />
            <span className="font-medium">בודק נתונים על משמרות קיימות מול השרת...</span>
          </div>
        )}

        {/* === Flex Container: Tabs (Right) + Back Button (Left) === */}
        <div className="flex flex-col-reverse md:flex-row md:items-center justify-between gap-4 mb-2">
            
            {/* Tabs Area */}
            <div className="overflow-x-auto py-4 px-4 -mx-4 scrollbar-hide flex-1">
              <div className="flex gap-3 min-w-max">
                {days.map((d, idx) => {
                  const locked = !!existingByDate[d.date];
                  const isActiveTab = idx === selected;
                  
                  return (
                    <button
                      key={d.date}
                      type="button"
                      onClick={() => setSelected(idx)}
                      className={`
                        relative flex flex-col items-center justify-center w-24 h-20 sm:w-28 sm:h-24 rounded-2xl border-2 transition-all duration-200
                        ${isActiveTab 
                          ? "border-blue-600 bg-blue-50/50 shadow-md scale-105 z-10" 
                          : "border-slate-100 bg-white hover:border-blue-200 hover:shadow-sm"
                        }
                        ${locked ? "opacity-75 bg-slate-50/50" : ""}
                      `}
                    >
                      <div className="absolute top-2 right-2">
                        {locked ? (
                          <Lock size={14} className="text-slate-400" />
                        ) : d.active ? (
                          <div className="bg-emerald-500 rounded-full p-0.5">
                            <Check size={10} className="text-white" />
                          </div>
                        ) : null}
                      </div>

                      <span className={`text-sm font-bold mb-1 ${isActiveTab ? "text-blue-700" : "text-slate-700"}`}>
                        {HEB_DAYS[idx]}
                      </span>
                      <span className="text-xs text-slate-500 font-medium font-mono dir-ltr">
                        {d.date.substring(5)}
                      </span>
                      
                      {isActiveTab && (
                        <div className="absolute -bottom-2.5 w-3 h-3 bg-blue-600 rotate-45 border-b border-r border-blue-600"></div>
                      )}
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Desktop Back Button (שמאל למחשב) */}
            <div className="hidden md:block pl-2">
               <Link 
                to="/admin" 
                className="flex items-center gap-2 rounded-xl bg-white border border-slate-200 px-4 py-2.5 text-xs font-bold text-slate-600 hover:text-slate-900 hover:border-slate-300 hover:shadow-md transition-all group"
               >
                 <ArrowRight size={14} className="text-slate-400 group-hover:text-slate-800 transition-colors" />
                 חזרה לניהול
               </Link>
            </div>
        </div>
        
        {/* Mobile Back Button (שמאל למובייל) */}
        {/* הוספתי justify-end כדי להצמיד אותו לשמאל ב-RTL */}
        <div className="md:hidden mb-2 flex justify-end">
           <Link 
            to="/admin" 
            className="flex items-center gap-2 text-sm font-bold text-slate-500 hover:text-slate-800 transition-colors bg-white px-3 py-1.5 rounded-lg border border-slate-200 shadow-sm"
           >
             <ArrowRight size={16} />
             חזרה לניהול
           </Link>
        </div>

        {/* === Main Form Card === */}
        <div className="rounded-3xl border border-slate-200 bg-white shadow-xl shadow-slate-200/50 overflow-hidden relative transition-all duration-300">
          
          {/* Top Status Bar */}
          <div className={`
             px-4 py-4 sm:px-6 sm:py-5 border-b flex flex-col sm:flex-row sm:items-center justify-between gap-4
             ${isLocked ? "bg-slate-50 border-slate-200" : "bg-white border-slate-100"}
          `}>
             <div className="flex items-start sm:items-center gap-3">
                <div className={`p-2.5 rounded-xl shrink-0 ${isLocked ? "bg-red-100 text-red-600" : "bg-blue-100 text-blue-600"}`}>
                   {isLocked ? <Lock size={20} /> : <Calendar size={20} />}
                </div>
                <div>
                   <h2 className="font-bold text-slate-800 text-lg flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-2 leading-tight">
                     <span>{HEB_DAYS[selected]}</span>
                     <span className="hidden sm:inline text-slate-300">|</span>
                     {/* כאן השינוי: פורמט תאריך הפוך לתצוגה */}
                     <span className="text-base sm:text-lg dir-ltr text-right sm:text-left font-mono text-slate-600">
                       {cur.date.split('-').reverse().join('/')}
                     </span>
                   </h2>
                   <p className="text-xs text-slate-500 mt-1">
                      {isLocked ? "יום זה נעול (קיימת משמרת)" : "הגדרת פרטי משמרת ליום זה"}
                   </p>
                </div>
             </div>

             {isLocked && lockedShift && (
                <button
                  type="button"
                  onClick={() => handleDeleteShiftForDate(cur.date)}
                  disabled={deletingShiftId === lockedShift.id}
                  className="w-full sm:w-auto flex items-center justify-center gap-2 text-xs font-bold text-red-600 bg-white sm:bg-red-50 px-4 py-2 rounded-xl border border-red-100 hover:bg-red-50 sm:hover:bg-red-100 transition-colors shadow-sm sm:shadow-none"
                >
                  <Trash2 size={14} />
                  {deletingShiftId === lockedShift.id ? "מבטל..." : "בטל משמרת ליום זה"}
                </button>
             )}
          </div>

          {/* Form Content */}
          <form onSubmit={handleSubmit} className="p-4 sm:p-6 space-y-6 sm:space-y-8">
            {/* שאר הקוד זהה לקודם */}
            
            {/* Toggle Active Switch */}
            <div className="flex items-center justify-between bg-slate-50 p-4 rounded-xl border border-slate-100">
              <label className="flex items-center gap-4 cursor-pointer">
                 <div className="relative">
                    <input 
                      type="checkbox" 
                      className="sr-only peer"
                      checked={cur.active && !isLocked}
                      onChange={(e) => {
                        if (isLocked) return;
                        update(selected, { active: e.target.checked });
                      }}
                      disabled={isLocked}
                    />
                    <div className="w-11 h-6 bg-slate-300 peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-blue-100 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                 </div>
                 <span className={`font-bold text-sm ${cur.active && !isLocked ? "text-slate-800" : "text-slate-400"}`}>
                    {cur.active ? "משמרת פעילה" : "אין משמרת ביום זה"}
                 </span>
              </label>

              {!isLocked && cur.active && (
                <button
                  type="button"
                  onClick={clearCurrent}
                  className="text-xs font-medium text-slate-500 hover:text-red-600 transition-colors"
                >
                  אפס נתונים
                </button>
              )}
            </div>

            {/* Form Fields */}
            <div className={`space-y-6 sm:space-y-8 transition-all duration-300 ${(!cur.active && !isLocked) ? "opacity-30 pointer-events-none grayscale" : "opacity-100"}`}>
              
              {/* Row 1: Name & Time */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 sm:gap-6">
                 <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">שם המשמרת</label>
                    <div className="relative">
                       <input
                          className="block w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 pl-10 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition-all disabled:bg-slate-50 disabled:text-slate-400"
                          value={cur.name}
                          onChange={(e) => update(selected, { name: e.target.value })}
                          placeholder="למשל: אירוע ערב"
                          disabled={isLocked || !cur.active}
                       />
                       <Type className="absolute left-3 top-2.5 text-slate-400" size={18} />
                    </div>
                 </div>

                 <div className="grid grid-cols-2 gap-3 sm:gap-4">
                    <div>
                      <label className="block text-sm font-semibold text-slate-700 mb-1.5">שעת התחלה</label>
                      <div className="relative">
                         <input
                            type="time"
                            className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 pl-8 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition-all disabled:bg-slate-50 disabled:text-slate-400"
                            value={cur.time}
                            onChange={(e) => update(selected, { time: e.target.value })}
                            disabled={isLocked || !cur.active}
                         />
                         <Clock className="absolute left-2.5 top-2.5 text-slate-400" size={16} />
                      </div>
                    </div>

                    <div>
                      <label className="block text-sm font-semibold text-slate-700 mb-1.5">תאריך</label>
                      <div className="relative">
                         <input
                            type="date"
                            className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 pl-8 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition-all disabled:bg-slate-50 disabled:text-slate-400"
                            value={cur.date}
                            onChange={(e) => handleDateChange(e.target.value)}
                            disabled={isLocked} 
                         />
                      </div>
                    </div>
                 </div>
              </div>

              {/* Row 2: Staffing */}
              <div className="rounded-xl border border-slate-100 bg-slate-50/50 p-4 sm:p-5">
                 <h3 className="text-sm font-bold text-slate-800 flex items-center gap-2 mb-4">
                    <Users size={16} className="text-emerald-500"/>
                    הגדרות כוח אדם
                 </h3>
                 <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6">
                    <NumberStepper
                      label="כמות נדרשת"
                      value={cur.required}
                      onChange={(val) => update(selected, { required: val })}
                      min={1} max={30}
                      disabled={isLocked || !cur.active}
                    />
                    <NumberStepper
                      label="מינימום"
                      value={cur.minimum}
                      onChange={(val) => update(selected, { minimum: val })}
                      min={0} max={30}
                      disabled={isLocked || !cur.active}
                    />
                    <NumberStepper
                      label="מוקדמים"
                      value={cur.earlyMin}
                      onChange={(val) => update(selected, { earlyMin: val })}
                      min={0} max={10}
                      disabled={isLocked || !cur.active}
                    />
                    <NumberStepper
                      label="רמת מיומנות (1-10)"
                      value={cur.skill}
                      onChange={(val) => update(selected, { skill: val })}
                      min={1} max={10}
                      disabled={isLocked || !cur.active}
                    />
                 </div>
              </div>

              {/* Row 3: Description */}
              <div>
                 <label className="block text-sm font-semibold text-slate-700 mb-1.5">תיאור והערות</label>
                 <div className="relative">
                    <textarea
                      className="block w-full rounded-xl border border-slate-200 bg-white px-4 py-3 pl-10 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition-all resize-none disabled:bg-slate-50 disabled:text-slate-400"
                      rows={3}
                      value={cur.description}
                      onChange={(e) => update(selected, { description: e.target.value })}
                      placeholder="הוסף הערות חשובות למשמרת..."
                      disabled={isLocked || !cur.active}
                    />
                    <AlignLeft className="absolute left-3 top-3 text-slate-400" size={18} />
                 </div>
              </div>

            </div>

            {/* Error Message */}
            {errorMsg && (
               <div className="flex items-start gap-3 p-4 bg-red-50 border border-red-100 rounded-xl text-red-700 text-sm animate-in slide-in-from-top-2">
                  <AlertTriangle size={18} className="shrink-0 mt-0.5" />
                  <div>{errorMsg}</div>
               </div>
            )}

            {/* Footer Actions */}
            <div className="pt-4 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-4">
               <div className="text-sm text-slate-500">
                  <span className="font-bold text-slate-800">{activeCount}</span> ימים מסומנים ליצירה
               </div>

               <button
                  disabled={submitting || activeCount === 0}
                  className={`
                     w-full sm:w-auto inline-flex justify-center items-center gap-2 rounded-xl px-8 py-3 text-sm font-bold text-white shadow-lg transition-all active:scale-95
                     ${submitting || activeCount === 0
                        ? "bg-slate-300 shadow-none cursor-not-allowed" 
                        : "bg-blue-600 hover:bg-blue-700 shadow-blue-500/30"
                     }
                  `}
               >
                  {submitting ? (
                     "מעבד נתונים..."
                  ) : (
                     <>
                       <Save size={18} />
                       צור {activeCount} משמרות
                     </>
                  )}
               </button>
            </div>

          </form>
        </div>

        {/* Success Modal */}
        {successMsg && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 backdrop-blur-sm p-4 animate-in fade-in">
            <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-2xl text-center transform scale-100 animate-in zoom-in-95">
              <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100 text-emerald-600">
                <Check size={32} />
              </div>
              <h2 className="text-xl font-bold text-slate-900 mb-2">
                מעולה!
              </h2>
              <p className="text-slate-500 mb-6">
                {successMsg}
              </p>
              <button
                onClick={() => setSuccessMsg(null)}
                className="w-full rounded-xl bg-slate-900 px-4 py-3 text-sm font-bold text-white hover:bg-slate-800 transition shadow-lg shadow-slate-200"
              >
                סגור וחזור
              </button>
            </div>
          </div>
        )}

      </div>
    </div>
  );
}