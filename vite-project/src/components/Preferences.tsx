// Preferences.tsx
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { PrefsApi } from "../lib/prefs";
import { useState } from "react";

export default function Preferences() {
  const qc = useQueryClient();
  const [value, setValue] = useState("");

  const { data: keywords = [], isLoading, error } = useQuery({
    queryKey: ['prefs','keywords'],
    queryFn: () => PrefsApi.list(),
  });

  const addMut = useMutation({
    mutationFn: (kw: string) => PrefsApi.add(kw),
    onMutate: async (kw) => {
      await qc.cancelQueries({ queryKey: ['prefs','keywords'] });
      const prev = qc.getQueryData<string[]>(['prefs','keywords']) || [];
      qc.setQueryData<string[]>(['prefs','keywords'], [...prev, kw]);
      return { prev };
    },
    onError: (_e, _kw, ctx) => { if (ctx?.prev) qc.setQueryData(['prefs','keywords'], ctx.prev); },
    onSuccess: () => {
      setValue("");
      qc.invalidateQueries({ queryKey: ['news','for-me'] });
    },
  });

  const removeMut = useMutation({
    mutationFn: (kw: string) => PrefsApi.remove(kw),
    onMutate: async (kw) => {
      await qc.cancelQueries({ queryKey: ['prefs','keywords'] });
      const prev = qc.getQueryData<string[]>(['prefs','keywords']) || [];
      qc.setQueryData<string[]>(['prefs','keywords'], prev.filter(x => x !== kw));
      return { prev };
    },
    onError: (_e, _kw, ctx) => { if (ctx?.prev) qc.setQueryData(['prefs','keywords'], ctx.prev); },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['news','for-me'] });
    },
  });

  const add = () => {
    const kw = value.trim();
    if (kw) addMut.mutate(kw);
  };

  return (
    <section style={{ margin: "16px 0", padding: 12, border: "1px solid #ddd", borderRadius: 8 }}>
      <h3 style={{ margin: 0, marginBottom: 8 }}>My Keywords</h3>
      {error && <div style={{ background: "#fee", padding: 8, borderRadius: 6, marginBottom: 8 }}>
        {(error as Error).message}
      </div>}
      <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
        <input
          placeholder="Add a keyword…"
          value={value}
          onChange={e => setValue(e.target.value)}
          onKeyDown={e => e.key === "Enter" && add()}
          style={{ flex: 1, padding: 8 }}
        />
        <button onClick={add} disabled={addMut.isPending}>Add</button>
      </div>

      <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
        {isLoading && <span>Loading…</span>}
        {!isLoading && keywords.length === 0 && <span style={{ color: "#666" }}>No keywords yet.</span>}
        {keywords.map(kw => (
          <span key={kw} style={{ display: "inline-flex", alignItems: "center", gap: 6, padding: "6px 10px",
            border: "1px solid #ccc", borderRadius: 20 }}>
            {kw}
            <button
              onClick={() => removeMut.mutate(kw)}
              title="Remove"
              style={{ color: "black", border: "none", background: "transparent", cursor: "pointer" }}
              disabled={removeMut.isPending}
            >✕</button>
          </span>
        ))}
      </div>
    </section>
  );
}
