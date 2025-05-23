import api from "../../../services/api";

const login = async (email: string, password: string): Promise<string> => {
  const response = await api.post<{ token: string }>("/account/login", {
    email,
    password,
  });
  return response.data.token;
};

const register = async (
  fullName: string,
  email: string,
  password: string
): Promise<string> => {
  const response = await api.post<{ message: string }>("/account/register", {
    fullName,
    email,
    password,
  });
  return response.data.message;
};

const resetPassword = async (): Promise<string> => {
  return Promise.resolve("Reset link sent to your email.");
};

//const validateToken = async (): Promise<boolean> => {
//  try {
//    const response = await api.get("/account/validate-token");
//    return response.status === 200;
//  } catch {
//    return false;
//  }
//};

const validtoken = async () => {

  const token = localStorage.getItem("token");
  if(!token) return true;
  try{
    await api.get("scheduling/employees",{
      headers:{Authorization:`Bearer ${token}`}
    })
    return true;
  } catch {
    return  false;
  }
}

const logout = () => {
  localStorage.removeItem("token");
};

const handleTabExit = () => {
  if (document.visibilityState === "hidden") {
    console.log("🧹 authService.handleTabExit triggered");
    logout();
  }
};

const isTokenExpired = (token: string): boolean => {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    const exp = payload.exp * 1000;
    return Date.now() > exp;
  } catch {
    return true;
  }
};

const validateToken = async (): Promise<boolean> => {
  const token = localStorage.getItem("token");
  if (!token || isTokenExpired(token)) return false;

  try {
    await api.get("/scheduling/employees", {
      headers: { Authorization: `Bearer ${token}` }
    });
    return true;
  } catch {
    return false;
  }
};
  

export default { login, register, resetPassword, validateToken, validtoken,logout,handleTabExit,isTokenExpired };
