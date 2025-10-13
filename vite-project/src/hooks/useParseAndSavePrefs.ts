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
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: PREFS_QUERY_KEY });
      qc.invalidateQueries({ queryKey: NEWS_FOR_ME_QUERY_KEY });
    },
  });
}
