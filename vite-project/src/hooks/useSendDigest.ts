import { useAppMutation } from './useAppMutation';
import { sendDigest } from '../lib/email';

export function useSendDigest() {
  return useAppMutation<{ sentTo?: string }, Error, number>({
    mutationFn: (limit) => sendDigest(limit),
  });
}
