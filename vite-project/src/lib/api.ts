import axios from "axios";
import { getToken, logout } from "./auth";

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE || "http://localhost:5001",
});


api.interceptors.request.use((config) => {
  // JWT
  const t = getToken();
  if (t) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${t}`;
  } else if (config?.headers?.Authorization) {
    delete (config.headers as any).Authorization;
  }

  let url = config.url ?? "";
  if (!/^https?:\/\//i.test(url)) {
    if (!url.startsWith("/")) url = "/" + url;
    if (!url.startsWith("/api/")) url = "/api" + url;
    config.url = url;
  }

  return config;
});

api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err?.response?.status === 401) {
      logout();
    }
    return Promise.reject(err);
  }
);
