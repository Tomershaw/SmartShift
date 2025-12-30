// src/features/scheduling/admin/pages/AdminRegisterEmployeePage.tsx
import { useState } from "react";
import { Link } from "react-router-dom"; // מחקתי את useNavigate
import api from "../../../../services/api";
import { 
  UserPlus, 
  ArrowRight, 
  Mail, 
  Phone, 
  Lock, 
  User, 
  Shield,
  Briefcase, 
  Users,
  CheckCircle,
  AlertCircle
} from "lucide-react";

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
  // נמחק: const navigate = useNavigate(); - לא בשימוש
  
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

  const onChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { name, value } = e.target;
    setForm(prev => ({ ...prev, [name]: value }));
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setMsg(null);
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
          response?: { status?: number; data?: unknown };
        }
      ).response;

      const data = resp?.data as ApiError | undefined;
      const text = friendlyError(data);
      setMsg({ ok: false, text });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div dir="rtl" className="min-h-screen bg-slate-50/50 pb-20 font-sans text-slate-900">
      <div className="mx-auto max-w-3xl px-4 py-10 sm:px-6 lg:px-8">
        
        {/* Header */}
        <header className="mb-8 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 text-slate-500 text-sm mb-1">
               <Link to="/admin" className="hover:text-slate-800 transition">ניהול</Link>
               <span>/</span>
               <Link to="/admin/employees" className="hover:text-slate-800 transition">עובדים</Link>
            </div>
            <h1 className="text-3xl font-black text-slate-900 tracking-tight flex items-center gap-3">
              <div className="p-2 bg-emerald-100 rounded-xl text-emerald-600">
                <UserPlus size={24} />
              </div>
              רישום עובד חדש
            </h1>
            <p className="text-slate-500 mt-2">
              הוספת עובד למערכת, הגדרת הרשאות ופרטי התחברות.
            </p>
          </div>
          
          <Link
            to="/admin"
            className="inline-flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 shadow-sm hover:bg-slate-50 hover:text-slate-900 transition-all sm:w-auto w-full"
          >
            <ArrowRight size={16} />
            חזרה לניהול
          </Link>
        </header>

        {/* Alert Messages */}
        {msg && (
          <div className={`mb-6 p-4 rounded-xl flex items-start gap-3 border shadow-sm animate-in slide-in-from-top-2 ${
            msg.ok ? "bg-emerald-50 border-emerald-100 text-emerald-800" : "bg-red-50 border-red-100 text-red-800"
          }`}>
            <div className={`p-1 rounded-full ${msg.ok ? "bg-emerald-100 text-emerald-600" : "bg-red-100 text-red-600"}`}>
              {msg.ok ? <CheckCircle size={18} /> : <AlertCircle size={18} />}
            </div>
            <div>
              <h3 className="font-bold text-sm">{msg.ok ? "הפעולה הושלמה בהצלחה" : "שגיאה ברישום"}</h3>
              <p className="text-sm mt-0.5 opacity-90">{msg.text}</p>
            </div>
          </div>
        )}

        <form onSubmit={onSubmit} className="space-y-6">
          
          {/* Card 1: פרטים אישיים */}
          <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
            <h2 className="text-lg font-bold text-slate-800 mb-4 flex items-center gap-2 border-b border-slate-100 pb-2">
              <User size={18} className="text-blue-500" />
              פרטים אישיים
            </h2>
            
            <div className="space-y-4">
              {/* שם מלא */}
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1.5">שם מלא <span className="text-red-500">*</span></label>
                <div className="relative">
                  <input
                    name="fullName"
                    type="text"
                    required
                    value={form.fullName}
                    onChange={onChange}
                    className="block w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-2.5 pl-10 text-sm outline-none focus:bg-white focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition-all"
                    placeholder="לדוגמה: ישראל ישראלי"
                  />
                  <User className="absolute left-3 top-2.5 text-slate-400" size={18} />
                </div>
              </div>

              {/* טלפון */}
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1.5">מספר טלפון</label>
                <div className="relative">
                  <input
                    name="phoneNumber"
                    type="tel"
                    value={form.phoneNumber}
                    onChange={onChange}
                    className="block w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-2.5 pl-10 text-sm outline-none focus:bg-white focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition-all"
                    placeholder="050-0000000"
                  />
                  <Phone className="absolute left-3 top-2.5 text-slate-400" size={18} />
                </div>
              </div>
            </div>
          </div>

          {/* Card 2: פרטי התחברות */}
          <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
            <h2 className="text-lg font-bold text-slate-800 mb-4 flex items-center gap-2 border-b border-slate-100 pb-2">
              <Shield size={18} className="text-purple-500" />
              פרטי גישה למערכת
            </h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* אימייל */}
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1.5">כתובת אימייל <span className="text-red-500">*</span></label>
                <div className="relative">
                  <input
                    name="email"
                    type="email"
                    required
                    value={form.email}
                    onChange={onChange}
                    className="block w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-2.5 pl-10 text-sm outline-none focus:bg-white focus:border-purple-500 focus:ring-2 focus:ring-purple-100 transition-all"
                    placeholder="name@company.com"
                  />
                  <Mail className="absolute left-3 top-2.5 text-slate-400" size={18} />
                </div>
              </div>

              {/* סיסמה */}
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1.5">סיסמה ראשונית <span className="text-red-500">*</span></label>
                <div className="relative">
                  <input
                    name="password"
                    type="password"
                    required
                    value={form.password}
                    onChange={onChange}
                    className="block w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-2.5 pl-10 text-sm outline-none focus:bg-white focus:border-purple-500 focus:ring-2 focus:ring-purple-100 transition-all"
                    placeholder="••••••••"
                  />
                  <Lock className="absolute left-3 top-2.5 text-slate-400" size={18} />
                </div>
                <p className="text-xs text-slate-500 mt-1">העובד יוכל לשנות את הסיסמה בכניסה הראשונה.</p>
              </div>
            </div>
          </div>

          {/* Card 3: הגדרות נוספות */}
          <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
            <h2 className="text-lg font-bold text-slate-800 mb-4 flex items-center gap-2 border-b border-slate-100 pb-2">
              <Briefcase size={18} className="text-emerald-500" />
              הגדרות תפקיד
            </h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* תפקיד */}
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1.5">תפקיד במערכת</label>
                <div className="relative">
                  <select
                    name="role"
                    value={form.role}
                    onChange={onChange}
                    className="block w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-2.5 pl-10 text-sm outline-none focus:bg-white focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100 transition-all appearance-none"
                  >
                    <option value="Employee">עובד (Employee)</option>
                    <option value="Manager">מנהל (Manager)</option>
                    <option value="Admin">אדמין (Admin)</option>
                  </select>
                  <Briefcase className="absolute left-3 top-2.5 text-slate-400" size={18} />
                </div>
              </div>

              {/* מגדר */}
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1.5">מגדר</label>
                <div className="relative">
                  <select
                    name="gender"
                    value={form.gender}
                    onChange={onChange}
                    className="block w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-2.5 pl-10 text-sm outline-none focus:bg-white focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100 transition-all appearance-none"
                  >
                    <option value="Unknown">לא מצוין</option>
                    <option value="Male">זכר</option>
                    <option value="Female">נקבה</option>
                    <option value="Other">אחר</option>
                  </select>
                  <Users className="absolute left-3 top-2.5 text-slate-400" size={18} />
                </div>
              </div>
            </div>
          </div>

          {/* Actions Footer */}
          <div className="flex items-center justify-end pt-4">
            <button
              type="submit"
              disabled={loading}
              className={`
                inline-flex items-center justify-center gap-2 rounded-xl px-8 py-3 text-sm font-bold text-white shadow-lg transition-all active:scale-95 w-full sm:w-auto
                ${loading 
                  ? "bg-slate-400 cursor-not-allowed shadow-none" 
                  : "bg-emerald-600 hover:bg-emerald-700 shadow-emerald-500/30"
                }
              `}
            >
              {loading ? (
                "יוצר משתמש..."
              ) : (
                <>
                  <UserPlus size={18} />
                  צור עובד חדש
                </>
              )}
            </button>
          </div>

        </form>
      </div>
    </div>
  );
}