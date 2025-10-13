import { useQuery } from '@tanstack/react-query';
import { PrefsApi, PREFS_QUERY_KEY } from '../lib/prefs';

export function useKeywords() {
  const q = useQuery<string[], Error>({
    queryKey: PREFS_QUERY_KEY,
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
