import { api } from "../lib/api";
import type { PreferencesDto } from "../types/Preferences";

export async function getPreferences() {
  const { data } = await api.get<PreferencesDto>("/preferences");
  return data;
}
export type TimeWindow = "24h" | "7d" | "30d" | null;

export type NewsFilterSpec = {
  includeKeywords: string[];
  excludeKeywords?: string[];
  category?: string | null;
  timeWindow?: TimeWindow;
};

export type NLResponse = {
  spec: NewsFilterSpec;
  saved: string[];
  total: number;
};

export async function putPreferences(p: PreferencesDto) {
  await api.put("/preferences", p);
}

export function emptyPreferences(): PreferencesDto {
  return {
    keywords: [],
    excludedKeywords: [],
    timeWindowDays: null,
    languages: [],
    categories: [],
    sort: "recent",
  };
}

// ✅ no query keys here

export const PrefsApi = {
  async list(): Promise<string[]> {
    const { data } = await api.get("/preferences");
    return data;
  },

  async add(keyword: string): Promise<string[]> {
    const { data } = await api.post("/preferences", { keyword });
    return data;
  },

  async remove(keyword: string): Promise<void> {
    await api.delete(`/preferences/${encodeURIComponent(keyword)}`);
  },

  async parseNatural(query: string): Promise<NLResponse> {
    const { data } = await api.post("/preferences/natural-language", { query });

    const s = data?.spec ?? {};
    const spec: NewsFilterSpec = {
      includeKeywords: s.includeKeywords ?? s.IncludeKeywords ?? [],
      excludeKeywords: s.excludeKeywords ?? s.ExcludeKeywords ?? [],
      category: s.category ?? s.Category ?? null,
      timeWindow: (s.timeWindow ?? s.TimeWindow ?? null) as TimeWindow,
    };

    return { spec, saved: data?.saved ?? [], total: data?.total ?? 0 };
  },
};
