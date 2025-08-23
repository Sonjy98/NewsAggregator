import { useEffect, useState } from "react";
import type { Article } from "../types/Article";
import NewsCard from "./NewsCard";
import { API_BASE, getToken, logout } from "../lib/auth";

export default function NewsFeed({ refreshKey = 0 }: { refreshKey?: number }) {
  const [articles, setArticles] = useState<Article[]>([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  useEffect(() => {
    const controller = new AbortController();
    (async () => {
      setLoading(true); setErr("");
      try {
        const token = getToken();
        const res = await fetch(`${API_BASE}/api/externalnews/for-me`, {
          headers: {
            "Content-Type": "application/json",
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          signal: controller.signal,
        });

        if (res.status === 401) { logout(); location.reload(); return; }
        if (!res.ok) throw new Error(await res.text());

        const data = await res.json();
        const parsed: Article[] = (data?.results ?? []).map((it: any, i: number) => ({
          id: it?.link || String(i),
          title: it?.title ?? "Untitled",
          body: it?.description || "No description available.",
          author: Array.isArray(it?.creator) ? (it.creator[0] ?? "Unknown") : (it?.creator ?? "Unknown"),
          publishedAt: it?.pubDate ?? "",
          image: it?.image_url ?? null,
        }));

        setArticles(parsed);
      } catch (e) {
        if ((e as any)?.name !== "AbortError") setErr(e instanceof Error ? e.message : "Failed to load news.");
      } finally {
        setLoading(false);
      }
    })();
    return () => controller.abort();
  }, [refreshKey]);

  if (loading) return <p>Loading news…</p>;
  if (err) return <p style={{ color: "crimson" }}>Error: {err}</p>;
  if (!articles.length) return <p>No articles yet. Add some keywords above.</p>;

  return <div>{articles.map(a => <NewsCard key={a.id} article={a} />)}</div>;
}
