<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Check, CopyPlus, Database, FileCog, Plus, Save, SlidersHorizontal, Trash2 } from 'lucide-vue-next'
import { ElMessage, ElMessageBox } from 'element-plus'
import { api, apiErrorMessage } from '../api'
import { useAppStore } from '../stores/app'
import { cloneOptions, defaultOptions } from '../types'
import type { CompareOptions } from '../types'

const store = useAppStore()
const profileName = ref('默认规则')
const description = ref('平台内置默认比较规则')
const options = ref<CompareOptions>(defaultOptions())
const activeTab = ref<'rules' | 'paths' | 'mapping'>('rules')
const saving = ref(false)

const selectedProfile = computed(() => store.profiles.find((profile) => profile.name === profileName.value))

function selectProfile(name: string) {
  const profile = store.profiles.find((item) => item.name === name)
  if (!profile) return
  profileName.value = profile.name
  description.value = profile.description
  options.value = cloneOptions(profile.options)
}

function newProfile() {
  profileName.value = `新规则 ${store.profiles.length + 1}`
  description.value = ''
  options.value = defaultOptions()
}

async function save() {
  if (!profileName.value.trim()) { ElMessage.warning('请输入规则名称'); return }
  saving.value = true
  try {
    const profile = await api.saveProfile({ name: profileName.value.trim(), description: description.value, options: options.value })
    const index = store.profiles.findIndex((item) => item.name === profile.name)
    if (index >= 0) store.profiles[index] = profile; else store.profiles.push(profile)
    store.activeProfile = profile.name
    profileName.value = profile.name
    ElMessage.success('比较规则已保存')
  } catch (error) { ElMessage.error(apiErrorMessage(error)) } finally { saving.value = false }
}

async function removeProfile() {
  if (!selectedProfile.value) return
  try {
    await ElMessageBox.confirm(`确定删除规则“${profileName.value}”吗？`, '删除规则', { type: 'warning', confirmButtonText: '删除', cancelButtonText: '取消' })
    await api.deleteProfile(profileName.value)
    store.profiles = store.profiles.filter((profile) => profile.name !== profileName.value)
    const next = store.profiles[0]
    if (next) selectProfile(next.name)
    ElMessage.success('规则已删除')
  } catch (error) { if (error !== 'cancel' && error !== 'close') ElMessage.error(apiErrorMessage(error)) }
}

function addPath(target: 'ignorePaths' | 'whitelistPaths') { options.value[target].push('$.data.example') }
function removePath(target: 'ignorePaths' | 'whitelistPaths', index: number) { options.value[target].splice(index, 1) }
function addMapping() { options.value.mappings.push({ from: '', to: '' }) }
function removeMapping(index: number) { options.value.mappings.splice(index, 1) }

onMounted(async () => {
  await store.initialize()
  if (store.profiles.length) selectProfile(store.activeProfile)
})
</script>

<template>
  <div class="view config-view">
    <div class="view-heading"><div><p class="eyebrow">RULE LIBRARY / 05</p><h1>配置中心</h1><p>保存可复用的比较规则，服务 JSON、接口和批量工作流。</p></div><div class="heading-actions"><el-button plain @click="newProfile"><CopyPlus :size="15" />新建规则</el-button><el-button type="primary" :loading="saving" @click="save"><Save :size="15" />保存规则</el-button></div></div>
    <div class="config-layout"><aside class="profile-panel panel"><div class="profile-panel-head"><div class="panel-title"><FileCog :size="16" class="title-icon" />规则列表</div><button class="mini-add" type="button" @click="newProfile"><Plus :size="15" /></button></div><div class="profile-list"><button v-for="profile in store.profiles" :key="profile.name" class="profile-item" :class="{ active: profile.name === profileName }" type="button" @click="selectProfile(profile.name)"><span class="profile-status"><Check v-if="profile.name === store.activeProfile" :size="12" /></span><span class="profile-item-copy"><strong>{{ profile.name }}</strong><small>{{ profile.description || '未填写说明' }}</small></span><span class="profile-time">{{ new Date(profile.updatedAt).toLocaleDateString('zh-CN', { month: '2-digit', day: '2-digit' }) }}</span></button><div v-if="!store.profiles.length" class="profile-empty">加载规则中...</div></div><div class="profile-panel-foot"><Database :size="14" /><span>本地存储 · {{ store.profiles.length }} 个规则</span></div></aside>
      <section class="rule-editor panel"><div class="rule-editor-head"><div><span class="section-caption">RULE PROFILE</span><div class="profile-name-row"><el-input v-model="profileName" size="large" /><el-tag v-if="selectedProfile" size="small" type="success" effect="plain">已保存</el-tag></div><el-input v-model="description" size="small" class="profile-description" placeholder="为这套规则添加说明" /></div><el-button v-if="selectedProfile && store.profiles.length > 1" text type="danger" @click="removeProfile"><Trash2 :size="14" />删除</el-button></div><div class="config-tabs"><button :class="{ active: activeTab === 'rules' }" type="button" @click="activeTab = 'rules'"><SlidersHorizontal :size="14" />基础规则</button><button :class="{ active: activeTab === 'paths' }" type="button" @click="activeTab = 'paths'">字段过滤<span>{{ options.ignorePaths.length + options.whitelistPaths.length }}</span></button><button :class="{ active: activeTab === 'mapping' }" type="button" @click="activeTab = 'mapping'">字段映射<span>{{ options.mappings.length }}</span></button></div>
        <div v-if="activeTab === 'rules'" class="rule-editor-content"><div class="config-section"><h3>比较内容</h3><p>决定差异引擎参与比较的字段层级。</p><div class="config-option-grid"><label><span><strong>字段 Key</strong><small>新增、删除字段</small></span><el-switch v-model="options.compareKeys" /></label><label><span><strong>字段 Value</strong><small>字符串、数字、布尔值</small></span><el-switch v-model="options.compareValues" /></label><label><span><strong>字段 Type</strong><small>类型变化检测</small></span><el-switch v-model="options.compareTypes" /></label><label><span><strong>区分大小写</strong><small>属性名匹配策略</small></span><el-switch v-model="options.caseSensitive" /></label></div></div><div class="config-section"><h3>Null 与数值</h3><p>处理动态响应中的空值和计算误差。</p><div class="config-field-grid"><div><label>Null 策略</label><el-select v-model="options.nullStrategy" size="small"><el-option label="严格比较" value="strict" /><el-option label="忽略 Null 差异" value="ignore" /><el-option label="缺失视为 Null" value="missing-as-null" /></el-select></div><div><label>数值容差</label><el-input-number v-model="options.numericTolerance" :min="0" :step="0.0001" :precision="6" controls-position="right" size="small" /></div><div><label>浮点误差</label><el-input-number v-model="options.floatEpsilon" :min="0" :step="0.000001" :precision="6" controls-position="right" size="small" /></div></div></div><div class="config-section"><h3>数组匹配</h3><p>数组可按主键匹配，避免元素顺序变化产生误报。</p><div class="array-config"><label><span>忽略数组顺序</span><el-switch v-model="options.ignoreArrayOrder" /></label><div><label>数组主键字段</label><el-input v-model="options.arrayKey" size="small" placeholder="如 id、code、uuid" /></div></div></div></div>
        <div v-else-if="activeTab === 'paths'" class="rule-editor-content paths-content"><div class="config-section"><div class="config-section-head"><div><h3>忽略字段</h3><p>支持 JSONPath、单段 * 通配符和 regex: 正则前缀。</p></div><button class="mini-add" type="button" @click="addPath('ignorePaths')"><Plus :size="14" /></button></div><div v-for="(path, index) in options.ignorePaths" :key="`ignore-${index}`" class="path-row"><el-input v-model="options.ignorePaths[index]" size="small" placeholder="$.meta.requestId" /><button type="button" @click="removePath('ignorePaths', index)"><Trash2 :size="14" /></button></div><div v-if="!options.ignorePaths.length" class="empty-inline">还没有配置忽略字段</div></div><div class="config-section"><div class="config-section-head"><div><h3>字段白名单</h3><p>启用后，只有命中的路径会参与比较。</p></div><button class="mini-add" type="button" @click="addPath('whitelistPaths')"><Plus :size="14" /></button></div><div v-for="(path, index) in options.whitelistPaths" :key="`white-${index}`" class="path-row"><el-input v-model="options.whitelistPaths[index]" size="small" placeholder="$.payload.data" /><button type="button" @click="removePath('whitelistPaths', index)"><Trash2 :size="14" /></button></div><div v-if="!options.whitelistPaths.length" class="empty-inline">未设置白名单，将比较全部字段</div></div></div>
        <div v-else class="rule-editor-content mapping-content"><div class="config-section"><div class="config-section-head"><div><h3>字段映射</h3><p>适用于新旧接口字段重命名的迁移场景。</p></div><button class="mini-add" type="button" @click="addMapping"><Plus :size="14" /></button></div><div class="mapping-header"><span>基准字段</span><span></span><span>目标字段</span><span></span></div><div v-for="(mapping, index) in options.mappings" :key="index" class="mapping-edit-row"><el-input v-model="mapping.from" size="small" placeholder="price" /><span>→</span><el-input v-model="mapping.to" size="small" placeholder="lastPrice" /><button type="button" @click="removeMapping(index)"><Trash2 :size="14" /></button></div><div v-if="!options.mappings.length" class="empty-inline">还没有配置字段映射</div></div></div>
        <div class="rule-editor-foot"><span><SlidersHorizontal :size="13" />规则会同步应用到 JSON、接口和批量比较</span><el-button type="primary" :loading="saving" @click="save"><Save :size="14" />保存规则</el-button></div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.config-view { max-width: 1320px; margin: 0 auto; }.config-layout { display: grid; grid-template-columns: 278px minmax(0, 1fr); gap: 18px; align-items: start; }.profile-panel { overflow: hidden; }.profile-panel-head { min-height: 58px; display: flex; align-items: center; justify-content: space-between; padding: 0 15px; border-bottom: 1px solid var(--line); }.mini-add { width: 26px; height: 26px; display: grid; place-items: center; border: 0; border-radius: 4px; background: var(--mint); color: var(--teal-dark); }.profile-list { padding: 7px; min-height: 240px; }.profile-item { width: 100%; display: flex; align-items: center; gap: 8px; min-height: 57px; padding: 8px; border: 1px solid transparent; background: transparent; border-radius: 6px; text-align: left; }.profile-item:hover { background: var(--surface-alt); }.profile-item.active { border-color: var(--line); background: var(--surface-alt); }.profile-status { width: 20px; height: 20px; display: grid; place-items: center; color: var(--teal-dark); background: var(--mint); border-radius: 4px; }.profile-item-copy { min-width: 0; flex: 1; }.profile-item-copy strong, .profile-item-copy small { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }.profile-item-copy strong { color: var(--ink); font-size: 11px; }.profile-item-copy small { color: var(--muted); font-size: 9px; margin-top: 3px; }.profile-time { color: var(--muted); font: 9px 'DM Mono', monospace; }.profile-empty { padding: 34px 10px; color: var(--muted); text-align: center; font-size: 11px; }.profile-panel-foot { min-height: 45px; display: flex; align-items: center; gap: 6px; padding: 0 15px; border-top: 1px solid var(--line); color: var(--muted); font-size: 10px; }.rule-editor { min-width: 0; overflow: hidden; }.rule-editor-head { min-height: 110px; display: flex; justify-content: space-between; align-items: flex-start; padding: 20px 23px 16px; border-bottom: 1px solid var(--line); }.section-caption { display: block; color: var(--teal-dark); font: 600 10px 'DM Mono', monospace; letter-spacing: 1px; margin-bottom: 7px; }.profile-name-row { display: flex; align-items: center; gap: 9px; }.profile-name-row .el-input { width: 260px; }.profile-name-row :deep(.el-input__inner) { color: var(--ink); font-size: 16px; font-weight: 800; }.profile-description { width: 330px; margin-top: 6px; }.config-tabs { display: flex; gap: 3px; padding: 0 23px; border-bottom: 1px solid var(--line); }.config-tabs button { display: inline-flex; align-items: center; gap: 7px; padding: 14px 12px 12px; border: 0; border-bottom: 2px solid transparent; background: transparent; color: var(--muted); font-size: 11px; }.config-tabs button.active { border-bottom-color: var(--teal); color: var(--teal-dark); font-weight: 700; }.config-tabs button span { padding: 2px 5px; border-radius: 7px; background: var(--surface-alt); font: 10px 'DM Mono', monospace; }.rule-editor-content { min-height: 410px; padding: 22px 23px 34px; }.config-section { padding-bottom: 22px; margin-bottom: 22px; border-bottom: 1px solid var(--line-soft); }.config-section:last-child { margin-bottom: 0; border-bottom: 0; }.config-section h3 { margin: 0; color: var(--ink); font-size: 13px; }.config-section > p, .config-section-head p { margin: 6px 0 15px; color: var(--muted); font-size: 10px; }.config-option-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }.config-option-grid label { min-height: 57px; display: flex; align-items: center; justify-content: space-between; padding: 11px 13px; border: 1px solid var(--line); border-radius: 6px; }.config-option-grid strong, .config-option-grid small { display: block; }.config-option-grid strong { color: var(--ink-soft); font-size: 11px; }.config-option-grid small { color: var(--muted); font-size: 9px; margin-top: 3px; }.config-field-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 13px; }.config-field-grid label, .array-config label { display: block; color: var(--muted); font-size: 10px; margin-bottom: 6px; }.config-field-grid .el-input-number, .config-field-grid .el-select { width: 100%; }.array-config { display: grid; grid-template-columns: 180px minmax(0, 280px); gap: 22px; align-items: center; }.array-config > label { display: flex; align-items: center; justify-content: space-between; color: var(--ink-soft); margin: 0; }.config-section-head { display: flex; justify-content: space-between; align-items: flex-start; }.path-row, .mapping-edit-row { display: flex; align-items: center; gap: 8px; margin-bottom: 9px; }.path-row .el-input, .mapping-edit-row .el-input { flex: 1; }.path-row button, .mapping-edit-row button { width: 28px; height: 28px; display: grid; place-items: center; border: 0; color: var(--muted); background: transparent; }.path-row button:hover, .mapping-edit-row button:hover { color: var(--red); }.mapping-header { display: grid; grid-template-columns: 1fr 22px 1fr 28px; gap: 8px; color: var(--muted); font-size: 10px; margin-bottom: 7px; }.mapping-edit-row > span { color: var(--teal); }.empty-inline { padding: 14px 0; color: var(--muted); font-size: 11px; }.rule-editor-foot { min-height: 60px; display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 0 23px; border-top: 1px solid var(--line); color: var(--muted); font-size: 10px; }.rule-editor-foot > span { display: inline-flex; align-items: center; gap: 6px; }
@media (max-width: 850px) { .config-layout { grid-template-columns: 1fr; }.profile-panel { order: 2; }.profile-list { display: flex; overflow: auto; gap: 6px; }.profile-item { min-width: 210px; }.profile-panel-foot { display: none; }.config-field-grid { grid-template-columns: 1fr; }.array-config { grid-template-columns: 1fr; gap: 12px; }.profile-name-row .el-input { width: 100%; }.profile-description { width: 100%; }.rule-editor-head { flex-direction: column; gap: 12px; } }
</style>

