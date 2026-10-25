<script setup lang="ts">
import { computed } from 'vue'

// 橫向多格 spritesheet，以 CSS steps() 逐格播放；尺寸單位為「邏輯像素」，由外層 --s 放大。
const props = withDefaults(
  defineProps<{
    src: string
    frames: number
    width: number
    height: number
    x?: number
    y?: number
    duration?: string
    delay?: string
  }>(),
  { x: 0, y: 0, duration: '1s', delay: '0s' },
)

const style = computed(() => ({
  '--fw': props.width,
  '--fh': props.height,
  '--frames': props.frames,
  '--dur': props.duration,
  '--delay': props.delay,
  '--x': props.x,
  '--y': props.y,
  backgroundImage: `url(${props.src})`,
}))
</script>

<template>
  <span class="sprite pixelated" :style="style" aria-hidden="true" />
</template>

<style scoped>
.sprite {
  position: absolute;
  left: calc(var(--x) * var(--s) * 1px);
  top: calc(var(--y) * var(--s) * 1px);
  width: calc(var(--fw) * var(--s) * 1px);
  height: calc(var(--fh) * var(--s) * 1px);
  background-repeat: no-repeat;
  background-size: calc(var(--fw) * var(--frames) * var(--s) * 1px) 100%;
  animation: sprite-play var(--dur) steps(var(--frames)) var(--delay) infinite;
  pointer-events: none;
}

@keyframes sprite-play {
  to {
    background-position-x: calc(var(--fw) * var(--frames) * var(--s) * -1px);
  }
}
</style>
