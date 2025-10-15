import { useQuery } from '@tanstack/react-query';
import { PrefsApi} from '../lib/prefs';
import { PREFS_QUERY_KEY, NEWS_FOR_ME_QUERY_KEY } from "../hooks/queryKeys";

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
