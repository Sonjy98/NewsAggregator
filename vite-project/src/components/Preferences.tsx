import { useEffect, useState } from "react";
import { PrefsApi } from "../lib/prefs";

export default function Preferences({ onChanged }: { onChanged?: () => void }) {
  const [keywords, setKeywords] = useState<string[]>([]);
  const [value, setValue] = useState("");
  const [err, setErr] = useState("");

  async function load() {
    try {
      setErr("");
      const ks = await PrefsApi.list();
      setKeywords(ks);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Failed to load preferences");
    }
  }

  useEffect(() => { load(); }, []);

  async function add() {
    const kw = value.trim();
    if (!kw) return;
    try {
      const ks = await PrefsApi.add(kw);
      setKeywords(ks);
      setValue("");
      onChanged?.();
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Failed to add");
    }
  }

  async function remove(kw: string) {
    try {
      await PrefsApi.remove(kw);
      setKeywords(k => k.filter(x => x !== kw));
      onChanged?.();
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Failed to remove");
    }
  }

  return (
    <section style={{ margin: "16px 0", padding: 12, border: "1px solid #ddd", borderRadius: 8 }}>
      <h3 style={{ margin: 0, marginBottom: 8 }}>My Keywords</h3>
      {err && <div style={{ background: "#fee", padding: 8, borderRadius: 6, marginBottom: 8 }}>{err}</div>}
      <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
        <input
          placeholder="Add a keyword…"
          value={value}
          onChange={e => setValue(e.target.value)}
          onKeyDown={e => e.key === "Enter" && add()}
          style={{ flex: 1, padding: 8 }}
        />
        <button onClick={add}>Add</button>
      </div>
      <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
        {keywords.length === 0 && <span style={{ color: "#666" }}>No keywords yet.</span>}
        {keywords.map(kw => (
          <span key={kw} style={{ display: "inline-flex", alignItems: "center", gap: 6, padding: "6px 10px",
            border: "1px solid #ccc", borderRadius: 20 }}>
            {kw}
            <button onClick={() => remove(kw)} title="Remove" style={{ color: "black",border: "none", background: "transparent", cursor: "pointer" }}>✕</button>
          </span>
        ))}
      </div>
    </section>
  );
}
