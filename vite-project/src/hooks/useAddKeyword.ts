import { useMutation, useQueryClient } from '@tanstack/react-query';
import { PrefsApi } from '../lib/prefs';

type Ctx = { prev: string[] };

export function useAddKeyword() {
  const qc = useQueryClient();

  const { mutate, isPending, reset } = useMutation<void, Error, string, Ctx>({
    mutationFn: (kw) => PrefsApi.add(kw).then(() => {}),
    onMutate: async (kw) => {
      await qc.cancelQueries({ queryKey: ['prefs', 'keywords'] });
      const prev = (qc.getQueryData<string[]>(['prefs', 'keywords']) ?? []).slice();
      qc.setQueryData<string[]>(['prefs', 'keywords'], [...prev, kw]);
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

  return { addKeyword: mutate, adding: isPending, reset };
}
