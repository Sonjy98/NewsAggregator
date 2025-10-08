import { useState } from "react";
import { useKeywords } from "../hooks/useKeywords";
import { useAddKeyword } from "../hooks/useAddKeyword";
import { useRemoveKeyword } from "../hooks/useRemoveKeyword";
import { useParseNatural } from "../hooks/useParseNatural";
import { toast } from "react-hot-toast";

export default function Preferences() {
  const [value, setValue] = useState("");

  const [nl, setNl] = useState("");

  const { keywords, isLoading, error } = useKeywords();
  const { addKeyword, adding } = useAddKeyword();
  const { removeKeyword, removing } = useRemoveKeyword();
  const { parseNatural, parsing, data: nlData, error: nlError, reset: resetNl } = useParseNatural();

  const submit = () => {
    const kw = value.trim();
    if (!kw) return;
    addKeyword(kw, {
      onSuccess: () => {
        setValue("");
        toast.success(`Added "${kw}"`);
      },
      onError: (e) => toast.error(e instanceof Error ? e.message : "Failed to add keyword"),
    });
  };

  const onRemove = (kw: string) => {
    removeKeyword(kw, {
      onSuccess: () => toast.success(`Removed "${kw}"`),
      onError: (e) => toast.error(e instanceof Error ? e.message : "Failed to remove keyword"),
    });
  };

  const submitNL = () => {
    const q = nl.trim();
    if (!q) return;
    parseNatural(q, {
      onSuccess: (res) => {
        toast.success(`Parsed preferences. Saved ${res.saved.length} new keyword(s).`);
      },
      onError: (e) => toast.error(e.message || "Failed to parse"),
    });
  };

  const spec = nlData?.spec;
  const saved = nlData?.saved ?? [];

  return (
    <section style={{ margin: "16px 0", padding: 12, border: "1px solid #ddd", borderRadius: 8 }}>
      <h3 style={{ margin: 0, marginBottom: 8 }}>My Keywords</h3>

      {error && (
        <div style={{ background: "#fee", padding: 8, borderRadius: 6, marginBottom: 8 }}>
          {(error as Error).message}
        </div>
      )}

      {/* Manual add */}
      <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
        <input
          placeholder="Add a keyword…"
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && submit()}
          style={{ flex: 1, padding: 8 }}
          disabled={adding}
        />
        <button onClick={submit} disabled={adding || !value.trim()}>
          {adding ? "Adding…" : "Add"}
        </button>
      </div>

      {/* Existing keywords */}
      <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 16 }}>
        {isLoading && <span>Loading…</span>}
        {!isLoading && keywords.length === 0 && <span style={{ color: "#666" }}>No keywords yet.</span>}
        {keywords.map((kw) => (
          <span
            key={kw}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 6,
              padding: "6px 10px",
              border: "1px solid #ccc",
              borderRadius: 20,
            }}
          >
            {kw}
            <button
              onClick={() => onRemove(kw)}
              title={`Remove ${kw}`}
              aria-label={`Remove ${kw}`}
              style={{ color: "black", border: "none", background: "transparent", cursor: "pointer" }}
              disabled={removing}
            >
              ✕
            </button>
          </span>
        ))}
      </div>

      {/* Natural-language → filters */}
      <hr style={{ margin: "16px 0", border: 0, borderTop: "1px solid #eee" }} />
      <h4 style={{ margin: "8px 0" }}>Describe what you want</h4>
      <p style={{ marginTop: 0, color: "#555" }}>
        Example: <i>Tech & AI, no crypto, prefer The Verge and Ars, last 7 days</i>
      </p>

      {nlError && (
        <div style={{ background: "#fee", padding: 8, borderRadius: 6, marginBottom: 8 }}>
          {nlError.message}
        </div>
      )}

      <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
        <textarea
          value={nl}
          onChange={(e) => setNl(e.target.value)}
          rows={3}
          placeholder="Write your preferences in natural language…"
          style={{ flex: 1, padding: 8, borderRadius: 6, border: "1px solid #ccc" }}
          disabled={parsing}
        />
        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          <button onClick={submitNL} disabled={parsing || !nl.trim()}>
            {parsing ? "Parsing…" : "Parse & Save"}
          </button>
          <button onClick={() => { setNl(""); resetNl(); }} disabled={parsing}>
            Clear
          </button>
        </div>
      </div>

      {spec && (
        <div style={{ background: "#f9f9f9", padding: 12, borderRadius: 8 }}>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
            <div>
              <div><strong>Include</strong></div>
              <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
                {spec.includeKeywords?.length ? spec.includeKeywords.map(k => <Pill key={"i-" + k}>{k}</Pill>) : <em>None</em>}
              </div>

              <div style={{ marginTop: 8 }}><strong>Must-have phrases</strong></div>
              <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
                {spec.mustHavePhrases?.length ? spec.mustHavePhrases.map(k => <Pill key={"m-" + k}>{k}</Pill>) : <em>None</em>}
              </div>

              <div style={{ marginTop: 8 }}><strong>Category</strong></div>
              <div>{spec.category ?? <em>None</em>}</div>
            </div>

            <div>
              <div><strong>Exclude</strong></div>
              <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
                {spec.excludeKeywords?.length ? spec.excludeKeywords.map(k => <Pill key={"e-" + k}>{k}</Pill>) : <em>None</em>}
              </div>

              <div style={{ marginTop: 8 }}><strong>Preferred sources</strong></div>
              <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
                {spec.preferredSources?.length ? spec.preferredSources.map(k => <Pill key={"s-" + k}>{k}</Pill>) : <em>None</em>}
              </div>

              <div style={{ marginTop: 8 }}><strong>Time window</strong></div>
              <div>{spec.timeWindow ?? <em>None</em>}</div>
            </div>
          </div>

          <div style={{ marginTop: 10 }}>
            <strong>Saved this run:</strong>{" "}
            {saved.length ? saved.map(k => <Pill key={"saved-" + k}>{k}</Pill>) : <em>None</em>}
          </div>
        </div>
      )}
    </section>
  );
}

const Pill: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <span
    style={{
      display: "inline-block",
      padding: "2px 8px",
      borderRadius: 999,
      border: "1px solid #ddd",
      margin: "2px",
      fontSize: 12,
      background: "#fff",
    }}
  >
    {children}
  </span>
);
