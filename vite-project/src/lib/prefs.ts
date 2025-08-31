import { API_BASE, getToken } from "./auth";

async function authed<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getToken();
  const res = await fetch(`${API_BASE}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    ...init,
  });
  const text = await res.text();
  if (!res.ok) throw new Error(text || `HTTP ${res.status}`);
  return text ? (JSON.parse(text) as T) : ({} as T);
}

export const PrefsApi = {
  list:  () => authed<string[]>("/api/preferences"),
  add:   (keyword: string) => authed<string[]>("/api/preferences", { method: "POST", body: JSON.stringify({ keyword }) }),
  remove:(keyword: string) => authed<void>      (`/api/preferences/${encodeURIComponent(keyword)}`, { method: "DELETE" }),
};
