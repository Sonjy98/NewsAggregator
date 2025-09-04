// src/hooks/useAppMutation.ts
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { UseMutationOptions } from '@tanstack/react-query';


type Invalidate = { queryKey: unknown[] } | Array<{ queryKey: unknown[] }>;

export function useAppMutation<TData, TError = Error, TVars = void, TCtx = unknown>(
  options: UseMutationOptions<TData, TError, TVars, TCtx> & { invalidate?: Invalidate }
) {
  const qc = useQueryClient();
  const m = useMutation<TData, TError, TVars, TCtx>({
    ...options,
    onSuccess: (data, vars, ctx) => {
      options.onSuccess?.(data, vars, ctx);
      const inv = options.invalidate;
      if (inv) {
        const list = Array.isArray(inv) ? inv : [inv];
        for (const it of list) qc.invalidateQueries({ queryKey: it.queryKey });
      }
    },
  });

  return {
    mutate: m.mutate,
    isLoading: m.isPending,
    isError: m.isError,
    error: m.error as TError | null,
    isSuccess: m.isSuccess,
    data: m.data as TData | undefined,
    reset: m.reset,
  };
}
