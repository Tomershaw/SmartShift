import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { adminSchedulingApi } from "../api/adminSchedulingApi";

type FieldErrors = Record<string, string[]>;

function toUtcIso(date: string, time: string) {
  // date: "YYYY-MM-DD", time: "HH:MM"
  const [h, m] = time.split(":").map(Number);
  const d = new Date(date + "T00:00:00");
  d.setHours(h ?? 0, m ?? 0, 0, 0);
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString(); // ל-UTC
}

export default function AdminCreateShiftPage() {
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [date, setDate] = useState<string>("");
  const [time, setTime] = useState<string>("");
  const [required, setRequired] = useState<number>(1);
  const [minimum, setMinimum] = useState<number>(0);
  const [earlyMin, setEarlyMin] = useState<number>(0);
  const [skill, setSkill] = useState<number>(1);
  const [description, setDescription] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<FieldErrors>({});

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setErrors({});

    try {
      const startTime = toUtcIso(date, time);

      await adminSchedulingApi.createShift({
        name,
        startTime, // ISO-UTC
        requiredEmployeeCount: required,
        minimumEmployeeCount: minimum,
        minimumEarlyEmployees: earlyMin,
        skillLevelRequired: skill,
        description,
      });

      alert("המשמרת נוצרה בהצלחה");
      navigate("/admin/shifts");
    } catch (err: unknown) {
      // ניסיון למפות שגיאות ולידציה מהשרת: { errors: { FieldName: [] } }
      const data = (err as { response?: { data?: { errors?: FieldErrors } } })
        ?.response?.data;
      if (data?.errors && typeof data.errors === "object") {
        setErrors(data.errors as FieldErrors);
      } else {
        alert("שגיאה ביצירת משמרת. בדוק לוגים.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  const err = (k: string) => errors?.[k]?.[0];

  return (
    <div dir="rtl" className="mx-auto max-w-3xl p-6">
      <header className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-extrabold text-slate-900">יצירת משמרת</h1>
        <Link
          to="/admin"
          className="rounded-xl border border-sky-200 bg-sky-50 px-3 py-1.5 text-sm text-sky-800 hover:bg-sky-100"
        >
          חזרה למרכז ניהול
        </Link>
      </header>

      <form
        onSubmit={handleSubmit}
        className="space-y-5 rounded-2xl border p-5 bg-white shadow-sm"
      >
        <div>
          <label className="block text-sm font-medium">שם המשמרת</label>
          <input
            className="mt-1 w-full rounded-xl border px-3 py-2"
            value={name}
            onChange={e => setName(e.target.value)}
            required
          />
          {err("Name") && (
            <p className="text-xs text-red-600 mt-1">{err("Name")}</p>
          )}
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium">תאריך</label>
            <input
              type="date"
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={date}
              onChange={e => setDate(e.target.value)}
              required
            />
            {err("StartTime") && (
              <p className="text-xs text-red-600 mt-1">{err("StartTime")}</p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium">שעה</label>
            <input
              type="time"
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={time}
              onChange={e => setTime(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="block text-sm font-medium">
              רמת מיומנות (1–10)
            </label>
            <input
              type="number"
              min={1}
              max={10}
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={skill}
              onChange={e => setSkill(Number(e.target.value))}
              required
            />
            {err("SkillLevelRequired") && (
              <p className="text-xs text-red-600 mt-1">
                {err("SkillLevelRequired")}
              </p>
            )}
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium">נדרש</label>
            <input
              type="number"
              min={1}
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={required}
              onChange={e => setRequired(Number(e.target.value))}
              required
            />
            {err("RequiredEmployeeCount") && (
              <p className="text-xs text-red-600 mt-1">
                {err("RequiredEmployeeCount")}
              </p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium">מינימום</label>
            <input
              type="number"
              min={0}
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={minimum}
              onChange={e => setMinimum(Number(e.target.value))}
              required
            />
            {err("MinimumEmployeeCount") && (
              <p className="text-xs text-red-600 mt-1">
                {err("MinimumEmployeeCount")}
              </p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium">
              מוקדמים (מינימום)
            </label>
            <input
              type="number"
              min={0}
              className="mt-1 w-full rounded-xl border px-3 py-2"
              value={earlyMin}
              onChange={e => setEarlyMin(Number(e.target.value))}
              required
            />
            {err("MinimumEarlyEmployees") && (
              <p className="text-xs text-red-600 mt-1">
                {err("MinimumEarlyEmployees")}
              </p>
            )}
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium">תיאור</label>
          <textarea
            className="mt-1 w-full rounded-xl border px-3 py-2"
            rows={3}
            value={description}
            onChange={e => setDescription(e.target.value)}
            required
          />
          {err("Description") && (
            <p className="text-xs text-red-600 mt-1">{err("Description")}</p>
          )}
        </div>

        <div className="flex items-center gap-3">
          <button
            disabled={submitting}
            className={`rounded-xl px-4 py-2 text-sm text-white ${
              submitting
                ? "bg-slate-400"
                : "bg-emerald-600 hover:bg-emerald-500"
            }`}
          >
            {submitting ? "שומר..." : "שמור משמרת"}
          </button>
          <Link
            to="/admin/shifts"
            className="rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
          >
            ביטול
          </Link>
        </div>
      </form>
    </div>
  );
}
