<script setup lang="ts">
import PixelStage from '@/components/business/PixelStage.vue'
import SpriteStrip from '@/components/common/SpriteStrip.vue'
import PixelWindow from '@/components/business/PixelWindow.vue'
import type { WeatherScene } from '@/types/weather.types'
import room from '@/assets/pixel/room.png'
import plant from '@/assets/pixel/plant.png'
import curtainL from '@/assets/pixel/curtain_l.png'
import curtainR from '@/assets/pixel/curtain_r.png'

defineProps<{ weather: WeatherScene }>()

const sceneLabel = { sunny: '晴天', rain: '雨天', night: '夜晚' } as const
</script>

<template>
  <PixelStage :height="240" :class="`scene--${weather}`" :label="`小房間，窗外是${sceneLabel[weather]}`">
        <div class="scene__window"><PixelWindow :weather="weather" /></div>
        <img class="scene__layer scene__room-img pixelated" :src="room" alt="" width="240" height="240" />
        <SpriteStrip :src="plant" :frames="4" :width="44" :height="80" :x="15" :y="64" duration="calc(var(--wind) * 2)" />
        <SpriteStrip :src="curtainL" :frames="4" :width="14" :height="84" :x="122" :y="24" duration="calc(var(--wind) * 2)" />
        <SpriteStrip :src="curtainR" :frames="4" :width="14" :height="84" :x="208" :y="24" duration="calc(var(--wind) * 2)" delay="-0.4s" />
        <slot />
  </PixelStage>
</template>

<style scoped>
.scene__layer,
.scene__window {
  position: absolute;
}

.scene__layer {
  left: 0;
  top: 0;
  width: 100%;
  height: 100%;
  transition: filter 0.6s;
}

.scene__window {
  left: calc(140px * var(--s));
  top: calc(28px * var(--s));
  width: calc(64px * var(--s));
  height: calc(72px * var(--s));
}

.scene--sunny {
  --wind: 1.6s;
}

.scene--rain {
  --wind: 0.8s;
}

.scene--night {
  --wind: 2.6s;
}

.scene--night .scene__room-img,
.scene--night .sprite {
  filter: brightness(0.62) saturate(0.9) hue-rotate(-12deg);
}
</style>
