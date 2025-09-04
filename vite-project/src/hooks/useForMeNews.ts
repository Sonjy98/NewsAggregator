import { useQuery } from '@tanstack/react-query';
import type { Article } from '../types/Article';
import { api } from '../lib/api';

function mapResults(rs: any[]): Article[] {
  return (rs ?? []).map((it: any, i: number) => ({
    id: it?.link || String(i),
    title: it?.title ?? 'Untitled',
    body: it?.description || 'No description available.',
    author: Array.isArray(it?.creator) ? (it.creator[0] ?? 'Unknown') : (it?.creator ?? 'Unknown'),
    publishedAt: it?.pubDate ?? '',
    image: it?.image_url ?? null,
  }));
}

export function useForMeNews() {
  return useQuery({
    queryKey: ['news', 'for-me'],
    queryFn: async (): Promise<Article[]> => {
      const { data } = await api.get('/externalnews/for-me');
      return mapResults(data?.results);
    },
  });
}
