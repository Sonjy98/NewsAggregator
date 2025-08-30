import type { AuthResponse, LoginRequest, RegisterRequest } from "../types/auth";
import { api } from "./api"

export const AuthApi = {
  // NOTE: paths do NOT include /api; api.ts already has baseURL: '/api' or VITE_API_BASE
  login(req: LoginRequest): Promise<AuthResponse> {
    return api.post("/auth/login", req).then(r => r.data)
  },
  register(req: RegisterRequest): Promise<AuthResponse> {
    return api.post("/auth/register", req).then(r => r.data)
  },
}

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
