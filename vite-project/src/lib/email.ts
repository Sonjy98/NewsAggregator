import { api } from "./api"
import { getToken } from "./auth"

export async function sendDigest(max = 10) {
  const token = getToken()
  if (!token) throw new Error("Please log in first.")

  try {
    const res = await api.post(`/email/send?max=${max}`)
    return res.data
  } catch (err: any) {
    if (err.response?.data) {
      const j = err.response.data
      throw new Error(
        j?.detail || j?.title || j?.message || `HTTP ${err.response.status}`
      )
    }
    throw err instanceof Error ? err : new Error("Request failed")
  }
}