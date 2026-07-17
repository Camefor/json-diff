import { createRouter, createWebHistory } from 'vue-router'
import CompareView from '../views/CompareView.vue'
import InterfaceView from '../views/InterfaceView.vue'
import BatchView from '../views/BatchView.vue'
import HistoryView from '../views/HistoryView.vue'
import ConfigView from '../views/ConfigView.vue'
import SettingsView from '../views/SettingsView.vue'
import ToolView from '../views/ToolView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/compare' },
    { path: '/compare', name: 'compare', component: CompareView, meta: { title: 'JSON 比较' } },
    { path: '/interface', name: 'interface', component: InterfaceView, meta: { title: '接口比较' } },
    { path: '/batch', name: 'batch', component: BatchView, meta: { title: '批量比较' } },
    { path: '/history', name: 'history', component: HistoryView, meta: { title: '历史记录' } },
    { path: '/config', name: 'config', component: ConfigView, meta: { title: '配置中心' } },
    { path: '/settings', name: 'settings', component: SettingsView, meta: { title: '系统设置' } },
    { path: '/tools/json', name: 'json-tool', component: ToolView, meta: { title: 'JSON 小工具' } },
  ],
})

export default router

