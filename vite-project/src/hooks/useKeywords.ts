import { useQueryClient, useQuery } from '@tanstack/react-query';
import { useAppMutation } from './useAppMutation';
import { PrefsApi } from '../lib/prefs';
import { getToken } from '../lib/auth';

type Ctx = { prev: string[] };

export function useKeywords() {
  const qc = useQueryClient();
  const token = getToken();

  const list = useQuery({
    queryKey: ['prefs', 'keywords'],
    queryFn: () => PrefsApi.list(),
    enabled: !!token,
  });

  const add = useAppMutation<void, Error, string, Ctx>({
    // PrefsApi.add returns Promise<string[]> — wrap to return Promise<void>
    mutationFn: async (kw) => { await PrefsApi.add(kw); },
    onMutate: async (kw) => {
      await qc.cancelQueries({ queryKey: ['prefs', 'keywords'] });
      const prev = (qc.getQueryData<string[]>(['prefs', 'keywords']) ?? []).slice();
      qc.setQueryData<string[]>(['prefs', 'keywords'], [...prev, kw]);
      return { prev };
    },
    onError: (_e, _kw, ctx) => {
      if (ctx?.prev) qc.setQueryData(['prefs', 'keywords'], ctx.prev);
    },
    invalidate: [{ queryKey: ['prefs', 'keywords'] }, { queryKey: ['news', 'for-me'] }],
  });

  const remove = useAppMutation<void, Error, string, Ctx>({
    // PrefsApi.remove returns Promise<string[]> — wrap to return Promise<void>
    mutationFn: async (kw) => { await PrefsApi.remove(kw); },
    onMutate: async (kw) => {
      await qc.cancelQueries({ queryKey: ['prefs', 'keywords'] });
      const prev = (qc.getQueryData<string[]>(['prefs', 'keywords']) ?? []).slice();
      qc.setQueryData<string[]>(['prefs', 'keywords'], prev.filter(x => x !== kw));
      return { prev };
    },
    onError: (_e, _kw, ctx) => {
      if (ctx?.prev) qc.setQueryData(['prefs', 'keywords'], ctx.prev);
    },
    invalidate: [{ queryKey: ['prefs', 'keywords'] }, { queryKey: ['news', 'for-me'] }],
  });

  return {
    keywords: list.data ?? [],
    isLoading: list.isLoading,
    error: list.error as Error | null,
    addKeyword: add.mutate,
    removeKeyword: remove.mutate,
    adding: add.isLoading,
    removing: remove.isLoading,
  };
}
