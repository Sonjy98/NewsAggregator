import { useState } from "react";
import { useKeywords } from "../hooks/useKeywords";
import { useAddKeyword } from "../hooks/useAddKeyword";
import { useRemoveKeyword } from "../hooks/useRemoveKeyword";
import { toast } from "react-hot-toast";

export default function Preferences() {
  const [value, setValue] = useState("");

  // list (read)
  const { keywords, isLoading, error } = useKeywords();

  // mutations (write)
  const { addKeyword, adding } = useAddKeyword();
  const { removeKeyword, removing } = useRemoveKeyword();

  const submit = () => {
    const kw = value.trim();
    if (!kw) return;
    addKeyword(kw, {
      onSuccess: () => {
        setValue("");
        toast.success(`Added "${kw}"`);
      },
      onError: (e) =>
        toast.error(e instanceof Error ? e.message : "Failed to add keyword"),
    });
  };

  const onRemove = (kw: string) => {
    removeKeyword(kw, {
      onSuccess: () => toast.success(`Removed "${kw}"`),
      onError: (e) =>
        toast.error(e instanceof Error ? e.message : "Failed to remove keyword"),
    });
  };

  return (
    <section
      style={{ margin: "16px 0", padding: 12, border: "1px solid #ddd", borderRadius: 8 }}
    >
      <h3 style={{ margin: 0, marginBottom: 8 }}>My Keywords</h3>

      {error && (
        <div
          style={{
            background: "#fee",
            padding: 8,
            borderRadius: 6,
            marginBottom: 8,
          }}
        >
          {(error as Error).message}
        </div>
      )}

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

      <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
        {isLoading && <span>Loading…</span>}
        {!isLoading && keywords.length === 0 && (
          <span style={{ color: "#666" }}>No keywords yet.</span>
        )}
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
              style={{
                color: "black",
                border: "none",
                background: "transparent",
                cursor: "pointer",
              }}
              disabled={removing}
            >
              ✕
            </button>
          </span>
        ))}
      </div>
    </section>
  );
}
