// src/hooks/useSendDigest.ts
import { useAppMutation } from './useAppMutation';
import { sendDigest as sendDigestApi } from '../lib/email';

export function useSendDigest() {
  // sendDigestApi(limit?: number) -> Promise<unknown>
  const m = useAppMutation<unknown, Error, number>({
    mutationFn: (limit) => sendDigestApi(limit),
  });

  return {
    sendDigest: m.mutate,  // (limit: number, opts?) => void
    sending: m.isLoading,  // boolean
    error: m.error,        // Error | null
  };
}
