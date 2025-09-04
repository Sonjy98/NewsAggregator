import { useState } from "react";
import { useKeywords } from "../hooks/useKeywords";

export default function Preferences() {
  const [value, setValue] = useState("");
  const { keywords, isLoading, error, addKeyword, removeKeyword, adding, removing } = useKeywords();

  const add = () => {
    const kw = value.trim();
    if (kw) {
      addKeyword(kw, { onSuccess: () => setValue("") });
    }
  };

  return (
    <section style={{ margin: "16px 0", padding: 12, border: "1px solid #ddd", borderRadius: 8 }}>
      <h3 style={{ margin: 0, marginBottom: 8 }}>My Keywords</h3>
      {error && <div style={{ background: "#fee", padding: 8, borderRadius: 6, marginBottom: 8 }}>
        {error.message}
      </div>}

      <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
        <input
          placeholder="Add a keyword…"
          value={value}
          onChange={e => setValue(e.target.value)}
          onKeyDown={e => e.key === "Enter" && add()}
          style={{ flex: 1, padding: 8 }}
        />
        <button onClick={add} disabled={adding}>Add</button>
      </div>

      <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
        {isLoading && <span>Loading…</span>}
        {!isLoading && keywords.length === 0 && <span style={{ color: "#666" }}>No keywords yet.</span>}
        {keywords.map(kw => (
          <span key={kw} style={{ display: "inline-flex", alignItems: "center", gap: 6,
            padding: "6px 10px", border: "1px solid #ccc", borderRadius: 20 }}>
            {kw}
            <button
              onClick={() => removeKeyword(kw)}
              title="Remove"
              style={{ color: "black", border: "none", background: "transparent", cursor: "pointer" }}
              disabled={removing}
            >✕</button>
          </span>
        ))}
      </div>
    </section>
  );
}
