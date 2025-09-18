import { useMutation, useQueryClient, type UseMutationOptions, type UseMutationResult, type QueryKey } from '@tanstack/react-query';

type Invalidate = { queryKey: QueryKey } | Array<{ queryKey: QueryKey }>;

type MinimalMutation<TData, TError, TVariables> = {
  mutate: UseMutationResult<TData, TError, TVariables>['mutate'];
  isLoading: boolean;
  error: TError | null;
};

export function useAppMutation<
  TData = unknown,
  TError = Error,
  TVariables = void,
  TContext = unknown
>(
  opts: UseMutationOptions<TData, TError, TVariables, TContext> & { invalidate?: Invalidate }
): MinimalMutation<TData, TError, TVariables> {
  const qc = useQueryClient();
  const { invalidate, onSuccess, ...rest } = opts;

  const m = useMutation<TData, TError, TVariables, TContext>({
    ...rest,
    onSuccess: (data, variables, ctx) => {
      onSuccess?.(data, variables, ctx);
      const targets = Array.isArray(invalidate) ? invalidate : invalidate ? [invalidate] : [];
      targets.forEach(t => qc.invalidateQueries(t));
    },
  });

  return {
    mutate: m.mutate,
    isLoading: m.isPending,
    error: (m.error as TError) ?? null,
  };
}
