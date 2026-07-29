<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { CalendarClock, ChevronRight, Download, Eye, FileDiff, RefreshCw, Repeat, Search, Trash2 } from 'lucide-vue-next'
import { ElMessage, ElMessageBox } from 'element-plus'
import DiffSummary from '../components/DiffSummary.vue'
import DiffTable from '../components/DiffTable.vue'
import { api, apiErrorMessage } from '../api'
import { useAppStore } from '../stores/app'
import type { HistoryQueryResponse, HistoryRecord, HistorySummary, InterfaceRequest } from '../types'

const router = useRouter()
const appStore = useAppStore()
const data = ref<HistoryQueryResponse>({ total: 0, page: 1, pageSize: 15, items: [] })
const keyword = ref('')
const loading = ref(false)
const detail = ref<HistoryRecord | null>(null)
const detailVisible = ref(false)

async function load() {
  loading.value = true
  try { data.value = await api.history(data.value.page, data.value.pageSize, keyword.value) } catch (error) { ElMessage.error(apiErrorMessage(error)) } finally { loading.value = false }
}

async function openDetail(item: HistorySummary) {
  try { detail.value = await api.historyDetail(item.id); detailVisible.value = true } catch (error) { ElMessage.error(apiErrorMessage(error)) }
}

async function remove(item: HistorySummary) {
  try {
    await ElMessageBox.confirm(`确定删除“${item.name}”吗？`, '删除历史记录', { type: 'warning', confirmButtonText: '删除', cancelButtonText: '取消' })
    await api.deleteHistory(item.id)
    ElMessage.success('历史记录已删除')
    if (detail.value?.id === item.id) { detailVisible.value = false; detail.value = null }
    await load()
  } catch (error) { if (error !== 'cancel' && error !== 'close') ElMessage.error(apiErrorMessage(error)) }
}

// 把历史记录中的接口请求快照写回 store，跳转后由 InterfaceView 消费回填
// 列表项触发时 target 只有 id，按需拉详情；详情抽屉触发时已带 oldRequest/newRequest，直接复用
async function restoreInterface(target: { id: string; oldRequest?: InterfaceRequest | null; newRequest?: InterfaceRequest | null }) {
  let oldRequest = target.oldRequest
  let newRequest = target.newRequest
  if (!oldRequest || !newRequest) {
    try {
      const record = await api.historyDetail(target.id)
      oldRequest = record.oldRequest
      newRequest = record.newRequest
    } catch (error) { ElMessage.error(apiErrorMessage(error)); return }
  }
  if (!oldRequest || !newRequest) {
    ElMessage.warning('该历史记录缺少接口请求快照，无法回填。')
    return
  }
  appStore.setInterfaceRestore({ oldRequest, newRequest })
  detailVisible.value = false
  router.push({ name: 'interface' })
}

function formatDate(value: string) { return new Intl.DateTimeFormat('zh-CN', { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' }).format(new Date(value)) }
function sourceLabel(source: HistorySummary['sourceType']) { return ({ json: 'JSON', interface: '接口', batch: '批量' })[source] }
function report(id: string, format: string) { window.open(api.reportUrl(id, format), '_blank') }
function pageChange(page: number) { data.value.page = page; load() }

onMounted(load)
</script>

<template>
  <div class="view history-view">
    <div class="view-heading"><div><p class="eyebrow">AUDIT TRAIL / 04</p><h1>历史记录</h1><p>集中查看每次比较的结果、规则与差异快照。</p></div><div class="heading-actions"><el-button plain :loading="loading" @click="load"><RefreshCw :size="15" />刷新记录</el-button></div></div>
    <section class="history-panel panel"><div class="history-toolbar"><div class="history-count"><FileDiff :size="16" class="title-icon" /><strong>比较历史</strong><span>{{ data.total }} 条记录</span></div><div class="history-search"><el-input v-model="keyword" size="small" clearable placeholder="搜索记录名称，回车查询" @keyup.enter="load"><template #prefix><Search :size="13" /></template></el-input><el-button type="primary" plain size="small" @click="load">查询</el-button></div></div><div v-loading="loading" class="history-table-scroll"><table class="history-table"><thead><tr><th>记录</th><th>来源</th><th>目标 URL</th><th>状态</th><th>差异概览</th><th>耗时</th><th>时间</th><th></th></tr></thead><tbody><tr v-for="item in data.items" :key="item.id" @dblclick="openDetail(item)"><td><div class="history-name"><span class="history-icon"><FileDiff :size="14" /></span><div><strong>{{ item.name }}</strong><small class="mono">{{ item.id.slice(0, 12) }}</small></div></div></td><td><span class="source-tag" :class="`source-${item.sourceType}`">{{ sourceLabel(item.sourceType) }}</span></td><td class="url-cell" :title="item.sourceType === 'interface' ? (item.newUrl || '') : ''">{{ item.sourceType === 'interface' && item.newUrl ? item.newUrl : '—' }}</td><td><span class="history-status" :class="item.isEqual ? 'equal' : 'different'"><i></i>{{ item.isEqual ? '一致' : '有差异' }}</span></td><td><div class="diff-counts"><span class="add">+{{ item.summary.added }}</span><span class="remove">-{{ item.summary.removed }}</span><span class="change">~{{ item.summary.changed + item.summary.typeChanged }}</span></div></td><td class="mono">{{ item.durationMs }} ms</td><td><span class="date-cell"><CalendarClock :size="13" />{{ formatDate(item.createdAt) }}</span></td><td><div class="row-actions"><el-tooltip v-if="item.sourceType === 'interface'" content="在接口比较中打开" placement="top"><button type="button" class="restore-action" @click="restoreInterface(item)"><Repeat :size="15" /></button></el-tooltip><el-tooltip content="查看详情" placement="top"><button type="button" @click="openDetail(item)"><Eye :size="15" /></button></el-tooltip><el-tooltip content="删除记录" placement="top"><button type="button" class="delete-action" @click="remove(item)"><Trash2 :size="15" /></button></el-tooltip></div></td></tr><tr v-if="!data.items.length && !loading"><td colspan="8" class="history-empty">暂无历史记录，完成一次比较后会自动出现在这里</td></tr></tbody></table></div><div class="history-pagination"><span>显示 {{ data.items.length ? (data.page - 1) * data.pageSize + 1 : 0 }} - {{ Math.min(data.page * data.pageSize, data.total) }} / {{ data.total }}</span><el-pagination v-model:current-page="data.page" small background layout="prev, pager, next" :page-size="data.pageSize" :total="data.total" @current-change="pageChange" /></div></section>

    <el-drawer v-model="detailVisible" size="min(860px, 92vw)" :with-header="false"><div v-if="detail" class="history-detail"><div class="detail-head"><div><p class="eyebrow">HISTORY DETAIL</p><h2>{{ detail.name }}</h2><span>{{ formatDate(detail.createdAt) }} · {{ sourceLabel(detail.sourceType) }} · {{ detail.id }}</span></div><div class="detail-actions"><el-button v-if="detail.sourceType === 'interface'" plain size="small" @click="restoreInterface(detail)"><Repeat :size="14" />在接口比较中打开</el-button><el-dropdown trigger="click"><el-button plain size="small"><Download :size="14" />导出<ChevronRight :size="13" /></el-button><template #dropdown><el-dropdown-menu><el-dropdown-item @click="report(detail.id, 'html')">HTML</el-dropdown-item><el-dropdown-item @click="report(detail.id, 'markdown')">Markdown</el-dropdown-item><el-dropdown-item @click="report(detail.id, 'csv')">CSV</el-dropdown-item><el-dropdown-item @click="report(detail.id, 'excel')">Excel</el-dropdown-item><el-dropdown-item @click="report(detail.id, 'pdf')">PDF</el-dropdown-item></el-dropdown-menu></template></el-dropdown></div></div><DiffSummary :result="detail.result" /><div v-if="detail.sourceType === 'interface'" class="detail-url-row"><div><span>基准接口</span><code>{{ detail.oldRequest?.url || detail.oldUrl || '—' }}</code></div><div><span>目标接口</span><code>{{ detail.newRequest?.url || detail.newUrl || '—' }}</code></div></div><div class="detail-json-grid"><div><span>基准 JSON</span><pre>{{ detail.oldJson }}</pre></div><div><span>目标 JSON</span><pre>{{ detail.newJson }}</pre></div></div><DiffTable :differences="detail.result.differences" /></div></el-drawer>
  </div>
</template>

<style scoped>
.history-view { max-width: 1340px; margin: 0 auto; }.history-panel { overflow: hidden; }.history-toolbar, .history-count, .history-search, .date-cell, .row-actions, .history-pagination, .diff-counts, .history-name { display: flex; align-items: center; }.history-toolbar { min-height: 65px; justify-content: space-between; padding: 0 18px; border-bottom: 1px solid var(--line); }.history-count { gap: 8px; }.history-count strong { font-size: 13px; }.history-count > span { color: var(--muted); font-size: 11px; }.history-search { gap: 7px; }.history-search .el-input { width: 235px; }.history-table-scroll { overflow: auto; min-height: 300px; }.history-table { width: 100%; min-width: 800px; border-collapse: collapse; font-size: 11px; }.history-table th { padding: 11px 14px; background: var(--surface-alt); color: var(--muted); text-align: left; font-weight: 700; }.history-table td { padding: 13px 14px; border-bottom: 1px solid var(--line-soft); color: var(--ink-soft); }.history-table tbody tr:hover td { background: var(--surface-alt); }.history-name { gap: 9px; }.history-icon { width: 29px; height: 29px; display: grid; place-items: center; color: var(--teal-dark); background: var(--mint); border-radius: 5px; }.history-name strong, .history-name small { display: block; }.history-name strong { color: var(--ink); font-size: 12px; }.history-name small { margin-top: 4px; color: var(--muted); font-size: 9px; }.source-tag { display: inline-block; padding: 4px 7px; border-radius: 4px; font-size: 10px; font-weight: 700; }.source-json { color: var(--teal-dark); background: var(--mint); }.source-interface { color: var(--blue); background: var(--blue-soft); }.source-batch { color: var(--amber); background: var(--amber-soft); }.history-status { display: inline-flex; align-items: center; gap: 5px; font-weight: 700; }.history-status i { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }.history-status.equal { color: var(--teal-dark); }.history-status.different { color: var(--red); }.diff-counts { gap: 8px; font: 11px 'DM Mono', monospace; }.diff-counts .add { color: var(--teal-dark); }.diff-counts .remove { color: var(--red); }.diff-counts .change { color: var(--amber); }.date-cell { gap: 5px; white-space: nowrap; }.row-actions { gap: 3px; justify-content: flex-end; }.row-actions button { width: 28px; height: 28px; display: grid; place-items: center; padding: 0; border: 0; border-radius: 4px; background: transparent; color: var(--muted); }.row-actions button:hover { color: var(--teal-dark); background: var(--surface-alt); }.row-actions .delete-action:hover { color: var(--red); }.history-empty { text-align: center; color: var(--muted) !important; padding: 58px !important; }.history-pagination { min-height: 57px; justify-content: space-between; padding: 0 18px; color: var(--muted); font-size: 10px; border-top: 1px solid var(--line); }.history-detail { min-height: 100%; padding: 28px 30px 40px; }.detail-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 15px; margin-bottom: 20px; }.detail-head h2 { margin: 0; font-size: 20px; }.detail-head > div > span { display: block; margin-top: 7px; color: var(--muted); font: 10px 'DM Mono', monospace; }.detail-json-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; padding: 16px 18px; border-bottom: 1px solid var(--line); }.detail-json-grid > div > span { display: block; margin-bottom: 7px; color: var(--muted); font-size: 10px; }.detail-json-grid pre { margin: 0; max-height: 160px; overflow: auto; padding: 11px; border: 1px solid var(--line); border-radius: 5px; background: var(--surface-alt); color: var(--ink-soft); font: 10px/1.5 'DM Mono', monospace; white-space: pre-wrap; word-break: break-word; }.url-cell { max-width: 280px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: var(--muted); font: 10px/1.4 'DM Mono', monospace; }.row-actions .restore-action:hover { color: var(--blue); }.detail-url-row { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; padding: 14px 18px; border-bottom: 1px solid var(--line); }.detail-url-row > div { min-width: 0; display: flex; flex-direction: column; gap: 6px; }.detail-url-row span { color: var(--muted); font-size: 10px; }.detail-url-row code { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; padding: 8px 10px; border: 1px solid var(--line); border-radius: 5px; background: var(--surface-alt); color: var(--ink-soft); font: 10px/1.4 'DM Mono', monospace; }
@media (max-width: 680px) { .history-toolbar { align-items: flex-start; flex-direction: column; gap: 10px; padding-block: 13px; }.history-search { width: 100%; }.history-search .el-input { flex: 1; width: auto; }.history-pagination { align-items: flex-start; flex-direction: column; gap: 8px; padding-block: 11px; }.history-detail { padding: 22px 15px; }.detail-json-grid { grid-template-columns: 1fr; }.detail-url-row { grid-template-columns: 1fr; }.detail-head { flex-direction: column; } }
</style>

