import { useQueryClient } from '@tanstack/react-query';
import { useAppMutation } from './useAppMutation';
import { AuthApi, saveSession, getUserEmail } from '../lib/auth';
import type { AuthResponse } from '../types/auth';

export function useAuth() {
  const qc = useQueryClient();

  const login = useAppMutation<AuthResponse, Error, { email: string; password: string }>({
    mutationFn: (vars) => AuthApi.login(vars),
    onSuccess: (session) => {
      saveSession(session);
      qc.invalidateQueries();
    },
  });

  const register = useAppMutation<AuthResponse, Error, { email: string; password: string }>({
    mutationFn: (vars) => AuthApi.register(vars),
    onSuccess: (session) => {
      saveSession(session);
      qc.invalidateQueries();
    },
  });

  function logout() {
    qc.clear();
  }

  return {
    login: login.mutate,
    loggingIn: login.isLoading,
    loginError: login.error,
    register: register.mutate,
    registering: register.isLoading,
    registerError: register.error,
    userEmail: getUserEmail?.() ?? '',
    logout,
  };
}
