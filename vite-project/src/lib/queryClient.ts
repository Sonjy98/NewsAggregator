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
    onError: (error, query) => {
      // Centralized query error reporting/logging (optional)
      // console.error('Query error:', query.queryKey, error);
    },
  }),
  mutationCache: new MutationCache({
    onError: (error, _vars, _ctx, mutation) => {
      // Centralized mutation error reporting/logging (optional)
      // console.error('Mutation error:', mutation.options.mutationKey, error);
    },
  }),
});
