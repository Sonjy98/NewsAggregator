import { useQuery } from '@tanstack/react-query';
import { PrefsApi } from '../lib/prefs';

export const keywordsKey = ['prefs', 'keywords'] as const;

export function useKeywords() {
  const q = useQuery<string[], Error>({
    queryKey: keywordsKey,
    queryFn: () => PrefsApi.list(),
    staleTime: 60_000,
  });

  return {
    keywords: q.data ?? [],
    isLoading: q.isLoading,
    error: q.error ?? null,
    refetch: q.refetch,
  };
}