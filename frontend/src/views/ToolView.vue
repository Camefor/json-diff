<script setup lang="ts">
import { ExternalLink, RefreshCw, Wrench } from 'lucide-vue-next'

const toolUrl = '/tools/jsontool/index.html'

function reloadTool() {
  const frame = document.querySelector<HTMLIFrameElement>('.tool-frame')
  frame?.contentWindow?.location.reload()
}
</script>

<template>
  <div class="view tool-view">
    <div class="view-heading">
      <div>
        <p class="eyebrow">TOOLS / 07</p>
        <h1>JSON 小工具</h1>
        <p>嵌入项目中的独立 JSON 数据比对工具，方便在同一个工作台内快速使用。</p>
      </div>
      <div class="heading-actions">
        <el-button plain @click="reloadTool"><RefreshCw :size="15" />刷新工具</el-button>
        <el-button type="primary" tag="a" :href="toolUrl" target="_blank" rel="noopener">
          <ExternalLink :size="15" />新窗口打开
        </el-button>
      </div>
    </div>

    <section class="tool-panel panel">
      <div class="panel-header">
        <div class="panel-title"><Wrench :size="16" class="title-icon" />JSON 数据比对工具</div>
        <span class="subtle">静态资源：frontend/public/tools/jsontool/index.html</span>
      </div>
      <!-- 小工具是独立 HTML 应用，用 iframe 隔离其样式和运行时，避免影响主平台。 -->
      <iframe class="tool-frame" :src="toolUrl" title="JSON 数据比对工具"></iframe>
    </section>
  </div>
</template>

<style scoped>
.tool-view {
  max-width: 1680px;
  margin: 0 auto;
}

.tool-panel {
  overflow: hidden;
}

.tool-frame {
  display: block;
  width: 100%;
  min-height: calc(100vh - 220px);
  border: 0;
  background: #f5f7fa;
}

@media (max-width: 760px) {
  .tool-frame {
    min-height: calc(100vh - 190px);
  }
}
</style>
