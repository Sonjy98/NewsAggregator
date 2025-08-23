import { API_BASE, getToken } from "./auth";

export async function sendDigest(max = 10) {
  const token = getToken();
  if (!token) throw new Error("Please log in first.");

  const res = await fetch(`${API_BASE}/api/email/send?max=${max}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
  });

  const text = await res.text();
  if (!res.ok) {
    // try to extract a useful message if JSON ProblemDetails
    try {
      const j = JSON.parse(text);
      throw new Error(j?.detail || j?.title || j?.message || `HTTP ${res.status}`);
    } catch {
      throw new Error(text || `HTTP ${res.status}`);
    }
  }

  // ok → parse JSON if any
  return text ? JSON.parse(text) : {};
}
