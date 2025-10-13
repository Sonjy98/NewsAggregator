import { useMutation, useQueryClient } from '@tanstack/react-query';
import { PrefsApi, PREFS_QUERY_KEY, NEWS_FOR_ME_QUERY_KEY } from '../lib/prefs';
import type { NLResponse } from '../lib/prefs';

export function useParseAndSavePrefs() {
  const qc = useQueryClient();

  return useMutation({
    mutationKey: ['prefs', 'parse-and-save'],
    mutationFn: async (query: string): Promise<NLResponse> => {
      return PrefsApi.parseNatural(query);
    },
    onSuccess: (data) => {
      qc.setQueryData<string[]>(PREFS_QUERY_KEY, (prev) => {
        const current = Array.isArray(prev) ? prev : [];
        const add = Array.isArray(data?.saved) ? data.saved : [];
        if (add.length === 0) return current;
        const set = new Set(current);
        for (const k of add) set.add(k);
        return Array.from(set);
      });

      qc.invalidateQueries({ queryKey: PREFS_QUERY_KEY });
      qc.invalidateQueries({ queryKey: NEWS_FOR_ME_QUERY_KEY });
    },
  });
}
