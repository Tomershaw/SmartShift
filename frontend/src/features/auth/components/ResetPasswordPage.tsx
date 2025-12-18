import { useState, useEffect } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import authService from "../api/authService";
import { useLoading } from "../../appLoading/context/useLoading";

const Input = (props: React.InputHTMLAttributes<HTMLInputElement>) => (
  <input
    {...props}
    className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400 transition"
  />
);

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { show, hide } = useLoading();

  // שליפת הפרמטרים מה-URL
  const token = searchParams.get("token");
  const email = searchParams.get("email");

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    if (!token || !email) {
      setError("Invalid link. Please request a new password reset link.");
    }
  }, [token, email]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (newPassword !== confirmPassword) {
      setError("הסיסמאות אינן תואמות");
      return;
    }

    if (!token || !email) {
      setError("קישור לא תקין או חסר");
      return;
    }

    show("מעדכן סיסמה...");

    try {
      // כאן חשוב לוודא שמות השדות:
      await authService.resetPassword({
        email: email,        // חייב להתאים ל-Email ב-C#
        token: token,        // חייב להתאים ל-Token ב-C#
        newPassword: newPassword // חייב להתאים ל-NewPassword ב-C#
      });

      setSuccess(true);
      setTimeout(() => navigate("/auth"), 3000);
    } catch (err: unknown) {
      // כאן אנחנו שולפים את השגיאה שזרקת ב-C# (ה-InvalidOperationException)
      const serverMsg = (err as { response?: { data?: { message?: string } } }).response?.data?.message || "איפוס הסיסמה נכשל";
      setError(serverMsg);
    } finally {
      hide();
    }
  };

  if (success) {
    return (
      <div className="fixed inset-0 flex justify-center items-center bg-gray-50">
        <div className="bg-white p-8 rounded-2xl shadow-xl max-w-md w-full text-center border border-green-200">
          <div className="text-5xl mb-4">✅</div>
          <h2 className="text-2xl font-bold text-gray-800 mb-2">
            Password Updated!
          </h2>
          <p className="text-gray-600">
            Your password has been changed successfully.
          </p>
          <p className="text-sm text-gray-500 mt-4">Redirecting to login...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="fixed inset-0 flex justify-center items-center">
      {/* רקע זהה לדף הלוגין */}
      <div
        className="absolute inset-0 bg-cover bg-center -z-10"
        style={{ backgroundImage: "url('/images/background.jpg')" }}
      ></div>

      <main className="w-full max-w-md bg-white/90 p-8 rounded-2xl shadow-2xl backdrop-blur-md">
        <h1 className="text-3xl font-bold text-center text-gray-800 mb-6">
          Set New Password
        </h1>

        {error && (
          <div className="bg-red-100 text-red-600 p-3 mb-4 rounded-lg text-center border border-red-200">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              New Password
            </label>
            <Input
              type="password"
              placeholder="Enter new password"
              value={newPassword}
              onChange={e => setNewPassword(e.target.value)}
              minLength={6}
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Confirm Password
            </label>
            <Input
              type="password"
              placeholder="Confirm new password"
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
              minLength={6}
              required
            />
          </div>

          <button
            type="submit"
            disabled={!token || !email}
            className="w-full mt-4 bg-gradient-to-r from-blue-600 to-blue-400 text-white p-3 rounded-lg hover:from-blue-700 hover:to-blue-500 transition transform hover:scale-105 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Update Password
          </button>
        </form>
      </main>
    </div>
  );
}
