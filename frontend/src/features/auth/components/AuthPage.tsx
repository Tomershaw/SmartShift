import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import authService from "../api/authService";
import { useLoading } from "../../appLoading/context/useLoading";

const Input = (props: React.InputHTMLAttributes<HTMLInputElement>) => (
  <input
    {...props}
    className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400 transition text-right"
    dir="rtl"
  />
);

function getRoleFromToken(token: string | null): string | undefined {
  if (!token) return;
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return (
      payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
      payload.role
    );
  } catch {
    return;
  }
}

export default function AuthPage() {
  const [isLogin, setIsLogin] = useState(true);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fullName, setFullName] = useState("");
  const [showReset, setShowReset] = useState(false);
  const [resetEmail, setResetEmail] = useState("");
  const [resetMessage, setResetMessage] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const { show, hide } = useLoading();
  const navigate = useNavigate();

  useEffect(() => {
    const checkToken = async () => {
      show("בודק התחברות...");
      try {
        await authService.validateToken();
      } finally {
        hide();
      }
    };
    checkToken();
    document.addEventListener("visibilitychange", authService.handleTabExit);
    return () => {
      document.removeEventListener(
        "visibilitychange",
        authService.handleTabExit
      );
      hide();
    };
  }, [show, hide]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);
    show(isLogin ? "מתחבר..." : "מבצע רישום...");

    try {
      if (isLogin) {
        const res = await authService.login(email, password);
        localStorage.setItem("token", res.token);
        localStorage.setItem("refreshToken", res.refreshToken);
        const role = getRoleFromToken(res.token);
        navigate(role === "Employee" ? "/employee/signup" : "/admin", {
          replace: true,
        });
      } else {
        await authService.register(fullName, email, password);
        setIsLogin(true);
        setError("");
      }
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "משהו השתבש, נסה שנית";
      setError(message);
    } finally {
      hide();
      setIsLoading(false);
    }
  };

  const handleResetPassword = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");
    setResetMessage("");
    show("שולח קישור לשחזור...");
    try {
      await authService.forgotPassword(resetEmail);
      setResetMessage("נשלח קישור לאיפוס סיסמה למייל שלך!");
      setResetEmail("");
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "שליחת הקישור נכשלה. וודא שהמייל תקין.";
      setError(message);
    } finally {
      hide();
    }
  };

  return (
    <div
      className="fixed inset-0 flex justify-center items-center font-sans"
      dir="rtl"
    >
      {/* רקע */}
      <div
        className="absolute inset-0 bg-cover bg-center -z-10"
        style={{ backgroundImage: "url('/images/background.jpg')" }}
      >
        <div className="absolute inset-0 bg-black/30"></div>{" "}
        {/* שכבת הצללה לרקע */}
      </div>

      <main className="w-full max-w-md bg-white/90 p-8 rounded-2xl shadow-2xl backdrop-blur-md border border-white/20 text-center">
        {/* לוגו ושם האולם */}
        <div className="mb-8">
          <h1 className="text-4xl font-extrabold text-gray-900 tracking-tight">
            SmartShift
          </h1>
          <div className="h-1 w-20 bg-blue-500 mx-auto mt-2 rounded-full"></div>
          <p className="text-xl text-gray-700 mt-4 font-medium">
            ברוכים הבאים ל-ARIA
          </p>
        </div>

        {!showReset ? (
          <>
            <h2 className="text-lg font-medium text-gray-600 mb-6">
              {isLogin ? "התחברות למערכת" : "יצירת חשבון חדש"}
            </h2>

            {error && (
              <div className="bg-red-50 text-red-600 p-3 mb-4 rounded-lg text-sm border border-red-100">
                {error}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              {!isLogin && (
                <Input
                  type="text"
                  placeholder="שם מלא"
                  value={fullName}
                  onChange={e => setFullName(e.target.value)}
                  required
                />
              )}
              <Input
                type="email"
                placeholder="אימייל"
                value={email}
                onChange={e => setEmail(e.target.value)}
                required
              />
              <Input
                type="password"
                placeholder="סיסמה"
                value={password}
                onChange={e => setPassword(e.target.value)}
                minLength={6}
                required
              />
              <button
                type="submit"
                className="w-full bg-blue-600 text-white p-3 rounded-lg font-bold hover:bg-blue-700 transition shadow-lg"
                disabled={isLoading}
              >
                {isLogin ? "כניסה" : "הרשמה"}
              </button>
            </form>

            <div className="mt-6 space-y-2">
              <button
                className="text-sm text-blue-600 hover:underline block mx-auto"
                onClick={() => {
                  setShowReset(true);
                  setError("");
                }}
              >
                שכחתי סיסמה
              </button>
            </div>
          </>
        ) : (
          /* מסך שחזור סיסמה */
          <>
            <h2 className="text-2xl font-bold text-gray-800 mb-4">
              איפוס סיסמה
            </h2>

            {resetMessage && (
              <div className="bg-green-50 text-green-700 p-3 mb-4 rounded-lg text-sm border border-green-200">
                {resetMessage}
              </div>
            )}

            {error && (
              <div className="bg-red-50 text-red-600 p-3 mb-4 rounded-lg text-sm border border-red-100">
                {error}
              </div>
            )}

            <form onSubmit={handleResetPassword} className="space-y-4">
              <p className="text-sm text-gray-600 mb-4">
                הכנס את כתובת המייל שלך ונשלח לך קישור לאיפוס הסיסמה
              </p>
              <Input
                type="email"
                placeholder="הכנס אימייל"
                value={resetEmail}
                onChange={e => setResetEmail(e.target.value)}
                required
              />
              <button
                type="submit"
                className="w-full bg-blue-600 text-white p-3 rounded-lg font-bold hover:bg-blue-700 transition shadow-lg"
              >
                שלח קישור לאיפוס
              </button>
            </form>

            <button
              className="mt-6 text-sm text-gray-500 hover:text-blue-600 font-medium"
              onClick={() => {
                setShowReset(false);
                setError("");
              }}
            >
              חזרה למסך התחברות
            </button>
          </>
        )}
      </main>
    </div>
  );
}
