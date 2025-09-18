import { useMutation, useQueryClient } from '@tanstack/react-query';
import { PrefsApi } from '../lib/prefs';

type Ctx = { prev: string[] };

export function useRemoveKeyword() {
  const qc = useQueryClient();

  const { mutate, isPending, reset } = useMutation<void, Error, string, Ctx>({
    mutationFn: (kw) => PrefsApi.remove(kw).then(() => {}),
    onMutate: async (kw) => {
      await qc.cancelQueries({ queryKey: ['prefs', 'keywords'] });
      const prev = (qc.getQueryData<string[]>(['prefs', 'keywords']) ?? []).slice();
      qc.setQueryData<string[]>(['prefs', 'keywords'], prev.filter(x => x !== kw));
      return { prev };
    },
    onError: (_e, _kw, ctx) => {
      if (ctx?.prev) qc.setQueryData(['prefs', 'keywords'], ctx.prev);
    },
    onSettled: () => {
      qc.invalidateQueries({ queryKey: ['prefs', 'keywords'] });
      qc.invalidateQueries({ queryKey: ['news', 'for-me'] });
    },
  });

  return { removeKeyword: mutate, removing: isPending, reset };
}
