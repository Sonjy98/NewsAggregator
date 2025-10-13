import { api } from '../lib/api';

export type TimeWindow = '24h' | '7d' | '30d' | null;

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

export const PREFS_QUERY_KEY = ['preferences'] as const;
export const NEWS_FOR_ME_QUERY_KEY = ['news', 'for-me'] as const;

export const PrefsApi = {
  async list(): Promise<string[]> {
    const { data } = await api.get('/preferences');
    return data;
  },

  async add(keyword: string): Promise<string[]> {
    const { data } = await api.post('/preferences', { keyword });
    return data;
  },

  async remove(keyword: string): Promise<void> {
    await api.delete(`/preferences/${encodeURIComponent(keyword)}`);
  },

  async parseNatural(query: string): Promise<NLResponse> {
    const { data } = await api.post('/preferences/natural-language', { query });

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
