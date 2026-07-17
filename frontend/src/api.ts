import axios from 'axios'
import type {
  BatchCompareRequest,
  BatchCompareResponse,
  CompareJsonRequest,
  CompareJsonResponse,
  CompareProfile,
  HistoryQueryResponse,
  HistoryRecord,
  InterfaceCompareRequest,
  InterfaceCompareResponse,
} from './types'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  headers: { 'Content-Type': 'application/json' },
})

export const api = {
  async compareJson(payload: CompareJsonRequest) {
    const { data } = await http.post<CompareJsonResponse>('/compare/json', payload)
    return data
  },
  async compareInterface(payload: InterfaceCompareRequest) {
    const { data } = await http.post<InterfaceCompareResponse>('/compare/interface', payload)
    return data
  },
  async compareBatch(payload: BatchCompareRequest) {
    const { data } = await http.post<BatchCompareResponse>('/compare/batch', payload)
    return data
  },
  async history(page = 1, pageSize = 20, keyword = '') {
    const { data } = await http.get<HistoryQueryResponse>('/history', { params: { page, pageSize, keyword } })
    return data
  },
  async historyDetail(id: string) {
    const { data } = await http.get<HistoryRecord>(`/history/${encodeURIComponent(id)}`)
    return data
  },
  async deleteHistory(id: string) {
    await http.delete(`/history/${encodeURIComponent(id)}`)
  },
  async profiles() {
    const { data } = await http.get<CompareProfile[]>('/config/profile')
    return data
  },
  async saveProfile(payload: Omit<CompareProfile, 'updatedAt'>) {
    const { data } = await http.post<CompareProfile>('/config/profile', payload)
    return data
  },
  async deleteProfile(name: string) {
    await http.delete(`/config/profile/${encodeURIComponent(name)}`)
  },
  reportUrl(id: string, format: string) {
    const base = import.meta.env.VITE_API_BASE_URL || '/api'
    return `${base}/report/${encodeURIComponent(id)}?format=${encodeURIComponent(format)}`
  },
}

export function apiErrorMessage(error: unknown, fallback = '请求失败，请检查 API 服务是否正常。') {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string; detail?: string } | undefined
    return data?.detail || data?.message || error.message || fallback
  }
  return error instanceof Error ? error.message : fallback
}

