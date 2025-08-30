import { useState } from "react";
import { AuthApi, saveSession} from "../lib/auth";
import { api } from "../lib/api";
import s from "./auth.module.css"; 

type Mode = "login" | "register";

export default function Login(props: { onLoggedIn?: (u: { userId: number; email: string }) => void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [mode, setMode] = useState<Mode>("login");
  const [err, setErr] = useState("");
  const [pending, setPending] = useState(false)

   async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (pending) return
    setErr("")
    setPending(true)
    try {
      const data = mode === "login"
        ? await AuthApi.login({ email, password })
        : await AuthApi.register({ email, password })

      saveSession(data)
      props.onLoggedIn?.({ userId: data.userId, email: data.email })
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Failed")
    } finally {
      setPending(false)
    }
  }

  return (
  <div className={s.wrap}>
    <div className={s.card}>
      <h2 className={s.title}>{mode === "login" ? "Log in" : "Create account"}</h2>

      {err && <div className={s.alert} role="alert">{err}</div>}

      <form className={s.form} onSubmit={handleSubmit} noValidate>
        <label className={s.label} htmlFor="email">Email</label>
        <input id="email" className={s.input} type="email" value={email}
               onChange={(e) => setEmail(e.target.value)} required autoComplete="email" />

        <label className={s.label} htmlFor="password">Password</label>
        <input id="password" className={s.input} type="password" value={password}
               onChange={(e) => setPassword(e.target.value)} required
               autoComplete={mode === "login" ? "current-password" : "new-password"} />

        <button type="submit" className={s.btnPrimary}>
          {mode === "login" ? "Log in" : "Register"}
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
