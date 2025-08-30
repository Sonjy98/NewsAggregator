// src/lib/api.ts
import axios from 'axios'
import { getToken, logout } from './auth'

export const api = axios.create({
  baseURL: '/api',
})

// attach JWT on each request
api.interceptors.request.use((config) => {
  const t = getToken()
  if (t) config.headers.Authorization = `Bearer ${t}`
  return config
})

// optional: central 401 handling (no hard reload)
api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err?.response?.status === 401) {
      logout()
      // let the UI react to missing token (App will show <Login/>)
    }
    return Promise.reject(err)
  }
)
