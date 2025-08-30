import { api } from './api'

export const PrefsApi = {
  async list(): Promise<string[]> {
    const { data } = await api.get('/preferences')
    return data
  },

  async add(keyword: string): Promise<string[]> {
    const { data } = await api.post('/preferences', { keyword })
    return data
  },

  async remove(keyword: string): Promise<void> {
    await api.delete(`/preferences/${encodeURIComponent(keyword)}`)
  },
}
