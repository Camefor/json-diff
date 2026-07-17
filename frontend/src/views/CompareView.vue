<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { Braces, ChevronDown, Download, FileCheck2, History, Play, RotateCcw, Settings2, SlidersHorizontal, WandSparkles } from 'lucide-vue-next'
import { ElMessage } from 'element-plus'
import JsonEditor from '../components/JsonEditor.vue'
import DiffSummary from '../components/DiffSummary.vue'
import DiffTable from '../components/DiffTable.vue'
import AdvancedOptionsDrawer from '../components/AdvancedOptionsDrawer.vue'
import { api, apiErrorMessage } from '../api'
import { useAppStore } from '../stores/app'
import { cloneOptions, defaultOptions } from '../types'
import type { CompareJsonResponse, CompareOptions } from '../types'

const sampleOld = `{
  "code": 0,
  "message": "success",
  "data": {
    "symbol": "AAPL",
    "price": 189.42,
    "currency": "USD",
    "tags": ["technology", "large-cap"],
    "meta": { "requestId": "old-2024-001" }
  }
}`
const sampleNew = `{
  "code": 0,
  "message": "success",
  "data": {
    "symbol": "AAPL",
    "price": 189.43,
    "currency": "USD",
    "tags": ["large-cap", "technology"],
    "meta": { "requestId": "new-2024-002" },
    "market": "NASDAQ"
  }
}`

const store = useAppStore()
const router = useRouter()
const oldJson = ref(sampleOld)
const newJson = ref(sampleNew)
const options = ref<CompareOptions>(defaultOptions())
const result = ref<CompareJsonResponse | null>(null)
const advancedVisible = ref(false)
const comparing = ref(false)
const oldValid = ref(true)
const newValid = ref(true)
const selectedProfile = ref(store.activeProfile)

const ruleCount = computed(() => options.value.ignorePaths.length + options.value.whitelistPaths.length + options.value.mappings.length)
const differenceText = computed(() => result.value ? `${result.value.summary.total} 条差异` : '等待执行比较')

function formatLocal(value: string) {
  try { return JSON.stringify(JSON.parse(value), null, 2) } catch { return value }
}

function loadSample() {
  oldJson.value = sampleOld
  newJson.value = sampleNew
  result.value = null
  ElMessage.success('已载入演示数据')
}

async function loadProfile(name: string) {
  const profile = store.profiles.find((item) => item.name === name)
  if (profile) {
    options.value = cloneOptions(profile.options)
    store.activeProfile = name
    ElMessage.success(`已切换到规则：${name}`)
  }
}

async function compare() {
  if (!oldValid.value || !newValid.value) {
    ElMessage.warning('请先修复 JSON 格式错误')
    return
  }
  try {
    JSON.parse(oldJson.value)
    JSON.parse(newJson.value)
  } catch {
    ElMessage.warning('请先修复 JSON 格式错误')
    return
  }
  comparing.value = true
  try {
    result.value = await api.compareJson({ oldJson: oldJson.value, newJson: newJson.value, name: 'JSON 工作台比较', options: options.value })
    await store.refreshHistory()
    ElMessage.success(result.value.isEqual ? '比较完成：响应一致' : `比较完成：发现 ${result.value.summary.total} 处差异`)
  } catch (error) {
    ElMessage.error(apiErrorMessage(error))
  } finally {
    comparing.value = false
  }
}

function exportLast(format: string) {
  if (!result.value) { ElMessage.info('请先执行一次比较'); return }
  window.open(api.reportUrl(result.value.id, format), '_blank')
}
</script>

<template>
  <div class="view compare-view">
    <div class="view-heading">
      <div><p class="eyebrow">CORE WORKSPACE / 01</p><h1>JSON 比较</h1><p>对比接口响应，快速定位字段、类型与值的变化。</p></div>
      <div class="heading-actions"><el-button text @click="loadSample"><RotateCcw :size="15" />载入示例</el-button><el-button plain @click="advancedVisible = true"><Settings2 :size="15" />高级规则<span v-if="ruleCount" class="button-count">{{ ruleCount }}</span></el-button><el-button type="primary" :loading="comparing" @click="compare"><Play :size="15" fill="currentColor" />开始比较</el-button></div>
    </div>

    <section class="compare-workspace panel">
      <div class="workspace-toolbar"><div class="toolbar-left"><div class="compare-mode"><Braces :size="15" class="title-icon" /><strong>JSON 响应</strong><span class="mode-slash">/</span><span>双栏对比</span></div><span class="rule-chip"><SlidersHorizontal :size="12" />{{ selectedProfile || '默认规则' }}</span></div><div class="toolbar-right"><el-select v-model="selectedProfile" size="small" style="width: 140px" :teleported="false" @change="loadProfile"><el-option v-for="profile in store.profiles" :key="profile.name" :label="profile.name" :value="profile.name" /></el-select><button class="toolbar-text-button" type="button" @click="advancedVisible = true"><WandSparkles :size="13" />规则设置</button></div></div>
      <div class="editor-grid">
        <div class="editor-column"><div class="editor-column-head"><div class="endpoint-label"><span class="endpoint-dot old"></span><strong>基准响应</strong><span>旧版本 / baseline</span></div><span class="valid-state" :class="{ invalid: !oldValid }"><FileCheck2 :size="13" />{{ oldValid ? 'JSON 有效' : '格式错误' }}</span></div><div class="editor-frame"><JsonEditor v-model="oldJson" label="BASELINE.JSON" accent="teal" @valid="oldValid = $event" /></div></div>
        <div class="editor-column"><div class="editor-column-head"><div class="endpoint-label"><span class="endpoint-dot new"></span><strong>目标响应</strong><span>新版本 / target</span></div><span class="valid-state" :class="{ invalid: !newValid }"><FileCheck2 :size="13" />{{ newValid ? 'JSON 有效' : '格式错误' }}</span></div><div class="editor-frame"><JsonEditor v-model="newJson" label="TARGET.JSON" accent="amber" @valid="newValid = $event" /></div></div>
      </div>
      <div class="compare-footer"><div class="compare-hint"><span class="shortcut-dot">↵</span><span>支持大 JSON、折叠、搜索与格式化</span></div><div class="footer-actions"><el-button text size="small" @click="oldJson = formatLocal(oldJson); newJson = formatLocal(newJson)"><WandSparkles :size="13" />格式化两侧</el-button><el-button type="primary" size="small" :loading="comparing" @click="compare"><Play :size="13" fill="currentColor" />执行比较</el-button></div></div>
    </section>

    <section v-if="result" class="result-section panel"><DiffSummary :result="result" /><div class="result-head"><div><div class="panel-title"><span class="result-mark"></span>差异结果</div><span class="subtle">{{ differenceText }} · 可按路径、类型筛选</span></div><div class="result-actions"><el-dropdown trigger="click"><el-button plain size="small"><Download :size="14" />导出报告<ChevronDown :size="13" /></el-button><template #dropdown><el-dropdown-menu><el-dropdown-item @click="exportLast('html')">HTML 网页</el-dropdown-item><el-dropdown-item @click="exportLast('markdown')">Markdown</el-dropdown-item><el-dropdown-item @click="exportLast('csv')">CSV</el-dropdown-item><el-dropdown-item @click="exportLast('excel')">Excel</el-dropdown-item><el-dropdown-item @click="exportLast('pdf')">PDF</el-dropdown-item></el-dropdown-menu></template></el-dropdown><el-button text size="small" @click="router.push('/history')"><History :size="14" />查看历史</el-button></div></div><DiffTable :differences="result.differences" /></section>
    <section v-else class="empty-result panel"><div class="empty-result-icon"><GitCompareArrows :size="25" /></div><div><strong>比较结果会显示在这里</strong><span>编辑两侧 JSON 后点击“开始比较”</span></div><div class="empty-rule-chips"><span>Key / Value / Type</span><span>数组主键</span><span>字段忽略</span></div></section>

    <AdvancedOptionsDrawer v-model:visible="advancedVisible" v-model="options" />
  </div>
</template>

<style scoped>
.compare-view { max-width: 1480px; margin: 0 auto; }.button-count { display: inline-grid; place-items: center; min-width: 17px; height: 17px; margin-left: 3px; padding-inline: 4px; color: #fff; background: var(--teal); border-radius: 9px; font-size: 10px; }.compare-workspace { overflow: hidden; }.workspace-toolbar { min-height: 57px; display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 0 18px; border-bottom: 1px solid var(--line); }.toolbar-left, .toolbar-right, .compare-mode, .endpoint-label, .valid-state, .footer-actions, .compare-hint, .toolbar-text-button, .rule-chip, .result-actions { display: flex; align-items: center; }.toolbar-left, .toolbar-right { gap: 13px; }.compare-mode { gap: 8px; color: var(--ink-soft); font-size: 11px; }.compare-mode strong { color: var(--ink); font-size: 13px; }.mode-slash { color: var(--line); }.title-icon { color: var(--teal); }.rule-chip { gap: 5px; padding: 5px 8px; color: var(--teal-dark); background: var(--mint); border-radius: 4px; font-size: 10px; font-weight: 700; }.toolbar-text-button { gap: 5px; padding: 5px 0; border: 0; background: transparent; color: var(--teal-dark); font-size: 11px; font-weight: 700; }.editor-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1px; background: var(--line); }.editor-column { min-width: 0; background: var(--surface); }.editor-column-head { height: 47px; display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 0 15px; }.endpoint-label { gap: 7px; }.endpoint-label strong { color: var(--ink); font-size: 12px; }.endpoint-label span:last-child { color: var(--muted); font-size: 10px; }.endpoint-dot { width: 7px; height: 7px; border-radius: 50%; }.endpoint-dot.old { background: var(--teal); }.endpoint-dot.new { background: var(--amber); }.valid-state { gap: 5px; color: var(--teal-dark); font-size: 10px; }.valid-state.invalid { color: var(--red); }.editor-frame { height: 360px; border-top: 1px solid var(--line-soft); border-bottom: 1px solid var(--line); }.compare-footer { min-height: 49px; display: flex; justify-content: space-between; align-items: center; gap: 10px; padding: 0 15px; }.compare-hint { gap: 7px; color: var(--muted); font-size: 10px; }.shortcut-dot { width: 17px; height: 17px; display: grid; place-items: center; color: var(--teal-dark); background: var(--mint); border-radius: 3px; font: 700 11px 'DM Mono', monospace; }.footer-actions { gap: 6px; }.result-section { overflow: hidden; margin-top: 22px; }.result-head { min-height: 65px; padding: 0 18px; display: flex; align-items: center; justify-content: space-between; gap: 15px; border-bottom: 1px solid var(--line-soft); }.result-head .panel-title { margin-bottom: 5px; }.result-mark { width: 7px; height: 7px; border-radius: 50%; background: var(--red); }.result-actions { gap: 6px; }.empty-result { min-height: 150px; margin-top: 22px; padding: 28px; display: flex; align-items: center; justify-content: center; gap: 15px; color: var(--muted); }.empty-result-icon { width: 48px; height: 48px; display: grid; place-items: center; color: var(--teal-dark); background: var(--mint); border-radius: 50%; }.empty-result strong, .empty-result span { display: block; }.empty-result strong { color: var(--ink); font-size: 13px; }.empty-result span { margin-top: 5px; font-size: 11px; }.empty-rule-chips { display: flex; gap: 5px; margin-left: 18px; }.empty-rule-chips span { padding: 5px 7px; color: var(--muted); border: 1px solid var(--line); border-radius: 4px; font-size: 10px; }
@media (max-width: 760px) { .workspace-toolbar { align-items: flex-start; flex-direction: column; padding-block: 11px; }.toolbar-right { width: 100%; justify-content: space-between; }.editor-grid { grid-template-columns: 1fr; }.editor-frame { height: 300px; }.compare-footer { align-items: flex-start; flex-direction: column; padding-block: 10px; }.empty-result { align-items: flex-start; flex-direction: column; }.empty-rule-chips { margin-left: 0; flex-wrap: wrap; }.result-head { align-items: flex-start; flex-direction: column; padding-block: 14px; }.result-actions { width: 100%; justify-content: flex-end; } }
</style>
