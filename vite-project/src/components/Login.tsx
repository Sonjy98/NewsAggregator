import { useState } from "react";
import s from "./auth.module.css";
import { api } from "../lib/api";
import { useAuth } from "../hooks/useAuth";

type Mode = "login" | "register";

export default function Login(props: { onLoggedIn?: (u: { userId: string; email: string }) => void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [mode, setMode] = useState<Mode>("login");
  const [err, setErr] = useState("");

  const { login, register, loggingIn, registering, loginError, registerError } = useAuth();
  const pending = loggingIn || registering;

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (pending) return;
    setErr("");
    const vars = { email, password };
    const onSuccess = (u: { userId: string; email: string }) => props.onLoggedIn?.(u);

    if (mode === "login") {
      login(vars, { onSuccess, onError: (e) => setErr(e.message) });
    } else {
      register(vars, { onSuccess, onError: (e) => setErr(e.message) });
    }
  }

  const topError = err || loginError?.message || registerError?.message || "";

  return (
    <div className={s.wrap}>
      <div className={s.card}>
        <h2 className={s.title}>{mode === "login" ? "Log in" : "Create account"}</h2>

        {topError && <div className={s.alert} role="alert">{topError}</div>}

        <form className={s.form} onSubmit={handleSubmit} noValidate>
          <label className={s.label} htmlFor="email">Email</label>
          <input id="email" className={s.input} type="email" value={email}
                 onChange={(e) => setEmail(e.target.value)} required autoComplete="email" />

          <label className={s.label} htmlFor="password">Password</label>
          <input id="password" className={s.input} type="password" value={password}
                 onChange={(e) => setPassword(e.target.value)} required
                 autoComplete={mode === "login" ? "current-password" : "new-password"} />

          <button type="submit" className={s.btnPrimary} disabled={pending}>
            {pending ? "Working…" : mode === "login" ? "Log in" : "Register"}
          </button>
        </form>

        <div className={s.footer}>
          {mode === "login" ? (
            <button type="button" className={s.linkBtn} onClick={() => setMode("register")}>
              Need an account? Register
            </button>
          ) : (
            <button type="button" className={s.linkBtn} onClick={() => setMode("login")}>
              Have an account? Log in
            </button>
          )}
        </div>

        <p className={s.meta}>API: {api.defaults.baseURL || "/api"}</p>
      </div>
    </div>
  );
}
