<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import * as monaco from 'monaco-editor'
import EditorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker'
import { Clipboard, Code2, FileCheck2, Search, Sparkles } from 'lucide-vue-next'
import { ElMessage } from 'element-plus'

const globalScope = self as typeof self & {
  MonacoEnvironment?: { getWorker: () => Worker }
}
globalScope.MonacoEnvironment = {
  getWorker: () => new EditorWorker(),
}

const props = withDefaults(defineProps<{
  modelValue: string
  label?: string
  accent?: 'teal' | 'amber'
  readonly?: boolean
}>(), {
  label: 'JSON',
  accent: 'teal',
  readonly: false,
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
  valid: [value: boolean]
}>()

const editorHost = ref<HTMLElement>()
let editor: monaco.editor.IStandaloneCodeEditor | undefined
let model: monaco.editor.ITextModel | undefined

function formatJson() {
  if (!editor) return
  try {
    const parsed = JSON.parse(editor.getValue())
    const formatted = JSON.stringify(parsed, null, 2)
    editor.setValue(formatted)
    emit('update:modelValue', formatted)
    monaco.editor.setModelMarkers(model!, 'json-compare', [])
    emit('valid', true)
    ElMessage.success('JSON 已格式化')
  } catch (error) {
    validateJson()
    ElMessage.error(error instanceof Error ? error.message : 'JSON 格式无效')
  }
}

function validateJson() {
  if (!editor || !model) return false
  try {
    JSON.parse(editor.getValue())
    monaco.editor.setModelMarkers(model, 'json-compare', [])
    emit('valid', true)
    return true
  } catch (error) {
    const message = error instanceof Error ? error.message : 'JSON 格式无效'
    monaco.editor.setModelMarkers(model, 'json-compare', [{
      startLineNumber: 1,
      startColumn: 1,
      endLineNumber: model.getLineCount(),
      endColumn: model.getLineMaxColumn(model.getLineCount()),
      message,
      severity: monaco.MarkerSeverity.Error,
    }])
    emit('valid', false)
    return false
  }
}

async function copyJson() {
  if (!editor) return
  await navigator.clipboard?.writeText(editor.getValue())
  ElMessage.success('已复制 JSON')
}

function findText() {
  editor?.getAction('actions.find')?.run()
}

function emitValue() {
  if (editor) emit('update:modelValue', editor.getValue())
}

onMounted(async () => {
  await nextTick()
  if (!editorHost.value) return
  model = monaco.editor.createModel(props.modelValue, 'json')
  editor = monaco.editor.create(editorHost.value, {
    model,
    theme: document.documentElement.classList.contains('is-dark') ? 'vs-dark' : 'vs',
    automaticLayout: true,
    minimap: { enabled: false },
    fontFamily: 'DM Mono, Consolas, monospace',
    fontSize: 12,
    lineHeight: 20,
    lineNumbersMinChars: 3,
    padding: { top: 13, bottom: 13 },
    scrollBeyondLastLine: false,
    wordWrap: 'on',
    tabSize: 2,
    readOnly: props.readonly,
    renderLineHighlight: 'line',
    folding: true,
  })
  editor.onDidChangeModelContent(emitValue)
  validateJson()
})

watch(() => props.modelValue, (value) => {
  if (editor && value !== editor.getValue()) editor.setValue(value)
})

onBeforeUnmount(() => {
  editor?.dispose()
  model?.dispose()
})

defineExpose({ formatJson, validateJson, findText })
</script>

<template>
  <div class="json-editor" :class="`accent-${accent}`">
    <div class="json-editor-toolbar">
      <span class="editor-label"><Code2 :size="14" />{{ label }}</span>
      <div class="editor-actions">
        <el-tooltip content="校验 JSON" placement="top">
          <button class="editor-icon" type="button" @click="validateJson"><FileCheck2 :size="14" /></button>
        </el-tooltip>
        <el-tooltip content="格式化" placement="top">
          <button class="editor-icon" type="button" @click="formatJson"><Sparkles :size="14" /></button>
        </el-tooltip>
        <el-tooltip content="搜索" placement="top">
          <button class="editor-icon" type="button" @click="findText"><Search :size="14" /></button>
        </el-tooltip>
        <el-tooltip content="复制内容" placement="top">
          <button class="editor-icon" type="button" @click="copyJson"><Clipboard :size="14" /></button>
        </el-tooltip>
      </div>
    </div>
    <div ref="editorHost" class="editor-host"></div>
  </div>
</template>

<style scoped>
.json-editor { display: flex; flex-direction: column; height: 100%; min-height: 0; background: var(--surface); }
.json-editor-toolbar { height: 39px; flex: 0 0 39px; display: flex; align-items: center; justify-content: space-between; padding: 0 12px; border-bottom: 1px solid var(--line-soft); }
.editor-label { display: inline-flex; align-items: center; gap: 6px; color: var(--ink-soft); font-size: 11px; font-weight: 700; }
.accent-teal .editor-label { color: var(--teal-dark); }
.accent-amber .editor-label { color: var(--amber); }
.editor-actions { display: flex; gap: 2px; }
.editor-icon { width: 27px; height: 27px; display: grid; place-items: center; color: var(--muted); background: transparent; border: 0; border-radius: 4px; padding: 0; }
.editor-icon:hover { color: var(--teal-dark); background: var(--surface-alt); }
.editor-host { min-height: 240px; flex: 1; }
</style>

