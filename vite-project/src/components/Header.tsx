import s from "./header.module.css";
import { useSendDigest } from "../hooks/useSendDigest";
import { useAuth } from "../hooks/useAuth";
import { toast, type Renderable, type Toast, type ValueFunction } from "react-hot-toast";

type Props = { title?: string; onLogout?: () => void };

export default function Header({ title = "My Epic News Feed", onLogout }: Props) {
  const { userEmail, logout } = useAuth();
  const { sendDigest, sending } = useSendDigest();

  function onEmailMe() {
    const to = userEmail ?? "your inbox";
    sendDigest(10, {
      onSuccess: () => toast.success(`Digest sent to ${to}`),
      onError: (e: { message: Renderable | ValueFunction<Renderable, Toast>; }) =>
        toast.error(e instanceof Error ? e.message : "Failed to send email"),
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
            {userEmail && (
              <span className={s.email} title={userEmail}>
                {userEmail}
              </span>
            )}
            <button
              className={`${s.btn} ${s.primary}`}
              onClick={onEmailMe}
              disabled={sending}
            >
              {sending ? "Sending…" : "Email me"}
            </button>
            <button className={`${s.btn} ${s.ghost}`} onClick={handleLogout}>
              Log out
            </button>
          </div>
        </div>
      </div>
    </header>
  );
}
