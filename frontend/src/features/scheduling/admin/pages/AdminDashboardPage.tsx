import { useNavigate } from "react-router-dom";

export default function AdminDashboardPage() {
  const navigate = useNavigate();

  const items = [
    { key: "employees", title: "ניהול עובדים", desc: "הוספה, עריכה ורשימת עובדים", to: "/admin/employees", icon: "👥" },
    { key: "shifts", title: "ניהול משמרות", desc: "יצירה ואישור משמרות", to: "/admin/shifts", icon: "📅" },
    { key: "registrations", title: "נרשמים", desc: "מי נרשם ולמה", to: "/admin/registrations", icon: "📝" },
    { key: "ratings", title: "דירוגים ומשובים", desc: "מתן ציון ומשוב לעובדים", to: "/admin/ratings", icon: "⭐" },
    { key: "settings", title: "הגדרות", desc: "העדפות ומדיניות", to: "/admin/settings", icon: "⚙️" },
  ];

  return (
    <div dir="rtl" className="mx-auto max-w-6xl p-6">
      <header className="mb-8">
        <h1 className="text-3xl font-extrabold text-slate-900">מרכז ניהול</h1>
        <p className="text-slate-600 mt-1">בחר פעולה כדי להתחיל</p>
      </header>

      <section className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
        {items.map(item => (
          <button
            key={item.key}
            onClick={() => navigate(item.to)}
            className="group text-right rounded-2xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md transition hover:-translate-y-0.5 focus:outline-none focus:ring-2 focus:ring-sky-300"
          >
            <div className="flex items-center justify-between">
              <span className="text-3xl">{item.icon}</span>
              <span className="text-xs text-slate-400">כניסה</span>
            </div>
            <h2 className="mt-3 text-xl font-bold text-slate-900">{item.title}</h2>
            <p className="mt-1 text-slate-600 text-sm">{item.desc}</p>
            <div className="mt-4 inline-flex items-center gap-2 text-sky-700 text-sm">
              <span className="opacity-90 group-hover:opacity-100">פתח</span>
              <span aria-hidden>›</span>
            </div>
          </button>
        ))}
      </section>
    </div>
  );
}
