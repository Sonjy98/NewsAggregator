import { useState } from "react";
import { getToken } from "../lib/auth";
import { sendDigest } from "../lib/email";

export default function EmailMeButton({ max = 10 }: { max?: number }) {
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string>("");

  // hide if not logged in
  if (!getToken()) return null;

  const onClick = async () => {
    try {
      setBusy(true);
      setMsg("");
      const res = await sendDigest(max);
      setMsg(`Sent ${res.count ?? max} article(s)${res.sentTo ? ` to ${res.sentTo}` : ""}.`);
    } catch (e: any) {
      setMsg(e?.message || "Failed to send email.");
    } finally {
      setBusy(false);
      // auto-clear after a few seconds
      setTimeout(() => setMsg(""), 4000);
    }
  };

  return (
    <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
      <button className="btn" onClick={onClick} disabled={busy}>
        {busy ? "Sending…" : "Email me"}
      </button>
      {msg && <span style={{ fontSize: 12, color: msg.startsWith("Sent") ? "green" : "crimson" }}>{msg}</span>}
    </div>
  );
}
