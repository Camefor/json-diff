<script setup lang="ts">
import { ref, watch } from 'vue'
import { Plus, Save, Trash2, X } from 'lucide-vue-next'
import type { CompareOptions, FieldMapping } from '../types'
import { cloneOptions, defaultOptions } from '../types'

const props = defineProps<{ visible: boolean; modelValue: CompareOptions }>()
const emit = defineEmits<{ 'update:visible': [value: boolean]; 'update:modelValue': [value: CompareOptions] }>()
const draft = ref<CompareOptions>(cloneOptions(props.modelValue))
const activeTab = ref('rules')

watch(() => props.visible, (visible) => { if (visible) draft.value = cloneOptions(props.modelValue) })
watch(() => props.modelValue, (value) => { if (!props.visible) draft.value = cloneOptions(value) })

function close() { emit('update:visible', false) }
function apply() { emit('update:modelValue', cloneOptions(draft.value)); close() }
function reset() { draft.value = defaultOptions() }
function addMapping() { draft.value.mappings.push({ from: '', to: '' }) }
function removeMapping(index: number) { draft.value.mappings.splice(index, 1) }
function addPath(target: 'ignorePaths' | 'whitelistPaths') { draft.value[target].push('$.data.example') }
function removePath(target: 'ignorePaths' | 'whitelistPaths', index: number) { draft.value[target].splice(index, 1) }
</script>

<template>
  <el-drawer :model-value="visible" direction="rtl" size="480px" :with-header="false" @close="close">
    <div class="options-drawer">
      <header class="drawer-head"><div><p class="eyebrow">COMPARE RULES</p><h2>高级比较规则</h2></div><button class="icon-button" type="button" @click="close"><X :size="18" /></button></header>
      <div class="drawer-tabs"><button :class="{ active: activeTab === 'rules' }" type="button" @click="activeTab = 'rules'">基础规则</button><button :class="{ active: activeTab === 'paths' }" type="button" @click="activeTab = 'paths'">字段过滤</button><button :class="{ active: activeTab === 'mapping' }" type="button" @click="activeTab = 'mapping'">字段映射</button></div>

      <div v-if="activeTab === 'rules'" class="drawer-section">
        <div class="rule-group"><span class="section-caption">比较内容</span><label class="switch-line"><span>比较字段 Key</span><el-switch v-model="draft.compareKeys" /></label><label class="switch-line"><span>比较字段 Value</span><el-switch v-model="draft.compareValues" /></label><label class="switch-line"><span>比较字段 Type</span><el-switch v-model="draft.compareTypes" /></label><label class="switch-line"><span>区分大小写</span><el-switch v-model="draft.caseSensitive" /></label></div>
        <div class="rule-group"><span class="section-caption">Null 与数值</span><div class="field-line"><label>Null 策略</label><el-select v-model="draft.nullStrategy" size="small"><el-option label="严格比较" value="strict" /><el-option label="忽略 Null 差异" value="ignore" /><el-option label="缺失视为 Null" value="missing-as-null" /></el-select></div><div class="field-line"><label>数值容差</label><el-input-number v-model="draft.numericTolerance" :min="0" :step="0.0001" :precision="6" controls-position="right" size="small" /></div><div class="field-line"><label>浮点误差</label><el-input-number v-model="draft.floatEpsilon" :min="0" :step="0.000001" :precision="6" controls-position="right" size="small" /></div></div>
        <div class="rule-group"><span class="section-caption">数组处理</span><label class="switch-line"><span>忽略数组顺序</span><el-switch v-model="draft.ignoreArrayOrder" /></label><div class="field-line"><label>数组主键</label><el-input v-model="draft.arrayKey" size="small" placeholder="如 id / code" clearable /></div></div>
      </div>

      <div v-else-if="activeTab === 'paths'" class="drawer-section path-rules"><div class="path-rule-block"><div class="rule-block-head"><div><span class="section-caption">忽略字段</span><small>支持 JSONPath、* 通配符与 regex: 前缀</small></div><button class="mini-add" type="button" @click="addPath('ignorePaths')"><Plus :size="14" /></button></div><div v-for="(path, index) in draft.ignorePaths" :key="`ignore-${index}`" class="path-input"><el-input v-model="draft.ignorePaths[index]" size="small" placeholder="$.meta.requestId" /><button type="button" @click="removePath('ignorePaths', index)"><Trash2 :size="14" /></button></div><div v-if="!draft.ignorePaths.length" class="empty-rule">暂无忽略字段</div></div><div class="path-rule-block"><div class="rule-block-head"><div><span class="section-caption">字段白名单</span><small>启用后仅比较匹配路径</small></div><button class="mini-add" type="button" @click="addPath('whitelistPaths')"><Plus :size="14" /></button></div><div v-for="(path, index) in draft.whitelistPaths" :key="`white-${index}`" class="path-input"><el-input v-model="draft.whitelistPaths[index]" size="small" placeholder="$.payload.data" /><button type="button" @click="removePath('whitelistPaths', index)"><Trash2 :size="14" /></button></div><div v-if="!draft.whitelistPaths.length" class="empty-rule">未设置白名单，将比较全部字段</div></div></div>

      <div v-else class="drawer-section mapping-rules"><div class="rule-block-head"><div><span class="section-caption">字段映射</span><small>将基准响应字段映射到目标响应字段</small></div><button class="mini-add" type="button" @click="addMapping"><Plus :size="14" /></button></div><div v-for="(mapping, index) in draft.mappings" :key="index" class="mapping-row"><el-input v-model="mapping.from" size="small" placeholder="price" /><span>→</span><el-input v-model="mapping.to" size="small" placeholder="lastPrice" /><button type="button" @click="removeMapping(index)"><Trash2 :size="14" /></button></div><div v-if="!draft.mappings.length" class="empty-rule">暂无字段映射</div></div>

      <footer class="drawer-foot"><el-button text @click="reset">恢复默认</el-button><el-button type="primary" @click="apply"><Save :size="15" />应用规则</el-button></footer>
    </div>
  </el-drawer>
</template>

<style scoped>
.options-drawer { height: 100%; display: flex; flex-direction: column; }.drawer-head { display: flex; justify-content: space-between; align-items: flex-start; padding: 24px 24px 17px; border-bottom: 1px solid var(--line); }.drawer-head h2 { margin: 0; font-size: 20px; }.drawer-tabs { display: flex; gap: 3px; padding: 13px 24px 0; border-bottom: 1px solid var(--line-soft); }.drawer-tabs button { border: 0; background: transparent; color: var(--muted); padding: 9px 11px 12px; font-size: 11px; border-bottom: 2px solid transparent; }.drawer-tabs button.active { color: var(--teal-dark); border-bottom-color: var(--teal); font-weight: 700; }.drawer-section { flex: 1; overflow: auto; padding: 21px 24px; }.rule-group { border-bottom: 1px solid var(--line-soft); padding-bottom: 18px; margin-bottom: 19px; }.section-caption { display: block; color: var(--ink); font-size: 12px; font-weight: 800; margin-bottom: 12px; }.switch-line, .field-line { min-height: 36px; display: flex; align-items: center; justify-content: space-between; gap: 14px; color: var(--ink-soft); font-size: 12px; }.field-line { margin-top: 7px; }.field-line label { min-width: 72px; }.field-line .el-select, .field-line .el-input, .field-line .el-input-number { flex: 1; max-width: 220px; }.rule-block-head { display: flex; justify-content: space-between; gap: 10px; align-items: flex-start; }.rule-block-head small { display: block; color: var(--muted); font-size: 10px; margin-top: -6px; margin-bottom: 13px; }.mini-add { width: 25px; height: 25px; display: grid; place-items: center; color: var(--teal-dark); background: var(--mint); border: 0; border-radius: 4px; }.path-rule-block { margin-bottom: 24px; }.path-input, .mapping-row { display: flex; align-items: center; gap: 7px; margin-bottom: 8px; }.path-input .el-input, .mapping-row .el-input { flex: 1; }.path-input button, .mapping-row button { width: 25px; height: 25px; padding: 0; display: grid; place-items: center; border: 0; color: var(--muted); background: transparent; }.path-input button:hover, .mapping-row button:hover { color: var(--red); }.mapping-row > span { color: var(--teal); }.empty-rule { padding: 15px 0; color: var(--muted); font-size: 11px; }.drawer-foot { display: flex; justify-content: flex-end; gap: 8px; padding: 15px 24px; border-top: 1px solid var(--line); }
</style>

