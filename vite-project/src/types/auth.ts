export interface AuthResponse { userId: string; email: string; token: string; }
export interface RegisterRequest { email: string; password: string; }
export interface LoginRequest    { email: string; password: string; }
