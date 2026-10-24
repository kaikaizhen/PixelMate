<script setup lang="ts">
import { computed } from 'vue'

// 像素舞台：負責整數倍放大、手機裁切與外框；內容以邏輯像素（寬 240）定位。
const props = withDefaults(defineProps<{ height: number; label: string; cropX?: number }>(), { cropX: 56 })

const vars = computed(() => ({ '--stage-h': props.height, '--crop-mobile': props.cropX }))
</script>

<template>
  <div class="stage" :style="vars" role="group" :aria-label="label">
    <div class="stage__viewport">
      <div class="stage__world">
        <slot />
      </div>
    </div>
    <p class="stage__tag">互動預覽</p>
  </div>
</template>

<style scoped>
.stage {
  --s: 2;
  --crop-x: var(--crop-mobile);
  --view-w: 160;
  position: relative;
  width: fit-content;
  margin-inline: auto;
}

.stage__viewport {
  position: relative;
  overflow: hidden;
  width: calc(var(--view-w) * var(--s) * 1px);
  height: calc(var(--stage-h) * var(--s) * 1px);
  border: 4px solid var(--c-ink);
  box-shadow: 8px 8px 0 var(--c-ink);
  background: var(--c-blue);
}

.stage__world {
  position: absolute;
  top: 0;
  left: calc(var(--crop-x) * var(--s) * -1px);
  width: calc(240px * var(--s));
  height: calc(var(--stage-h) * var(--s) * 1px);
}

.stage__tag {
  position: absolute;
  right: -4px;
  bottom: -2.2rem;
  margin: 0;
  padding: 0.1rem 0.6rem;
  font-size: 0.8rem;
  color: var(--c-ink-soft);
}

@media (min-width: 480px) {
  .stage {
    --crop-x: 0;
    --view-w: 240;
  }
}

@media (min-width: 1400px) {
  .stage {
    --s: 3;
  }
}
</style>
