import type { AuthResponse, LoginRequest, RegisterRequest } from "../types/auth";

export const API_BASE = import.meta.env.VITE_API_BASE;

async function apiPost<TReq, TRes>(path: string, body: TReq): Promise<TRes> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  const text = await res.text();
  if (!res.ok) throw new Error(text || "Request failed");
  return text ? (JSON.parse(text) as TRes) : ({} as TRes);
}

export const AuthApi = {
  login:    (req: LoginRequest)    => apiPost<LoginRequest,    AuthResponse>("/api/auth/login", req),
  register: (req: RegisterRequest) => apiPost<RegisterRequest, AuthResponse>("/api/auth/register", req),
};

export function saveSession(data: AuthResponse) {
  localStorage.setItem("auth_token", data.token);
  localStorage.setItem("auth_user", JSON.stringify({ userId: data.userId, email: data.email }));
}
export function getToken() { return localStorage.getItem("auth_token"); }
export function getUser()  {
  const raw = localStorage.getItem("auth_user");
  return raw ? (JSON.parse(raw) as { userId: number; email: string }) : null;
}

export function getUserEmail(): string | null {
  const u = getUser();
  if (u?.email) return u.email;
  const t = getToken();
  if (!t) return null;
  try {
    const payload = JSON.parse(atob(t.split(".")[1]));
    return payload?.email ?? null;
  } catch {
    return null;
  }
}

export function logout() {
  localStorage.removeItem("auth_token");
  localStorage.removeItem("auth_user");
}
