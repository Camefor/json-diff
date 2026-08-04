<script setup lang="ts">
import { computed, ref } from 'vue'
import { Copy, Filter, Search } from 'lucide-vue-next'
import { ElMessage } from 'element-plus'
import type { DiffKind, JsonDifference } from '../types'

const props = withDefaults(defineProps<{
  differences: JsonDifference[]
  compact?: boolean
}>(), { compact: false })

const keyword = ref('')
const kindFilter = ref<'all' | DiffKind>('all')

const visibleDifferences = computed(() => props.differences.filter((item) => {
  const matchesKind = kindFilter.value === 'all' || item.kind === kindFilter.value
  const query = keyword.value.trim().toLowerCase()
  return matchesKind && (!query || [item.path, item.oldValue, item.newValue, item.message].some((value) => value?.toLowerCase().includes(query)))
}))

function kindLabel(kind: DiffKind) {
  return ({ Added: '新增', Removed: '删除', Changed: '值变化', TypeChanged: '类型变化' })[kind]
}

function pretty(value: string | null) {
  if (value === null) return '∅'
  try { return JSON.stringify(JSON.parse(value), null, 2) } catch { return value }
}

function copyPathFallback(path: string) {
  const textarea = document.createElement('textarea')
  textarea.value = path
  textarea.setAttribute('readonly', '')
  textarea.style.position = 'fixed'
  textarea.style.opacity = '0'
  document.body.appendChild(textarea)
  textarea.select()

  try {
    return document.execCommand('copy')
  } finally {
    textarea.remove()
  }
}

async function copyPath(path: string) {
  let copied = false
  if (navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(path)
      copied = true
    } catch {
      // Clipboard API 可能因非 HTTPS 或浏览器权限被拒绝，继续尝试兼容复制。
    }
  }

  if (!copied) {
    try { copied = copyPathFallback(path) } catch { copied = false }
  }

  if (copied) ElMessage.success('路径已复制')
  else ElMessage.error('路径复制失败，请手动复制')
}
</script>

<template>
  <div class="diff-table-wrap">
    <div class="diff-toolbar">
      <div class="diff-view-switch"><span class="active"><Filter :size="13" />差异明细</span><span>共 {{ visibleDifferences.length }} 条</span></div>
      <div class="diff-filters">
        <el-select v-model="kindFilter" size="small" style="width: 118px" :teleported="false">
          <el-option label="全部类型" value="all" /><el-option label="新增" value="Added" /><el-option label="删除" value="Removed" /><el-option label="值变化" value="Changed" /><el-option label="类型变化" value="TypeChanged" />
        </el-select>
        <el-input v-model="keyword" size="small" placeholder="筛选路径或值" clearable style="width: 180px"><template #prefix><Search :size="13" /></template></el-input>
      </div>
    </div>
    <div class="diff-scroll" :class="{ compact }">
      <table class="diff-table">
        <thead><tr><th class="col-status">状态</th><th class="col-path">路径</th><th>基准响应</th><th>目标响应</th><th class="col-message">说明</th></tr></thead>
        <tbody>
          <tr v-for="item in visibleDifferences" :key="`${item.path}-${item.kind}`">
            <td><span class="kind-pill" :class="`kind-${item.kind.toLowerCase()}`">{{ kindLabel(item.kind) }}</span></td>
            <td><div class="path-cell"><code>{{ item.path }}</code><button class="mini-icon" type="button" title="复制路径" @click="copyPath(item.path)"><Copy :size="12" /></button></div></td>
            <td><pre class="value-cell" :class="{ empty: item.oldValue === null }">{{ pretty(item.oldValue) }}</pre><small>{{ item.oldType }}</small></td>
            <td><pre class="value-cell" :class="{ empty: item.newValue === null }">{{ pretty(item.newValue) }}</pre><small>{{ item.newType }}</small></td>
            <td class="message-cell">{{ item.message }}</td>
          </tr>
          <tr v-if="visibleDifferences.length === 0"><td colspan="5" class="empty-table">没有符合条件的差异</td></tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.diff-table-wrap { background: var(--surface); }
.diff-toolbar { min-height: 50px; padding: 0 16px; display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--line-soft); gap: 10px; }
.diff-view-switch { display: flex; gap: 16px; align-items: center; color: var(--muted); font-size: 11px; }.diff-view-switch span { display: inline-flex; align-items: center; gap: 6px; }.diff-view-switch .active { color: var(--ink); font-weight: 700; }
.diff-filters { display: flex; gap: 8px; align-items: center; }
.diff-scroll { overflow: auto; max-height: 465px; }.diff-scroll.compact { max-height: 330px; }
.diff-table { border-collapse: collapse; width: 100%; table-layout: fixed; font-size: 11px; }.diff-table th { position: sticky; top: 0; z-index: 1; background: var(--surface-alt); color: var(--muted); text-align: left; font-weight: 700; padding: 10px 12px; border-bottom: 1px solid var(--line); }.diff-table td { padding: 11px 12px; border-bottom: 1px solid var(--line-soft); vertical-align: top; color: var(--ink-soft); }.diff-table tr:hover td { background: var(--surface-alt); }.col-status { width: 86px; }.col-path { width: 23%; }.col-message { width: 112px; }
.kind-pill { display: inline-block; padding: 3px 6px; border-radius: 4px; font-size: 10px; font-weight: 700; }.kind-added { color: var(--teal-dark); background: var(--mint); }.kind-removed { color: var(--red); background: var(--red-soft); }.kind-changed { color: var(--amber); background: var(--amber-soft); }.kind-typechanged { color: var(--blue); background: var(--blue-soft); }
.path-cell { display: flex; align-items: flex-start; gap: 5px; }.path-cell code { color: var(--ink); font: 11px 'DM Mono', monospace; word-break: break-all; }.mini-icon { border: 0; background: transparent; padding: 0; color: var(--muted); }.mini-icon:hover { color: var(--teal); }
.value-cell { margin: 0 0 5px; max-height: 82px; overflow: auto; white-space: pre-wrap; word-break: break-word; color: var(--ink); font: 11px/1.55 'DM Mono', monospace; }.value-cell.empty { color: var(--muted); }.diff-table small { color: var(--muted); font: 10px 'DM Mono', monospace; }.message-cell { line-height: 1.6; }.empty-table { text-align: center !important; color: var(--muted) !important; padding: 42px !important; }
@media (max-width: 700px) { .diff-toolbar { align-items: flex-start; flex-direction: column; padding-block: 11px; }.diff-filters { width: 100%; }.diff-filters .el-input { flex: 1; }.col-message { width: 90px; }.col-path { width: 25%; } }
</style>

