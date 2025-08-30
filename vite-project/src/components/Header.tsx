import { useState } from "react";
import s from "./header.module.css";
import { getUserEmail } from "../lib/auth";
import { sendDigest } from "../lib/email";

type Props = { 
  title?: string; 
  onLogout?: () => void;   // new prop 
};

export default function Header({ title = "My Epic News Feed", onLogout }: Props) {
  const email = getUserEmail() ?? "";
  const [sending, setSending] = useState(false);
  const [msg, setMsg] = useState<string>("");

  async function onEmailMe() {
    try {
      setSending(true);
      setMsg("");
      const res = await sendDigest(10);
      const to = (res?.sentTo ?? email) || "your inbox";
      setMsg(`Sent to ${to}`);
    } catch (e) {
      setMsg(e instanceof Error ? e.message : "Failed to send email");
    } finally {
      setSending(false);
      setTimeout(() => setMsg(""), 3500);
    }
  }

  function handleLogout() {
    // no location.reload()
    onLogout?.();
  }

  return (
    <header className={s.header}>
      <div className={s.bar}>
        <div className={s.container}>
          <div className={s.brand}>{title}</div>
          <div className={s.spacer} />
          <div className={s.userArea}>
            {email && <span className={s.email} title={email}>{email}</span>}
            <button className={`${s.btn} ${s.primary}`} onClick={onEmailMe} disabled={sending}>
              {sending ? "Sending…" : "Email me"}
            </button>
            <button className={`${s.btn} ${s.ghost}`} onClick={handleLogout}>Log out</button>
          </div>
        </div>
      </div>

      {msg && <div className={s.toast}>{msg}</div>}
    </header>
  );
}
