import s from "./header.module.css";
import { useSendDigest } from "../hooks/useSendDigest";
import { useAuth } from "../hooks/useAuth";
import { useState } from "react";

type Props = { title?: string; onLogout?: () => void };

export default function Header({ title = "My Epic News Feed", onLogout }: Props) {
  const { userEmail, logout } = useAuth();
  const { mutate: sendDigest, isLoading, isError, error, isSuccess, reset } = useSendDigest();
  const [msg, setMsg] = useState("");

  function onEmailMe() {
    setMsg("");
    reset();
    sendDigest(10, {
      onSuccess: (res) => {
        const to = (res?.sentTo ?? userEmail) || "your inbox";
        setMsg(`Sent to ${to}`);
        setTimeout(() => setMsg(""), 3500);
      },
      onError: (e) => {
        setMsg(e instanceof Error ? e.message : "Failed to send email");
        setTimeout(() => setMsg(""), 3500);
      },
    });
  }

  function handleLogout() {
    logout();
    onLogout?.();
  }

  return (
    <header className={s.header}>
      <div className={s.bar}>
        <div className={s.container}>
          <div className={s.brand}>{title}</div>
          <div className={s.spacer} />
          <div className={s.userArea}>
            {userEmail && <span className={s.email} title={userEmail}>{userEmail}</span>}
            <button className={`${s.btn} ${s.primary}`} onClick={onEmailMe} disabled={isLoading}>
              {isLoading ? "Sending…" : "Email me"}
            </button>
            <button className={`${s.btn} ${s.ghost}`} onClick={handleLogout}>Log out</button>
          </div>
        </div>
      </div>
      {msg && <div className={s.toast}>{msg}</div>}
      {isError && !msg && <div className={s.toast}>{(error as Error)?.message ?? "Failed to send email"}</div>}
      {isSuccess && !msg && <div className={s.toast}>Sent!</div>}
    </header>
  );
}
