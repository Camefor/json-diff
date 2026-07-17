<script setup lang="ts">
import { computed, ref } from 'vue'
import { CheckCircle2, ChevronDown, Download, FileInput, FileJson2, Play, Plus, RotateCcw, Trash2, Upload } from 'lucide-vue-next'
import { ElMessage } from 'element-plus'
import { api, apiErrorMessage } from '../api'
import { defaultOptions } from '../types'
import type { BatchCompareItemRequest, BatchCompareResponse } from '../types'

type BatchItem = BatchCompareItemRequest & { id: string }

const items = ref<BatchItem[]>([
  { id: 'task-1', name: '行情详情 / quote', oldJson: '{"symbol":"AAPL","price":189.42}', newJson: '{"symbol":"AAPL","price":189.43}' },
  { id: 'task-2', name: '用户资料 / profile', oldJson: '{"id":1001,"name":"Ada","active":true}', newJson: '{"id":1001,"name":"Ada Lovelace","active":true}' },
  { id: 'task-3', name: '订单列表 / orders', oldJson: '{"items":[{"id":1,"status":"paid"}]}', newJson: '{"items":[{"id":1,"status":"paid"},{"id":2,"status":"pending"}]}' },
])
const batchResult = ref<BatchCompareResponse | null>(null)
const running = ref(false)
const fileInput = ref<HTMLInputElement>()
const expandedId = ref('task-1')

const totalDiffs = computed(() => batchResult.value?.items.reduce((sum, item) => sum + (item.result?.summary.total ?? 0), 0) ?? 0)
const equalRate = computed(() => batchResult.value ? Math.round((batchResult.value.equal / Math.max(batchResult.value.total, 1)) * 100) : 0)

function addItem() {
  const id = `task-${Date.now()}`
  items.value.push({ id, name: '未命名任务', oldJson: '{}', newJson: '{}' })
  expandedId.value = id
}

function removeItem(id: string) {
  if (items.value.length <= 1) { ElMessage.info('至少保留一条任务'); return }
  items.value = items.value.filter((item) => item.id !== id)
}

function resetItems() {
  items.value = []
  addItem()
  batchResult.value = null
}

async function runBatch() {
  if (!items.value.length) { ElMessage.warning('请先添加任务'); return }
  running.value = true
  try {
    batchResult.value = await api.compareBatch({ items: items.value, options: defaultOptions() })
    ElMessage.success(`批量比较完成，${batchResult.value.equal} 条一致，${batchResult.value.different} 条存在差异`)
  } catch (error) { ElMessage.error(apiErrorMessage(error)) } finally { running.value = false }
}

function parseCsvLine(line: string) {
  const values: string[] = []
  let current = ''; let quoted = false
  for (let index = 0; index < line.length; index += 1) {
    const char = line[index]
    if (char === '"' && line[index + 1] === '"') { current += '"'; index += 1 } else if (char === '"') quoted = !quoted
    else if (char === ',' && !quoted) { values.push(current); current = '' } else current += char
  }
  values.push(current)
  return values
}

function importText(text: string, fileName: string) {
  try {
    if (fileName.toLowerCase().endsWith('.csv')) {
      const lines = text.split(/\r?\n/).filter(Boolean)
      const header = parseCsvLine(lines.shift() || '').map((value) => value.trim().toLowerCase())
      const nameIndex = header.indexOf('name'); const oldIndex = header.findIndex((value) => ['oldjson', 'old', 'baseline'].includes(value)); const newIndex = header.findIndex((value) => ['newjson', 'new', 'target'].includes(value))
      items.value = lines.map((line, index) => { const values = parseCsvLine(line); return { id: `import-${index}`, name: values[nameIndex] || `导入任务 ${index + 1}`, oldJson: values[oldIndex] || '{}', newJson: values[newIndex] || '{}' } })
    } else {
      const parsed = JSON.parse(text)
      const source = Array.isArray(parsed) ? parsed : parsed.items
      if (!Array.isArray(source)) throw new Error('JSON 文件应为数组或包含 items 数组')
      items.value = source.map((item: Record<string, unknown>, index: number) => ({ id: String(item.id || `import-${index}`), name: String(item.name || `导入任务 ${index + 1}`), oldJson: typeof item.oldJson === 'string' ? item.oldJson : JSON.stringify(item.oldJson || {}, null, 2), newJson: typeof item.newJson === 'string' ? item.newJson : JSON.stringify(item.newJson || {}, null, 2) }))
    }
    expandedId.value = items.value[0]?.id || ''
    batchResult.value = null
    ElMessage.success(`已导入 ${items.value.length} 条任务`)
  } catch (error) { ElMessage.error(error instanceof Error ? error.message : '导入文件格式无效') }
}

function openFile() { fileInput.value?.click() }
function onFileChange(event: Event) { const file = (event.target as HTMLInputElement).files?.[0]; if (!file) return; const reader = new FileReader(); reader.onload = () => importText(String(reader.result || ''), file.name); reader.readAsText(file); (event.target as HTMLInputElement).value = '' }

function exportBatch() {
  if (!batchResult.value) { ElMessage.info('请先执行批量比较'); return }
  const rows = ['name,isEqual,total,added,removed,changed,error', ...batchResult.value.items.map((item) => [item.name, item.isEqual, item.result?.summary.total ?? '', item.result?.summary.added ?? '', item.result?.summary.removed ?? '', item.result?.summary.changed ?? '', item.error || ''].map((value) => `"${String(value).replace(/"/g, '""')}"`).join(','))]
  const blob = new Blob([`\ufeff${rows.join('\n')}`], { type: 'text/csv;charset=utf-8' }); const url = URL.createObjectURL(blob); const anchor = document.createElement('a'); anchor.href = url; anchor.download = 'batch-compare-report.csv'; anchor.click(); URL.revokeObjectURL(url)
}
</script>

<template>
  <div class="view batch-view">
    <div class="view-heading"><div><p class="eyebrow">AUTOMATION QUEUE / 03</p><h1>批量比较</h1><p>导入接口列表，一次执行多组响应校验并汇总结果。</p></div><div class="heading-actions"><input ref="fileInput" class="hidden-file" type="file" accept=".json,.csv" @change="onFileChange" /><el-button plain @click="openFile"><Upload :size="15" />导入列表</el-button><el-button plain @click="resetItems"><RotateCcw :size="15" />清空任务</el-button><el-button type="primary" :loading="running" @click="runBatch"><Play :size="15" fill="currentColor" />执行全部</el-button></div></div>
    <div v-if="batchResult" class="metric-strip batch-metrics"><div class="metric"><span class="metric-label">任务总数</span><strong class="metric-value">{{ batchResult.total }}</strong></div><div class="metric"><span class="metric-label">通过</span><strong class="metric-value teal">{{ batchResult.equal }}</strong></div><div class="metric"><span class="metric-label">存在差异</span><strong class="metric-value red">{{ batchResult.different }}</strong></div><div class="metric"><span class="metric-label">一致率</span><strong class="metric-value blue">{{ equalRate }}%</strong><small class="metric-note">共 {{ totalDiffs }} 处字段差异</small></div></div>
    <section class="batch-panel panel"><div class="panel-header"><div class="panel-title"><FileInput :size="16" class="title-icon" />任务列表 <span class="subtle">{{ items.length }} 条待执行</span></div><div class="batch-header-actions"><el-button text size="small" @click="exportBatch"><Download :size="13" />导出结果</el-button><el-button type="primary" plain size="small" @click="addItem"><Plus :size="13" />添加任务</el-button></div></div><div class="batch-list"><div v-for="(item, index) in items" :key="item.id" class="batch-item" :class="{ expanded: expandedId === item.id }"><div class="batch-row"><button class="expand-button" type="button" @click="expandedId = expandedId === item.id ? '' : item.id"><ChevronDown :size="15" /></button><span class="task-index">{{ String(index + 1).padStart(2, '0') }}</span><div class="task-main"><el-input v-model="item.name" size="small" /><span class="task-meta">{{ item.oldJson.length + item.newJson.length }} chars · JSON payload</span></div><span v-if="!batchResult" class="task-status pending">待执行</span><span v-else-if="batchResult.items.find((entry) => entry.id === item.id)?.error" class="task-status failed">失败</span><span v-else-if="batchResult.items.find((entry) => entry.id === item.id)?.isEqual" class="task-status passed"><CheckCircle2 :size="13" />通过</span><span v-else class="task-status changed">有差异</span><button class="mini-icon danger" type="button" title="删除任务" @click="removeItem(item.id)"><Trash2 :size="14" /></button></div><div v-if="expandedId === item.id" class="batch-detail"><div><label>基准响应</label><el-input v-model="item.oldJson" type="textarea" :rows="5" spellcheck="false" /></div><div><label>目标响应</label><el-input v-model="item.newJson" type="textarea" :rows="5" spellcheck="false" /></div></div></div><div v-if="!items.length" class="batch-empty"><FileJson2 :size="23" /><strong>暂无批量任务</strong><span>导入 JSON/CSV，或添加一条任务开始</span><el-button type="primary" plain size="small" @click="addItem"><Plus :size="13" />添加任务</el-button></div></div><div class="batch-foot"><span><CheckCircle2 :size="14" />支持 JSON 数组与 CSV（name / oldJson / newJson）</span><button type="button" @click="openFile"><Upload :size="13" />导入文件</button></div></section>
  </div>
</template>

<style scoped>
.batch-view { max-width: 1240px; margin: 0 auto; }.hidden-file { display: none; }.batch-metrics { margin-bottom: 22px; border: 1px solid var(--line); border-radius: var(--radius); overflow: hidden; box-shadow: var(--shadow); }.metric-note { color: var(--muted); font-size: 10px; }.batch-panel { overflow: hidden; }.batch-header-actions { display: flex; gap: 7px; }.batch-list { padding: 7px 0; }.batch-item { border-bottom: 1px solid var(--line-soft); }.batch-item:last-child { border-bottom: 0; }.batch-row { min-height: 61px; display: flex; align-items: center; gap: 11px; padding: 7px 17px; }.expand-button { width: 25px; height: 25px; display: grid; place-items: center; border: 0; color: var(--muted); background: transparent; transition: transform .15s ease; }.batch-item.expanded .expand-button { transform: rotate(180deg); color: var(--teal-dark); }.task-index { color: var(--muted); font: 11px 'DM Mono', monospace; width: 24px; }.task-main { flex: 1; min-width: 0; }.task-main :deep(.el-input__wrapper) { box-shadow: none !important; padding-inline: 0; }.task-main :deep(.el-input__inner) { font-size: 12px; font-weight: 700; color: var(--ink); }.task-meta { display: block; margin-top: 3px; color: var(--muted); font: 10px 'DM Mono', monospace; }.task-status { min-width: 66px; display: inline-flex; justify-content: flex-start; align-items: center; gap: 4px; font-size: 10px; font-weight: 700; }.task-status.pending { color: var(--muted); }.task-status.passed { color: var(--teal-dark); }.task-status.changed { color: var(--amber); }.task-status.failed { color: var(--red); }.mini-icon { width: 27px; height: 27px; display: grid; place-items: center; padding: 0; border: 0; background: transparent; color: var(--muted); }.mini-icon:hover { color: var(--teal); }.mini-icon.danger:hover { color: var(--red); }.batch-detail { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; padding: 0 58px 17px 78px; }.batch-detail label { display: block; margin-bottom: 6px; color: var(--muted); font-size: 10px; }.batch-detail :deep(.el-textarea__inner) { font: 11px/1.55 'DM Mono', monospace; min-height: 105px !important; }.batch-empty { min-height: 190px; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 7px; color: var(--teal-dark); }.batch-empty strong { color: var(--ink); font-size: 13px; }.batch-empty span { color: var(--muted); font-size: 11px; margin-bottom: 7px; }.batch-foot { min-height: 49px; display: flex; justify-content: space-between; align-items: center; padding: 0 17px; border-top: 1px solid var(--line); color: var(--muted); font-size: 10px; }.batch-foot > span, .batch-foot button { display: inline-flex; align-items: center; gap: 6px; }.batch-foot > span svg { color: var(--teal); }.batch-foot button { border: 0; background: transparent; color: var(--teal-dark); font-size: 10px; font-weight: 700; }
@media (max-width: 700px) { .batch-header-actions { flex-wrap: wrap; }.batch-row { gap: 6px; padding-inline: 10px; }.task-index { display: none; }.task-status { min-width: 50px; }.batch-detail { grid-template-columns: 1fr; padding: 0 15px 15px 42px; }.batch-foot { align-items: flex-start; flex-direction: column; gap: 8px; padding-block: 11px; } }
</style>
