import { defineStore } from 'pinia'
import { api } from '../api'
import type { CompareProfile, HistorySummary, InterfaceRequest } from '../types'

// 接口比较页跳转时一次性携带的请求载荷：写入后由 InterfaceView 消费并清空
export interface InterfaceRestorePayload {
  oldRequest: InterfaceRequest
  newRequest: InterfaceRequest
}

export const useAppStore = defineStore('app', {
  state: () => ({
    profiles: [] as CompareProfile[],
    recentHistory: [] as HistorySummary[],
    activeProfile: '默认规则',
    sidebarCollapsed: false,
    darkMode: false,
    initialized: false,
    interfaceRestore: null as InterfaceRestorePayload | null,
  }),
  actions: {
    async initialize() {
      if (this.initialized) return
      try {
        const [profiles, history] = await Promise.all([api.profiles(), api.history(1, 5)])
        this.profiles = profiles
        this.recentHistory = history.items
        if (profiles.length && !profiles.some((profile) => profile.name === this.activeProfile)) {
          this.activeProfile = profiles[0].name
        }
      } finally {
        this.initialized = true
      }
    },
    async refreshHistory() {
      const history = await api.history(1, 5)
      this.recentHistory = history.items
    },
    toggleSidebar() {
      this.sidebarCollapsed = !this.sidebarCollapsed
    },
    toggleDarkMode() {
      this.darkMode = !this.darkMode
      document.documentElement.classList.toggle('is-dark', this.darkMode)
    },
    setInterfaceRestore(payload: InterfaceRestorePayload) {
      this.interfaceRestore = payload
    },
    // 读取后立即清空，避免刷新页面时被旧数据覆盖
    consumeInterfaceRestore() {
      const payload = this.interfaceRestore
      this.interfaceRestore = null
      return payload
    },
  },
})

