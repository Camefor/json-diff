<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ChevronDown, Clock3, FileJson2, Globe2, Play, Plus, Settings2, ShieldCheck, Trash2 } from 'lucide-vue-next'
import { ElMessage } from 'element-plus'
import DiffSummary from '../components/DiffSummary.vue'
import DiffTable from '../components/DiffTable.vue'
import AdvancedOptionsDrawer from '../components/AdvancedOptionsDrawer.vue'
import { api, apiErrorMessage } from '../api'
import { useAppStore } from '../stores/app'
import { defaultOptions } from '../types'
import type { CompareOptions, InterfaceCompareResponse, InterfaceRequest } from '../types'

const appStore = useAppStore()
const oldRequest = ref<InterfaceRequest>({ url: 'http://localhost:5297/api/health', method: 'GET', headers: {}, query: {}, body: '' })
const newRequest = ref<InterfaceRequest>({ url: 'http://localhost:5297/api/health', method: 'GET', headers: {}, query: {}, body: '' })
const oldHeadersText = ref('{}')
const newHeadersText = ref('{}')
const oldQueryText = ref('{}')
const newQueryText = ref('{}')
const options = ref<CompareOptions>(defaultOptions())
const advancedVisible = ref(false)
const activeTab = ref<'headers' | 'query' | 'body'>('headers')
const comparing = ref(false)
const result = ref<InterfaceCompareResponse | null>(null)

const methodOptions = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE']
const statusLabel = computed(() => result.value ? `${result.value.oldResponse.statusCode} / ${result.value.newResponse.statusCode}` : '等待请求')

function parseRecord(text: string, field: string): Record<string, string> {
  if (!text.trim()) return {}
  try {
    const value = JSON.parse(text)
    if (value && typeof value === 'object' && !Array.isArray(value)) return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, String(item)]))
  } catch { throw new Error(`${field} 不是有效 JSON`) }
  throw new Error(`${field} 必须是 JSON 对象`)
}

function syncRequests() {
  oldRequest.value.headers = parseRecord(oldHeadersText.value, '基准 Header')
  newRequest.value.headers = parseRecord(newHeadersText.value, '目标 Header')
  oldRequest.value.query = parseRecord(oldQueryText.value, '基准 Query')
  newRequest.value.query = parseRecord(newQueryText.value, '目标 Query')
}

async function compareInterface() {
  try { syncRequests() } catch (error) { ElMessage.warning(error instanceof Error ? error.message : '请求配置无效'); return }
  if (!oldRequest.value.url || !newRequest.value.url) { ElMessage.warning('请填写旧接口和新接口 URL'); return }
  comparing.value = true
  try {
    result.value = await api.compareInterface({ name: '接口工作台比较', oldRequest: oldRequest.value, newRequest: newRequest.value, options: options.value })
    ElMessage.success(result.value.result.isEqual ? '接口响应一致' : `接口响应存在 ${result.value.result.summary.total} 处差异`)
  } catch (error) { ElMessage.error(apiErrorMessage(error)) } finally { comparing.value = false }
}

function addHeader(side: 'old' | 'new') {
  const text = side === 'old' ? oldHeadersText : newHeadersText
  try { const value = parseRecord(text.value, 'Header'); value['X-Compare-Trace'] = 'enabled'; text.value = JSON.stringify(value, null, 2) } catch { text.value = '{\n  "X-Compare-Trace": "enabled"\n}' }
}

function clearRequest(side: 'old' | 'new') {
  if (side === 'old') { oldHeadersText.value = '{}'; oldQueryText.value = '{}'; oldRequest.value.body = '' } else { newHeadersText.value = '{}'; newQueryText.value = '{}'; newRequest.value.body = '' }
}

// 从历史记录跳转过来时，store 里携带了请求快照，回填到表单
onMounted(() => {
  const payload = appStore.consumeInterfaceRestore()
  if (!payload) return
  const normalize = (request: InterfaceRequest): InterfaceRequest => ({
    url: request.url ?? '',
    method: request.method ?? 'GET',
    headers: { ...(request.headers ?? {}) },
    query: { ...(request.query ?? {}) },
    body: request.body ?? '',
  })
  oldRequest.value = normalize(payload.oldRequest)
  newRequest.value = normalize(payload.newRequest)
  oldHeadersText.value = JSON.stringify(oldRequest.value.headers, null, 2)
  newHeadersText.value = JSON.stringify(newRequest.value.headers, null, 2)
  oldQueryText.value = JSON.stringify(oldRequest.value.query, null, 2)
  newQueryText.value = JSON.stringify(newRequest.value.query, null, 2)
  result.value = null
  ElMessage.success('已从历史记录回填接口请求')
})
</script>

<template>
  <div class="view interface-view">
    <div class="view-heading"><div><p class="eyebrow">REQUEST RUNNER / 02</p><h1>接口比较</h1><p>配置旧、新接口请求，自动拉取响应并生成同一套差异报告。</p></div><div class="heading-actions"><el-button plain @click="advancedVisible = true"><Settings2 :size="15" />比较规则</el-button><el-button type="primary" :loading="comparing" @click="compareInterface"><Play :size="15" fill="currentColor" />发送并比较</el-button></div></div>

    <section class="interface-workspace panel">
      <div class="interface-toolbar"><div class="panel-title"><Globe2 :size="16" class="title-icon" />请求配置<span class="subtle">两侧请求会按独立配置发送</span></div><div class="interface-toolbar-right"><span class="secure-note"><ShieldCheck :size="13" />请求在服务端执行</span><el-button text size="small" @click="clearRequest('old'); clearRequest('new')"><Trash2 :size="13" />清空参数</el-button></div></div>
      <div class="request-grid">
        <div class="request-card request-old"><div class="request-card-head"><div class="request-title"><span class="endpoint-dot old"></span><strong>基准接口</strong><span>旧版本</span></div><el-tag size="small" effect="plain" type="success">BASELINE</el-tag></div><div class="request-url-row"><el-select v-model="oldRequest.method" size="small" class="method-select"><el-option v-for="method in methodOptions" :key="method" :label="method" :value="method" /></el-select><el-input v-model="oldRequest.url" size="small" placeholder="https://legacy-api.example.com/v1/resource" /></div><div class="request-settings"><button v-for="tab in [{ key: 'headers', label: 'Headers' }, { key: 'query', label: 'Query' }, { key: 'body', label: 'Body' }]" :key="tab.key" :class="{ active: activeTab === tab.key }" type="button" @click="activeTab = tab.key as 'headers' | 'query' | 'body'">{{ tab.label }}<span v-if="tab.key === 'headers' && oldHeadersText !== '{}'">1</span></button></div><div class="request-editor"><el-input v-if="activeTab === 'headers'" v-model="oldHeadersText" type="textarea" :rows="7" spellcheck="false" placeholder="{\n  &quot;Authorization&quot;: &quot;Bearer ...&quot;\n}" /><el-input v-else-if="activeTab === 'query'" v-model="oldQueryText" type="textarea" :rows="7" spellcheck="false" placeholder="{\n  &quot;page&quot;: &quot;1&quot;\n}" /><el-input v-else v-model="oldRequest.body" type="textarea" :rows="7" spellcheck="false" placeholder="请求 Body（JSON）" /></div><div class="request-card-foot"><span><Clock3 :size="13" />超时 30 秒</span><button type="button" @click="addHeader('old')"><Plus :size="13" />添加 Header</button></div></div>

        <div class="request-card request-new"><div class="request-card-head"><div class="request-title"><span class="endpoint-dot new"></span><strong>目标接口</strong><span>新版本</span></div><el-tag size="small" effect="plain" type="warning">TARGET</el-tag></div><div class="request-url-row"><el-select v-model="newRequest.method" size="small" class="method-select"><el-option v-for="method in methodOptions" :key="method" :label="method" :value="method" /></el-select><el-input v-model="newRequest.url" size="small" placeholder="https://new-api.example.com/v2/resource" /></div><div class="request-settings"><button v-for="tab in [{ key: 'headers', label: 'Headers' }, { key: 'query', label: 'Query' }, { key: 'body', label: 'Body' }]" :key="tab.key" :class="{ active: activeTab === tab.key }" type="button" @click="activeTab = tab.key as 'headers' | 'query' | 'body'">{{ tab.label }}<span v-if="tab.key === 'headers' && newHeadersText !== '{}'">1</span></button></div><div class="request-editor"><el-input v-if="activeTab === 'headers'" v-model="newHeadersText" type="textarea" :rows="7" spellcheck="false" placeholder="{\n  &quot;Authorization&quot;: &quot;Bearer ...&quot;\n}" /><el-input v-else-if="activeTab === 'query'" v-model="newQueryText" type="textarea" :rows="7" spellcheck="false" placeholder="{\n  &quot;page&quot;: &quot;1&quot;\n}" /><el-input v-else v-model="newRequest.body" type="textarea" :rows="7" spellcheck="false" placeholder="请求 Body（JSON）" /></div><div class="request-card-foot"><span><Clock3 :size="13" />超时 30 秒</span><button type="button" @click="addHeader('new')"><Plus :size="13" />添加 Header</button></div></div>
      </div>
      <div class="interface-runbar"><div><FileJson2 :size="15" /><span>仅支持返回 JSON 的接口</span></div><el-button type="primary" :loading="comparing" @click="compareInterface"><Play :size="14" fill="currentColor" />发送并比较</el-button></div>
    </section>

    <section v-if="result" class="interface-result panel"><div class="response-meta"><div><span>响应状态</span><strong>{{ statusLabel }}</strong></div><div><span>基准耗时</span><strong>{{ result.oldResponse.durationMs }} ms</strong></div><div><span>目标耗时</span><strong>{{ result.newResponse.durationMs }} ms</strong></div><div class="meta-url"><span>目标请求地址</span><code>{{ result.newResponse.url }}</code></div></div><DiffSummary :result="result.result" /><DiffTable :differences="result.result.differences" /></section>
    <section v-else class="interface-placeholder panel"><Globe2 :size="25" /><div><strong>配置请求后开始接口比较</strong><span>服务端会分别执行两侧请求，并保留响应历史</span></div><ChevronDown :size="16" /></section>
    <AdvancedOptionsDrawer v-model:visible="advancedVisible" v-model="options" />
  </div>
</template>

<style scoped>
.interface-view { max-width: 1480px; margin: 0 auto; }.interface-workspace { overflow: hidden; }.interface-toolbar, .interface-toolbar-right, .secure-note { display: flex; align-items: center; }.interface-toolbar { min-height: 57px; justify-content: space-between; padding: 0 18px; border-bottom: 1px solid var(--line); }.interface-toolbar .panel-title { gap: 8px; }.interface-toolbar .subtle { margin-left: 5px; font-weight: 400; }.interface-toolbar-right { gap: 10px; }.secure-note { gap: 5px; color: var(--teal-dark); font-size: 10px; }.request-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1px; background: var(--line); }.request-card { min-width: 0; background: var(--surface); padding: 16px 17px 13px; }.request-card-head, .request-title, .request-url-row, .request-card-foot, .interface-runbar, .response-meta { display: flex; align-items: center; }.request-card-head { justify-content: space-between; margin-bottom: 13px; }.request-title { gap: 7px; }.request-title strong { font-size: 12px; }.request-title span:last-child { color: var(--muted); font-size: 10px; }.request-url-row { gap: 7px; }.method-select { width: 92px; flex: 0 0 92px; }.request-settings { display: flex; gap: 3px; border-bottom: 1px solid var(--line-soft); margin-top: 17px; }.request-settings button { display: inline-flex; gap: 5px; align-items: center; padding: 7px 9px 9px; border: 0; border-bottom: 2px solid transparent; background: transparent; color: var(--muted); font-size: 10px; }.request-settings button.active { border-bottom-color: var(--teal); color: var(--teal-dark); font-weight: 700; }.request-settings button span { min-width: 14px; padding: 1px 4px; border-radius: 8px; background: var(--mint); color: var(--teal-dark); }.request-editor { padding-top: 11px; }.request-editor :deep(.el-textarea__inner) { font: 11px/1.55 'DM Mono', monospace; min-height: 140px !important; resize: vertical; }.request-card-foot { justify-content: space-between; padding-top: 10px; color: var(--muted); font-size: 10px; }.request-card-foot span, .request-card-foot button { display: inline-flex; gap: 5px; align-items: center; }.request-card-foot button { border: 0; background: transparent; color: var(--teal-dark); font-size: 10px; font-weight: 700; }.interface-runbar { justify-content: space-between; padding: 13px 17px; border-top: 1px solid var(--line); }.interface-runbar > div { display: flex; align-items: center; gap: 6px; color: var(--muted); font-size: 10px; }.interface-result { overflow: hidden; margin-top: 22px; }.response-meta { min-height: 73px; gap: 35px; padding: 0 18px; border-bottom: 1px solid var(--line); }.response-meta > div { display: flex; flex-direction: column; gap: 6px; }.response-meta span { color: var(--muted); font-size: 10px; }.response-meta strong { color: var(--ink); font: 700 13px 'DM Mono', monospace; }.response-meta > div:first-child strong { color: var(--teal-dark); }.response-meta .meta-url { min-width: 0; margin-left: auto; }.meta-url code { max-width: 420px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: var(--ink-soft); font: 10px 'DM Mono', monospace; }.interface-placeholder { min-height: 126px; margin-top: 22px; display: flex; justify-content: center; align-items: center; gap: 13px; color: var(--teal-dark); }.interface-placeholder strong, .interface-placeholder span { display: block; }.interface-placeholder strong { color: var(--ink); font-size: 13px; }.interface-placeholder span { margin-top: 5px; color: var(--muted); font-size: 11px; }.interface-placeholder > :last-child { color: var(--muted); margin-left: 8px; }
@media (max-width: 850px) { .request-grid { grid-template-columns: 1fr; }.interface-toolbar { align-items: flex-start; flex-direction: column; gap: 10px; padding-block: 12px; }.response-meta { align-items: flex-start; flex-wrap: wrap; gap: 15px 28px; padding-block: 15px; }.response-meta .meta-url { width: 100%; margin-left: 0; }.meta-url code { max-width: 100%; } }
</style>

