import axios from "axios";
import { getToken, logout } from "./auth";

const rawBase = (import.meta.env.VITE_API_BASE ?? "/api").trim();
const baseURL = rawBase.replace(/\/+$/, "");
console.log(baseURL);

export const api = axios.create({
  baseURL,
  withCredentials: true,
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

  (config as any).__t0 = performance.now();

  if (import.meta.env.DEV && typeof config.url === "string" && config.url.startsWith("/api/")) {
    console.warn(
      `[api] Avoid passing URLs starting with "/api". Use relative paths like "/auth/login". Got: ${config.url}`
    );
  }

  return config;
});

api.interceptors.response.use(
  (res) => {
    if (import.meta.env.DEV) {
      const t0 = (res.config as any).__t0 ?? performance.now();
      const ms = (performance.now() - t0).toFixed(1);
      const method = (res.config.method || "GET").toUpperCase();
      const url = res.config.url || "";
      const reqId = res.headers["x-request-id"];
      console.groupCollapsed(
        `✅ ${res.status} ${method} ${url} (${ms} ms)` + (reqId ? `  #${reqId}` : "")
      );
      console.debug("request", {
        url,
        params: res.config.params,
        data: res.config.data,
      });
      console.debug("response", {
        status: res.status,
        data: res.data,
        requestId: reqId,
      });
      console.groupEnd();
    }
    return res;
  },
  (err) => {
    if (import.meta.env.DEV) {
      const cfg = err.config || {};
      const t0 = (cfg as any).__t0 ?? performance.now();
      const ms = (performance.now() - t0).toFixed(1);
      const method = (cfg.method || "GET").toUpperCase();
      const url = cfg.url || "";
      const status = err.response?.status ?? "ERR";
      const reqId =
        err.response?.headers?.["x-request-id"] ??
        err.response?.data?.traceId ??
        err.response?.data?.trace_id;
      console.groupCollapsed(
        `❌ ${status} ${method} ${url} (${ms} ms)` + (reqId ? `  #${reqId}` : "")
      );
      console.warn("request", { url, params: cfg.params, data: cfg.data });
      console.error("error", {
        status: err.response?.status,
        data: err.response?.data,
        requestId: reqId,
      });
      console.groupEnd();
    }
    if (err?.response?.status === 401) {
      logout();
    }
    return Promise.reject(err);
  }
);
