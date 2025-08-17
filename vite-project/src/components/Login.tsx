import { useState } from "react";
import { AuthApi, saveSession, API_BASE } from "../lib/auth";

type Mode = "login" | "register";

export default function Login(props: { onLoggedIn?: (u: { userId: number; email: string }) => void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [mode, setMode] = useState<Mode>("login");
  const [err, setErr] = useState("");

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setErr("");
    try {
      const data = mode === "login"
        ? await AuthApi.login({ email, password })
        : await AuthApi.register({ email, password });
      saveSession(data);
      props.onLoggedIn?.({ userId: data.userId, email: data.email });
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Failed");
    }
  }

  return (
    <div style={{ maxWidth: 420, margin: "60px auto", padding: 24, border: "1px solid #ddd", borderRadius: 12 }}>
      <h2 style={{ marginBottom: 16 }}>{mode === "login" ? "Log in" : "Create account"}</h2>
      {err && <div style={{ background: "#fee", padding: 8, borderRadius: 8, marginBottom: 12 }}>{err}</div>}
      <form onSubmit={handleSubmit}>
        <label style={{ display: "block", marginBottom: 8 }}>Email</label>
        <input type="email" value={email} onChange={e => setEmail(e.target.value)}
               required style={{ width: "100%", padding: 10, marginBottom: 12 }} />
        <label style={{ display: "block", marginBottom: 8 }}>Password</label>
        <input type="password" value={password} onChange={e => setPassword(e.target.value)}
               required style={{ width: "100%", padding: 10, marginBottom: 20 }} />
        <button type="submit" style={{ width: "100%", padding: 10 }}>
          {mode === "login" ? "Log in" : "Register"}
        </button>
      </form>
      <div style={{ marginTop: 12, textAlign: "center" }}>
        {mode === "login" ? (
          <button onClick={() => setMode("register")} style={{ background: "none", border: "none", color: "#06f", cursor: "pointer" }}>
            Need an account? Register
          </button>
        ) : (
          <button onClick={() => setMode("login")} style={{ background: "none", border: "none", color: "#06f", cursor: "pointer" }}>
            Have an account? Log in
          </button>
        )}
      </div>
      <p style={{ marginTop: 16, fontSize: 12, color: "#666" }}>API: {API_BASE}</p>
    </div>
  );
}
