import { api } from '../lib/api';

export type TimeWindow = '24h' | '7d' | '30d' | null;

export type NewsFilterSpec = {
  includeKeywords: string[];
  excludeKeywords?: string[];
  preferredSources?: string[];
  category?: string | null;
  timeWindow?: TimeWindow;
  mustHavePhrases?: string[];
  avoidTopics?: string[];
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

  // Backend already parses AND persists
  async parseNatural(query: string): Promise<NLResponse> {
    const { data } = await api.post('/preferences/natural-language', { query });

    // normalize possible casing variations
    const s = data?.spec ?? {};
    const spec: NewsFilterSpec = {
      includeKeywords: s.includeKeywords ?? s.IncludeKeywords ?? [],
      excludeKeywords: s.excludeKeywords ?? s.ExcludeKeywords ?? [],
      preferredSources: s.preferredSources ?? s.PreferredSources ?? [],
      category: s.category ?? s.Category ?? null,
      timeWindow: (s.timeWindow ?? s.TimeWindow ?? null) as TimeWindow,
      mustHavePhrases: s.mustHavePhrases ?? s.MustHavePhrases ?? [],
      avoidTopics: s.avoidTopics ?? s.AvoidTopics ?? [],
    };

    return { spec, saved: data?.saved ?? [], total: data?.total ?? 0 };
  },
};
