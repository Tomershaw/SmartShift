// src/features/scheduling/admin/pages/AdminRegisterEmployeePage.tsx
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import api from "../../../../services/api";

/* ---- Types + helpers ---- */
type ApiError = { errors?: string[] | string; message?: string };

type CreateUserResponse = {
  success: boolean;
  message?: string;
  userId?: string;
  tenantId?: string;
  errors?: string[];
};

function friendlyError(data?: ApiError): string {
  if (!data) return "נכשלה ההרשמה. בדוק נתונים והרשאות.";
  if (Array.isArray(data.errors)) return data.errors.join(", ");
  if (typeof data.errors === "string") return data.errors;
  if (typeof data.message === "string") return data.message;
  return "נכשלה ההרשמה. בדוק נתונים והרשאות.";
}

export default function AdminRegisterEmployeePage() {
  const navigate = useNavigate();
  const [form, setForm] = useState({
    fullName: "",
    email: "",
    phoneNumber: "",
    password: "",
    role: "Employee",
    gender: "Unknown",
  });
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [rawError, setRawError] = useState<string | null>(null);

  const onChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { name, value } = e.target;
    setForm(prev => ({ ...prev, [name]: value }));
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setMsg(null);
    setRawError(null);
    setLoading(true);

    try {
      const res = await api.post<CreateUserResponse>("/users/create", {
        fullName: form.fullName,
        email: form.email,
        password: form.password,
        phoneNumber: form.phoneNumber,
        role: form.role,
        gender: form.gender,
      });

      if (res.data?.success) {
        setMsg({ ok: true, text: res.data.message ?? "העובד נוצר בהצלחה" });
        setForm({
          fullName: "",
          email: "",
          phoneNumber: "",
          password: "",
          role: "Employee",
          gender: "Unknown",
        });
      } else {
        const text =
          res.data?.errors?.join(", ") ??
          res.data?.message ??
          "שגיאה לא ידועה ביצירת העובד";
        setMsg({ ok: false, text });
      }
    } catch (err: unknown) {
      const resp = (
        err as {
          config?: { url?: string };
          response?: { status?: number; data?: unknown };
        }
      ).response;

      const data = resp?.data as ApiError | undefined;
      const text = friendlyError(data);

      console.error("Create user failed:", {
        status: resp?.status,
        data: resp?.data,
      });

      setMsg({ ok: false, text });
      setRawError(
        JSON.stringify({ status: resp?.status, data: resp?.data }, null, 2)
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div dir="rtl" className="min-h-screen p-6 flex flex-col items-center">
      {/* עוטף את כל התוכן ברוחב נוח ובמרכז */}
      <div className="w-full max-w-2xl">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <div>
            <nav className="text-xs text-slate-500">
              <Link to="/admin" className="hover:text-slate-700">
                מרכז ניהול
              </Link>
              <span className="mx-1">/</span>
              <span className="text-slate-700">רישום עובד חדש</span>
            </nav>
            <h1 className="mt-2 text-2xl font-extrabold text-slate-900">
              רישום עובד חדש
            </h1>
            <p className="text-slate-600 text-sm mt-1">
              מלא את הפרטים. המערכת תקשר את העובד ל-tenant של המנהל אוטומטית.
            </p>
          </div>

          <button
            onClick={() => navigate(-1)}
            className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm hover:shadow"
          >
            ← חזרה
          </button>
        </div>

        {/* כרטיס תוכן ממורכז */}
        <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          {/* Alert */}
          {msg && (
            <div
              className={`mb-4 rounded-lg border p-3 text-sm ${
                msg.ok
                  ? "border-emerald-200 bg-emerald-50 text-emerald-800"
                  : "border-red-200 bg-red-50 text-red-800"
              }`}
            >
              {msg.text}
            </div>
          )}

          {/* Raw error details */}
          {rawError && !msg?.ok && (
            <details className="mb-5 rounded-lg border border-slate-300 bg-slate-50 p-3">
              <summary className="cursor-pointer text-sm text-slate-700">
                פרטי שגיאה מהשרת
              </summary>
              <pre className="mt-2 overflow-auto text-xs text-slate-800">
                {rawError}
              </pre>
            </details>
          )}

          <form onSubmit={onSubmit} className="space-y-5">
            {/* Full name */}
            <Field label="שם מלא" htmlFor="fullName" required>
              <div className="relative">
                <input
                  id="fullName"
                  name="fullName"
                  type="text"
                  value={form.fullName}
                  onChange={onChange}
                  required
                  className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2 pe-10 text-sm outline-none ring-0 transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100"
                  placeholder="לדוגמה: תומר מלך"
                />
                <InputIcon />
              </div>
            </Field>

            {/* Email and Phone */}
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
              <Field label="אימייל" htmlFor="email" required>
                <div className="relative">
                  <input
                    id="email"
                    name="email"
                    type="email"
                    value={form.email}
                    onChange={onChange}
                    required
                    className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2 pe-10 text-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100"
                    placeholder="name@example.com"
                  />
                  <InputIcon />
                </div>
              </Field>

              <Field label="טלפון" htmlFor="phoneNumber">
                <div className="relative">
                  <input
                    id="phoneNumber"
                    name="phoneNumber"
                    type="tel"
                    value={form.phoneNumber}
                    onChange={onChange}
                    className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2 pe-10 text-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100"
                    placeholder="0541234567"
                  />
                  <InputIcon />
                </div>
              </Field>
            </div>

            {/* Password / Role / Gender */}
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-3">
              <Field
                label="סיסמה זמנית"
                htmlFor="password"
                required
                hint="העובד יוכל להחליף מאוחר יותר"
              >
                <div className="relative">
                  <input
                    id="password"
                    name="password"
                    type="password"
                    value={form.password}
                    onChange={onChange}
                    required
                    className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2 pe-10 text-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100"
                    placeholder="מינימום לפי מדיניות השרת"
                  />
                  <InputIcon />
                </div>
              </Field>

              <Field label="תפקיד" htmlFor="role">
                <select
                  id="role"
                  name="role"
                  value={form.role}
                  onChange={onChange}
                  className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100"
                >
                  <option value="Employee">Employee</option>
                  <option value="Manager">Manager</option>
                  <option value="Admin">Admin</option>
                </select>
              </Field>

              <Field label="מגדר" htmlFor="gender">
                <select
                  id="gender"
                  name="gender"
                  value={form.gender}
                  onChange={onChange}
                  className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100"
                >
                  <option value="Unknown">לא מצוין</option>
                  <option value="Male">זכר</option>
                  <option value="Female">נקבה</option>
                  <option value="Other">אחר</option>
                </select>
              </Field>
            </div>

            {/* Actions */}
            <div className="flex items-center gap-3 pt-2">
              <button
                type="submit"
                disabled={loading}
                className={`rounded-xl px-4 py-2 text-sm font-semibold text-white transition ${
                  loading
                    ? "bg-slate-400 cursor-not-allowed"
                    : "bg-emerald-600 hover:bg-emerald-500"
                }`}
              >
                {loading ? "שומר..." : "צור עובד חדש"}
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
          </form>
        </section>
      </div>
    </div>
  );
}

/* ---------- Small UI helpers ---------- */
function Field(props: {
  label: string;
  htmlFor: string;
  children: React.ReactNode;
  required?: boolean;
  hint?: string;
}) {
  return (
    <div>
      <label
        htmlFor={props.htmlFor}
        className="mb-1 block text-sm font-medium text-slate-700"
      >
        {props.label}{" "}
        {props.required && <span className="text-red-600">*</span>}
      </label>
      {props.children}
      {props.hint && (
        <p className="mt-1 text-xs text-slate-500">{props.hint}</p>
      )}
    </div>
  );
}

function InputIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      className="pointer-events-none absolute inset-y-0 left-3 my-auto h-4 w-4 text-slate-400"
      aria-hidden
    >
      <path
        fill="currentColor"
        d="M12 12c2.21 0 4-1.79 4-4S14.21 4 12 4S8 5.79 8 8s1.79 4 4 4m0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4"
      />
    </svg>
  );
}
