<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { createFx, fxFor, type Fx } from '@/utils/weatherFx'
import { useSettingsStore } from '@/stores/useSettingsStore'

// Moving weather (rain, snow, drifting clouds, twinkling stars...) on a w x h logical-pixel canvas scaled up crisply.
// It sits on top of the weather picture; the picture is the scenery, this is what moves in it.
const props = defineProps<{ weather: string; w: number; h: number }>()
const canvas = ref<HTMLCanvasElement | null>(null)
const kinds = computed(() => fxFor(props.weather).join(','))

const FPS = 12                                   // chunky on purpose: it should feel like the pixel art, not like video
let fx: Fx | null = null
let raf = 0
let last = 0
let acc = 0
const settings = useSettingsStore()           // the player's own switch (帳號 > 天氣動態); the system's reduce-motion flag no longer decides

function build() {
  fx = createFx(props.weather, props.w, props.h, 7)
  const ctx = canvas.value?.getContext('2d')
  if (ctx && fx) { fx.update(0); fx.draw(ctx) }  // never an empty first frame
}

function tick(now: number) {
  raf = requestAnimationFrame(tick)
  if (document.visibilityState !== 'visible') { last = now; return }
  acc += Math.min(now - last, 250) / 1000
  last = now
  const step = 1 / FPS
  if (acc < step) return
  const ctx = canvas.value?.getContext('2d')
  if (!ctx || !fx) return
  fx.update(acc)
  acc = 0
  fx.draw(ctx)
}

function run(on: boolean) {
  cancelAnimationFrame(raf)
  if (on) { last = performance.now(); acc = 0; raf = requestAnimationFrame(tick) }
}

onMounted(() => {
  build()
  run(settings.weatherMotion)
})
watch(() => settings.weatherMotion, run)
watch(() => [props.weather, props.w, props.h], build)
onBeforeUnmount(() => cancelAnimationFrame(raf))
</script>

<template>
  <canvas v-if="kinds" ref="canvas" class="fx pixelated" data-weather-fx :data-kind="kinds" :width="w" :height="h" aria-hidden="true" />
</template>

<style scoped>
.fx { position: absolute; inset: 0; width: 100%; height: 100%; image-rendering: pixelated; pointer-events: none; }
</style>
