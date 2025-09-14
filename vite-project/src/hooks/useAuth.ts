import { useQueryClient } from '@tanstack/react-query';
import { useAppMutation } from './useAppMutation';
import { AuthApi, saveSession, getUserEmail, clearSession } from '../lib/auth';
import type { AuthResponse } from '../types/auth';

type LoginRequest = { email: string; password: string };
type RegisterRequest = { email: string; password: string };

export function useAuth() {
  const qc = useQueryClient();

  const login = useAppMutation<AuthResponse, Error, LoginRequest>({
    mutationFn: (req) => AuthApi.login(req),
    onSuccess: (session) => {
      saveSession(session);
      // invalidate whatever depends on auth
      qc.invalidateQueries({ queryKey: ['prefs'] });
      qc.invalidateQueries({ queryKey: ['prefs', 'keywords'] });
      qc.invalidateQueries({ queryKey: ['news'] });
      qc.invalidateQueries({ queryKey: ['news', 'for-me'] });
    },
  });

  const register = useAppMutation<AuthResponse, Error, RegisterRequest>({
    mutationFn: (req) => AuthApi.register(req),
    onSuccess: (session) => {
      saveSession(session);
      qc.invalidateQueries({ queryKey: ['prefs'] });
      qc.invalidateQueries({ queryKey: ['prefs', 'keywords'] });
      qc.invalidateQueries({ queryKey: ['news'] });
      qc.invalidateQueries({ queryKey: ['news', 'for-me'] });
    },
  });

  const logout = () => {
    clearSession?.();
    qc.clear();
  };

  return {
    login: login.mutate,
    loggingIn: login.isLoading,
    loginError: login.error,

    register: register.mutate,
    registering: register.isLoading,
    registerError: register.error,

    userEmail: getUserEmail?.() || '',
    logout,
  };
}
