<script setup lang="ts">
import { Check, CircleAlert, Clock3, FileWarning, Plus, Minus, Type } from 'lucide-vue-next'
import type { CompareJsonResponse } from '../types'

defineProps<{ result: CompareJsonResponse }>()
</script>

<template>
  <div class="result-overview">
    <div class="result-state" :class="result.isEqual ? 'equal' : 'different'">
      <div class="state-icon"><Check v-if="result.isEqual" :size="20" /><CircleAlert v-else :size="20" /></div>
      <div><strong>{{ result.isEqual ? '响应一致' : '检测到差异' }}</strong><span>{{ result.isEqual ? '两份 JSON 通过当前规则比较' : `共发现 ${result.summary.total} 处字段差异` }}</span></div>
    </div>
    <div class="result-metrics">
      <div><span>新增</span><strong class="metric-new"><Plus :size="12" />{{ result.summary.added }}</strong></div>
      <div><span>删除</span><strong class="metric-remove"><Minus :size="12" />{{ result.summary.removed }}</strong></div>
      <div><span>值变化</span><strong class="metric-change"><FileWarning :size="12" />{{ result.summary.changed }}</strong></div>
      <div><span>类型变化</span><strong class="metric-type"><Type :size="12" />{{ result.summary.typeChanged }}</strong></div>
      <div><span>比较耗时</span><strong class="metric-time"><Clock3 :size="12" />{{ result.durationMs }} ms</strong></div>
    </div>
  </div>
</template>

<style scoped>
.result-overview { display: flex; align-items: center; justify-content: space-between; gap: 18px; padding: 16px 18px; background: var(--surface); border-bottom: 1px solid var(--line); }
.result-state { min-width: 208px; display: flex; align-items: center; gap: 11px; }
.state-icon { width: 35px; height: 35px; display: grid; place-items: center; border-radius: 50%; }
.result-state.equal .state-icon { color: var(--teal-dark); background: var(--mint); }
.result-state.different .state-icon { color: var(--red); background: var(--red-soft); }
.result-state strong, .result-state span { display: block; }
.result-state strong { color: var(--ink); font-size: 13px; }
.result-state span { color: var(--muted); font-size: 11px; margin-top: 3px; }
.result-metrics { display: flex; align-items: center; justify-content: flex-end; gap: 23px; }
.result-metrics > div { display: flex; flex-direction: column; gap: 5px; }
.result-metrics span { color: var(--muted); font-size: 10px; }
.result-metrics strong { display: inline-flex; align-items: center; gap: 3px; font: 700 12px 'DM Mono', monospace; }
.metric-new { color: var(--teal-dark); }.metric-remove { color: var(--red); }.metric-change { color: var(--amber); }.metric-type { color: var(--blue); }.metric-time { color: var(--ink-soft); }
@media (max-width: 820px) { .result-overview { align-items: flex-start; flex-direction: column; } .result-metrics { width: 100%; justify-content: space-between; gap: 8px; } }
</style>

