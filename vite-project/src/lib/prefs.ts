import { api } from './api'

export type NewsFilterSpec = {
  includeKeywords: string[];
  excludeKeywords?: string[];
  preferredSources?: string[];
  category?: string | null;
  timeWindow?: string | null;
  mustHavePhrases?: string[];
  avoidTopics?: string[];
};

export type NLResponse = {
  spec: NewsFilterSpec;
  saved: string[];
  total: number;
};

export const PrefsApi = {
  async list(): Promise<string[]> {
    const { data } = await api.get('/preferences')
    return data
  },

  async add(keyword: string): Promise<string[]> {
    const { data } = await api.post('/preferences', { keyword })
    return data
  },

  async remove(keyword: string): Promise<void> {
    await api.delete(`/preferences/${encodeURIComponent(keyword)}`)
  },
    async parseNatural(query: string): Promise<NLResponse> {
    const { data } = await api.post("/preferences/natural-language", { query });
    return data;
  },
};
