import {
  QueryClient,
  QueryCache,
  MutationCache,
} from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 30_000,
    },
    mutations: { retry: 0 },
  },
  queryCache: new QueryCache({
    onError: (_error, _query) => {

    },
  }),
  mutationCache: new MutationCache({
    onError: (_error, _vars, _ctx, _mutation) => {

    },
  }),
});
