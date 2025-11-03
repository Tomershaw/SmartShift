// src/features/scheduling/admin/ratings/pages/AdminEmployeeParametersPage.tsx
import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { employeeParametersApi } from "../api/employeeParametersApi";
import { useLoading } from "../../../../../appLoading/context/useLoading";

/** Type guard לשגיאות דמויות-HTTP (duck typing) */
type HttpResponseLike = { status?: number };
type HttpErrorLike = { message?: string; response?: HttpResponseLike };

function isHttpErrorLike(err: unknown): err is HttpErrorLike {
  return (
    typeof err === "object" &&
    err !== null &&
    ("message" in err || "response" in err)
  );
}

export default function AdminEmployeeParametersPage() {
  const { employeeId = "" } = useParams<{ employeeId: string }>();
  const navigate = useNavigate();
  const { show, hide } = useLoading();

  // ערכי ברירת מחדל
  const [skillLevel, setSkillLevel] = useState<number>(3);       // 1..5
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

  // ✅ Prefill מהשרת (GET /admin/employees/{employeeId})
  useEffect(() => {
    if (!employeeId) return;

    let mounted = true;
    (async () => {
      show("טוען פרמטרים...");
      try {
        const dto = await employeeParametersApi.getParameters(employeeId);
        if (!mounted) return;

        setSkillLevel(typeof dto.skillLevel === "number" ? dto.skillLevel : 3);
        setPriorityRating(
          typeof dto.priorityRating === "number" ? dto.priorityRating : 0
        );
        setMaxShiftsPerWeek(
          typeof dto.maxShiftsPerWeek === "number" ? dto.maxShiftsPerWeek : 5
        );
        setAdminNotes(dto.adminNotes ?? "");
      } catch (err: unknown) {
        if (isHttpErrorLike(err) && err.response?.status === 404) {
          alert("העובד לא נמצא (או לא שייך לטננט הנוכחי).");
        } else {
          const msg =
            (isHttpErrorLike(err) && err.message) ||
            (err instanceof Error ? err.message : "שגיאה בלתי צפויה");
          console.error("Failed to load employee parameters:", msg);
          alert("שגיאה בטעינת פרמטרים מהשרת.");
        }
      } finally {
        hide();
      }
    })();

    return () => {
      mounted = false;
    };
  }, [employeeId, show, hide]);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
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

      const data = await employeeParametersApi.updateParameters(
        employeeId,
        payload
      );

      if (data.success) {
        alert("הפרמטרים עודכנו בהצלחה.");
        navigate("/admin/ratings");
      } else {
        const msg = data.message || data.errors?.join(", ") || "עדכון נכשל";
        alert(msg);
      }
    } catch (err: unknown) {
      const msg =
        (isHttpErrorLike(err) && err.message) ||
        (err instanceof Error ? err.message : "שגיאה בלתי צפויה");
      console.error("Update parameters failed:", msg);
      alert("שגיאה בעדכון פרמטרים. בדוק הרשאות/קלט/שרת.");
    } finally {
      hide();
    }
  }

  return (
    <div dir="rtl" className="mx-auto max-w-3xl p-6 space-y-6">
      <header className="mb-2">
        <h1 className="text-2xl font-extrabold text-slate-900">עדכון פרמטרים לעובד</h1>
        <p className="text-slate-600 text-sm mt-1 break-all">{employeeId}</p>
      </header>

      <form
        onSubmit={handleSubmit}
        className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm space-y-5"
      >
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <label className="block text-sm">
            <span className="text-slate-600">Skill Level (1–5)</span>
            <input
              type="number"
              min={1}
              max={5}
              value={skillLevel}
              onChange={(e) => setSkillLevel(Number(e.target.value))}
              className="mt-1 w-full border rounded-lg px-3 py-2"
            />
          </label>

          <label className="block text-sm">
            <span className="text-slate-600">Priority Rating (≥ 0)</span>
            <input
              type="number"
              min={0}
              value={priorityRating}
              onChange={(e) => setPriorityRating(Number(e.target.value))}
              className="mt-1 w-full border rounded-lg px-3 py-2"
            />
          </label>

          <label className="block text-sm">
            <span className="text-slate-600">Max Shifts / Week (≥ 0)</span>
            <input
              type="number"
              min={0}
              value={maxShiftsPerWeek}
              onChange={(e) => setMaxShiftsPerWeek(Number(e.target.value))}
              className="mt-1 w-full border rounded-lg px-3 py-2"
            />
          </label>
        </div>

        <label className="block text-sm">
          <span className="text-slate-600">Admin Notes (אופציונלי)</span>
          <textarea
            value={adminNotes}
            onChange={(e) => setAdminNotes(e.target.value)}
            rows={3}
            className="mt-1 w-full border rounded-lg px-3 py-2"
            placeholder="הערות מנהל…"
          />
        </label>

        <div className="flex items-center gap-2">
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
