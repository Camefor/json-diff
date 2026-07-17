<script setup lang="ts">
import { computed, onMounted, type Component } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  Activity,
  ChevronRight,
  CircleHelp,
  Database,
  FileDiff,
  GitCompareArrows,
  History,
  Layers3,
  Menu,
  Moon,
  PanelLeftClose,
  PanelLeftOpen,
  Settings2,
  SlidersHorizontal,
  Sun,
  Wrench,
  Waypoints,
} from 'lucide-vue-next'
import { useAppStore } from './stores/app'

interface NavItem {
  label: string
  path: string
  icon: Component
  shortcut?: string
}

const router = useRouter()
const route = useRoute()
const store = useAppStore()

const primaryNav: NavItem[] = [
  { label: 'JSON 比较', path: '/compare', icon: GitCompareArrows, shortcut: '01' },
  { label: '接口比较', path: '/interface', icon: Waypoints, shortcut: '02' },
  { label: '批量比较', path: '/batch', icon: Layers3, shortcut: '03' },
]

const secondaryNav: NavItem[] = [
  { label: '历史记录', path: '/history', icon: History },
  { label: '配置中心', path: '/config', icon: SlidersHorizontal },
  { label: '系统设置', path: '/settings', icon: Settings2 },
]

const toolNav: NavItem[] = [
  { label: 'JSON 小工具', path: '/tools/json', icon: Wrench },
]

const pageTitle = computed(() => (route.meta.title as string) || 'JSON 比较')
const isActive = (path: string) => route.path === path

function go(path: string) {
  router.push(path)
}

onMounted(() => {
  store.initialize()
})
</script>

<template>
  <div class="app-shell" :class="{ 'sidebar-collapsed': store.sidebarCollapsed }">
    <aside class="sidebar">
      <div class="brand-lockup">
        <div class="brand-mark"><FileDiff :size="20" stroke-width="2.2" /></div>
        <div class="brand-copy">
          <strong>JSON Compare</strong>
          <span>接口响应验证台</span>
        </div>
      </div>

      <nav class="nav-area" aria-label="主导航">
        <p class="nav-label">COMPARE</p>
        <button
          v-for="item in primaryNav"
          :key="item.path"
          class="nav-item"
          :class="{ active: isActive(item.path) }"
          type="button"
          @click="go(item.path)"
        >
          <component :is="item.icon" :size="18" stroke-width="1.9" />
          <span class="nav-text">{{ item.label }}</span>
          <kbd v-if="item.shortcut" class="nav-kbd">{{ item.shortcut }}</kbd>
        </button>

        <p class="nav-label nav-label-secondary">MANAGE</p>
        <button
          v-for="item in secondaryNav"
          :key="item.path"
          class="nav-item"
          :class="{ active: isActive(item.path) }"
          type="button"
          @click="go(item.path)"
        >
          <component :is="item.icon" :size="18" stroke-width="1.9" />
          <span class="nav-text">{{ item.label }}</span>
        </button>

        <p class="nav-label nav-label-secondary">TOOLS</p>
        <button
          v-for="item in toolNav"
          :key="item.path"
          class="nav-item"
          :class="{ active: isActive(item.path) }"
          type="button"
          @click="go(item.path)"
        >
          <component :is="item.icon" :size="18" stroke-width="1.9" />
          <span class="nav-text">{{ item.label }}</span>
        </button>
      </nav>

      <div class="sidebar-bottom">
        <div class="engine-status">
          <span class="status-dot"></span>
          <div class="status-copy"><span>比较引擎</span><strong>在线 · v1.0</strong></div>
          <Activity :size="16" class="status-activity" />
        </div>
        <button class="user-card" type="button">
          <span class="user-avatar">R</span>
          <span class="user-copy"><strong>Review Team</strong><small>本地工作区</small></span>
          <Settings2 :size="15" class="muted-icon" />
        </button>
      </div>
    </aside>

    <section class="main-shell">
      <header class="topbar">
        <div class="topbar-left">
          <el-tooltip :content="store.sidebarCollapsed ? '展开侧栏' : '收起侧栏'" placement="bottom">
            <button class="icon-button mobile-menu" type="button" @click="store.toggleSidebar()">
              <PanelLeftOpen v-if="store.sidebarCollapsed" :size="18" />
              <PanelLeftClose v-else :size="18" />
            </button>
          </el-tooltip>
          <div class="breadcrumbs"><span>工作台</span><ChevronRight :size="14" /><strong>{{ pageTitle }}</strong></div>
        </div>
        <div class="topbar-actions">
          <div class="topbar-stat"><Database :size="15" /><span>{{ store.profiles.length || 1 }} 个规则</span></div>
          <el-tooltip content="帮助文档" placement="bottom">
            <button class="icon-button" type="button"><CircleHelp :size="18" /></button>
          </el-tooltip>
          <el-tooltip :content="store.darkMode ? '切换亮色' : '切换深色'" placement="bottom">
            <button class="icon-button" type="button" @click="store.toggleDarkMode()">
              <Sun v-if="store.darkMode" :size="18" />
              <Moon v-else :size="18" />
            </button>
          </el-tooltip>
          <div class="topbar-divider"></div>
          <div class="topbar-build"><span class="online-pulse"></span>本地服务已连接</div>
        </div>
      </header>

      <main class="content-area">
        <router-view v-slot="{ Component }">
          <transition name="page" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </main>
    </section>
  </div>
</template>

