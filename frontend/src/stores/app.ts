import { defineStore } from 'pinia'
import { api } from '../api'
import type { CompareProfile, HistorySummary } from '../types'

export const useAppStore = defineStore('app', {
  state: () => ({
    profiles: [] as CompareProfile[],
    recentHistory: [] as HistorySummary[],
    activeProfile: '默认规则',
    sidebarCollapsed: false,
    darkMode: false,
    initialized: false,
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
  },
})

