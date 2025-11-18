// src/features/scheduling/admin/ratings/pages/AdminEmployeeParametersPage.tsx

import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { employeeParametersApi } from "../api/employeeParametersApi";
import { useLoading } from "../../../../../appLoading/context/useLoading";

// --- Type guards ---
type HttpResponseLike = { status?: number };
type HttpErrorLike = { message?: string; response?: HttpResponseLike };

function isHttpErrorLike(err: unknown): err is HttpErrorLike {
  return (
    typeof err === "object" &&
    err !== null &&
    ("message" in err || "response" in err)
  );
}

// --- Number Stepper (מועתק מהדף השני אחד לאחד) ---
type NumberStepperProps = {
  label: string;
  value: number;
  onChange: (val: number) => void;
  min: number;
  max: number;
};

function NumberStepper({ label, value, onChange, min, max }: NumberStepperProps) {
  const dec = () => {
    if (value > min) onChange(value - 1);
  };

  const inc = () => {
    if (value < max) onChange(value + 1);
  };

  return (
    <div>
      <label className="block text-xs sm:text-sm font-medium text-slate-700 mb-1">
        {label}
      </label>

      <div className="mt-1 flex items-center rounded-xl border px-2 py-1.5 bg-white shadow-sm border-slate-300">
        <button
          type="button"
          onClick={dec}
          disabled={value <= min}
          className="px-2 py-1 text-sm font-semibold text-slate-700 disabled:text-slate-300"
        >
          -
        </button>

        <div className="flex-1 text-center text-sm font-semibold text-slate-900">
          {value}
        </div>

        <button
          type="button"
          onClick={inc}
          disabled={value >= max}
          className="px-2 py-1 text-sm font-semibold text-slate-700 disabled:text-slate-300"
        >
          +
        </button>
      </div>
    </div>
  );
}

// --- Main Page ---
export default function AdminEmployeeParametersPage() {
  const { employeeId = "" } = useParams<{ employeeId: string }>();
  const navigate = useNavigate();
  const { show, hide } = useLoading();

  // ערכים
  const [skillLevel, setSkillLevel] = useState<number>(3); // 1..5
  const [priorityRating, setPriorityRating] = useState<number>(0); // >=0
  const [maxShiftsPerWeek, setMaxShiftsPerWeek] = useState<number>(5); // >=0
  const [adminNotes, setAdminNotes] = useState<string>("");

  const canSubmit = useMemo(() => {
    return (
      !!employeeId &&
      skillLevel >= 1 &&
      skillLevel <= 5 &&
      priorityRating >= 0 &&
      maxShiftsPerWeek >= 0
    );
  }, [employeeId, skillLevel, priorityRating, maxShiftsPerWeek]);

  // Prefill - GET
  useEffect(() => {
    if (!employeeId) return;

    let mounted = true;

    (async () => {
      show("טוען פרמטרים...");
      try {
        const dto = await employeeParametersApi.getParameters(employeeId);
        if (!mounted) return;

        setSkillLevel(dto.skillLevel ?? 3);
        setPriorityRating(dto.priorityRating ?? 0);
        setMaxShiftsPerWeek(dto.maxShiftsPerWeek ?? 5);
        setAdminNotes(dto.adminNotes ?? "");
      } catch (err: unknown) {
        if (isHttpErrorLike(err) && err.response?.status === 404) {
          alert("העובד לא נמצא.");
        } else {
          alert("שגיאה בטעינת נתונים.");
        }
      } finally {
        hide();
      }
    })();

    return () => {
      mounted = false;
    };
  }, [employeeId, show, hide]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!canSubmit) return;

    show("שומר פרמטרים...");

    try {
      const payload = {
        skillLevel,
        priorityRating,
        maxShiftsPerWeek,
        adminNotes: adminNotes.trim() || null,
      };

      const res = await employeeParametersApi.updateParameters(employeeId, payload);

      if (res.success) {
        alert("נשמר בהצלחה.");
        navigate("/admin/ratings");
      } else {
        alert(res.message || "עדכון נכשל.");
      }
    } catch {
      alert("שגיאה בעדכון.");
    } finally {
      hide();
    }
  }

  return (
    <div dir="rtl" className="mx-auto max-w-3xl p-6 space-y-6">
      <header>
        <h1 className="text-2xl font-extrabold text-slate-900">עדכון פרמטרים לעובד</h1>
      </header>

      <form
        onSubmit={handleSubmit}
        className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm space-y-6"
      >
        {/* Stepper grid */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">

          <NumberStepper
            label="Skill Level (1–5)"
            value={skillLevel}
            min={1}
            max={5}
            onChange={setSkillLevel}
          />

          <NumberStepper
            label="Priority Rating (≥ 0)"
            value={priorityRating}
            min={0}
            max={999}
            onChange={setPriorityRating}
          />

          <NumberStepper
            label="Max Shifts / Week (≥ 0)"
            value={maxShiftsPerWeek}
            min={0}
            max={999}
            onChange={setMaxShiftsPerWeek}
          />

        </div>

        {/* Notes */}
        <label className="block text-sm">
          <span className="text-slate-600">Admin Notes (אופציונלי)</span>
          <textarea
            rows={3}
            value={adminNotes}
            onChange={(e) => setAdminNotes(e.target.value)}
            className="mt-1 w-full border rounded-xl px-3 py-2"
          />
        </label>

        {/* Actions */}
        <div className="flex items-center gap-3">
          <button
            type="submit"
            disabled={!canSubmit}
            className={`px-4 py-2 rounded-xl text-white ${
              canSubmit ? "bg-slate-900 hover:bg-slate-800" : "bg-slate-400"
            }`}
          >
            שמירה
          </button>

          <button
            type="button"
            onClick={() => window.history.back()}
            className="px-4 py-2 rounded-xl border"
          >
            חזרה
          </button>
        </div>
      </form>
    </div>
  );
}
