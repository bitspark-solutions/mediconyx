"use client";
import { useAuthStore } from "../store/useAuthStore";

const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765";

async function refreshToken(): Promise<string | null> {
  const { refreshToken, setTokens, logout } = useAuthStore.getState();
  if (!refreshToken) return null;
  try {
    const res = await fetch(`${apiBase}/api/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    });
    if (!res.ok) {
      // Refresh failed - token is invalid/expired, logout user
      logout();
      return null;
    }
    const data = await res.json();
    setTokens(data.accessToken, data.refreshToken || refreshToken);
    return data.accessToken;
  } catch {
    logout();
    return null;
  }
}

async function request(path: string, init: RequestInit = {}, retry = true): Promise<Response> {
  const { accessToken } = useAuthStore.getState();
  const headers = new Headers(init.headers || {});
  headers.set("Content-Type", headers.get("Content-Type") || "application/json");
  if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);
  const res = await fetch(`${apiBase}${path}`, { ...init, headers });
  if (res.status !== 401 || !retry) return res;
  const newToken = await refreshToken();
  if (!newToken) return res;
  headers.set("Authorization", `Bearer ${newToken}`);
  return fetch(`${apiBase}${path}`, { ...init, headers });
}

export const apiClient = {
  get: (path: string) => request(path, { method: "GET" }),
  post: (path: string, body: any) => request(path, { method: "POST", body: JSON.stringify(body) }),
  put: (path: string, body: any) => request(path, { method: "PUT", body: JSON.stringify(body) }),
  patch: (path: string, body: any) => request(path, { method: "PATCH", body: JSON.stringify(body) }),
  delete: (path: string) => request(path, { method: "DELETE" }),
};

