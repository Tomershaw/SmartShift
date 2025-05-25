import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import authService from "../api/authService";

const Input = (props: React.InputHTMLAttributes<HTMLInputElement>) => (
  <input
    {...props}
    className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400 transition"
  />
);

export default function AuthPage() {
  const [isLogin, setIsLogin] = useState(true);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fullName, setFullName] = useState("");
  const [error, setError] = useState("");
  const [showReset, setShowReset] = useState(false);
  const [resetMessage, setResetMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  

  const navigate = useNavigate();

  useEffect(() => {
    const checkToken = async () => {
      const isValid = await authService.validateToken();
      setIsLoading(false);
      if (isValid) navigate("/schedule");
    };

    checkToken();
    document.addEventListener("visibilitychange", authService.handleTabExit);
    return () => document.removeEventListener("visibilitychange", authService.handleTabExit);
  }, [navigate]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if(!email || !password && !fullName) {
      setError("Please fill in all fields");
      return;
    }
    setIsLoading(true);
    try {
      if (isLogin) {
        const res = await authService.login(email, password);
        localStorage.setItem("token", res.token);
        localStorage.setItem("refreshToken", res.refreshToken);
        navigate("/schedule");
      } else {
        await authService.register(fullName, email, password);
        setIsLogin(true);
        setError("");
      }
    } catch (err: unknown) {
      console.log('Error:', err);
      
      // Type guard לבדיקה בטוחה
      if (err && typeof err === 'object' && 'response' in err) {
        const error = err as {
          response?: {
            data?: {
              errors?: Record<string, string[]>;
              message?: string;
            }
          }
        };
  
        const errors = error.response?.data?.errors;
        const message = error.response?.data?.message;
  
        if (errors) {
          const flatMessages = Object.values(errors).flat().join(" ");
          setError(flatMessages);
        } else if (message) {
          setError(message);
        } else {
          setError("Something went wrong. Please try again.");
        }
      } else {
        setError("Something went wrong. Please try again.");
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleResetPassword = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setResetMessage("Reset functionality will be available soon.");
  };

  if (isLoading) {
    return (
      <div className="fixed inset-0 flex items-center justify-center bg-white">
        <div className="w-12 h-12 border-4 border-blue-500 border-dashed rounded-full animate-spin"></div>
      </div>
    );
  }

  return (
    <div className="fixed inset-0 flex justify-center items-center">
      <div
        className="absolute inset-0 bg-cover bg-center -z-10"
        style={{ backgroundImage: "url('/images/background.jpg')" }}
      ></div>

      <main className="w-full max-w-md bg-white bg-opacity-80 p-8 rounded-2xl shadow-2xl backdrop-blur-md">
        <h1 className="text-5xl font-bold text-center text-gray-800 mb-8">SmartShift</h1>

        {!showReset ? (
          <>
            <h2 className="text-2xl font-semibold text-center text-gray-600 mb-6">
              {isLogin ? "Welcome Back!" : "Create an Account"} 
            </h2>

            {error && (
              <div className="bg-red-100 text-red-600 p-3 mb-4 rounded-lg text-center">
                {error}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4 w-full px-4">
              {!isLogin && (
                <Input
                  type="text"
                  placeholder="Full Name"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  required
                />
              )}
              <Input
                type="email"
                placeholder="Email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
              <Input
                type="password"
                placeholder="Password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                minLength={6}
                required
              />
              <button
                type="submit"
                className="w-full bg-gradient-to-r from-blue-500 to-teal-400 text-white p-3 rounded-lg hover:from-blue-600 hover:to-teal-500 transition transform hover:scale-105"
              >
                {isLogin ? "Login" : "Register"}
              </button>
            </form>

            <div className="mt-4 text-center">
              <button
                className="text-sm text-gray-600 hover:text-blue-500 transition"
                onClick={() => setShowReset(true)}
              >
                Forgot Password?
              </button>
            </div>

            <div className="mt-6 text-center text-gray-600">
              {isLogin ? "Don't have an account?" : "Already have an account?"}{" "}
              <button
                type="button"
                onClick={() => {
                  setIsLogin(!isLogin);
                  setError("");
                }}
                className="text-blue-500 hover:underline font-medium transition"
              >
                {isLogin ? "Register" : "Login"}
              </button>
            </div>
          </>
        ) : (
          <>
            <h2 className="text-2xl font-semibold text-center text-gray-600 mb-6">
              Reset Password (Coming Soon)
            </h2>
            {resetMessage && (
              <div className="bg-green-100 text-green-600 p-3 mb-4 rounded-lg text-center">
                {resetMessage}
              </div>
            )}
            <form onSubmit={handleResetPassword} className="space-y-4 w-full px-4">
              <button
                type="submit"
                className="w-full bg-gradient-to-r from-blue-500 to-teal-400 text-white p-3 rounded-lg hover:from-blue-600 hover:to-teal-500 transition transform hover:scale-105"
              >
                Notify Me When Ready
              </button>
            </form>
            <div className="mt-6 text-center text-gray-600">
              <button
                className="text-blue-500 hover:underline font-medium transition"
                onClick={() => setShowReset(false)}
              >
                Back to Login
              </button>
            </div>
          </>
        )}
      </main>
    </div>
  );
}