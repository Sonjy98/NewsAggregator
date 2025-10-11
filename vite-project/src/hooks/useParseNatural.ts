import { useMutation, useQueryClient } from "@tanstack/react-query";
import { PrefsApi, type NLResponse } from "../lib/prefs";

export function useParseNatural() {
  const qc = useQueryClient();

  const mutation = useMutation<NLResponse, Error, string>({
    mutationFn: (query: string) => PrefsApi.parseNatural(query),
    onSuccess: () => {
      // refresh keywords after backend saved any new ones
      qc.invalidateQueries({ queryKey: ["keywords"] });
    },
  });

  return {
    parseNatural: (
      query: string,
      opts?: { onSuccess?: (r: NLResponse) => void; onError?: (e: Error) => void }
    ) => mutation.mutate(query, opts),
    data: mutation.data,
    parsing: mutation.isPending,
    error: mutation.error,
    reset: mutation.reset,
  };
}
