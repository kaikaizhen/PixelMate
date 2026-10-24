<script setup lang="ts">
import PixelRoom from '@/components/business/PixelRoom.vue'
import SpriteStrip from '@/components/common/SpriteStrip.vue'
import type { WeatherScene } from '@/types/weather.types'
import petWalk from '@/assets/pixel/pet_walk.png'
import petWatch from '@/assets/pixel/pet_watch.png'
import petSleep from '@/assets/pixel/pet_sleep.png'
import zzz from '@/assets/pixel/zzz.png'

defineProps<{ weather: WeatherScene }>()
</script>

<template>
  <PixelRoom :weather="weather">
    <!-- 晴天：在地毯上來回散步 -->
    <div v-if="weather === 'sunny'" class="wr__walker">
      <SpriteStrip :src="petWalk" :frames="4" :width="48" :height="48" duration="0.6s" />
    </div>

    <!-- 雨天：窩在窗邊看雨 -->
    <template v-else-if="weather === 'rain'">
      <SpriteStrip :src="petWatch" :frames="4" :width="48" :height="48" :x="146" :y="130" duration="1.8s" />
    </template>

    <!-- 夜晚：睡著了 -->
    <template v-else>
      <SpriteStrip :src="petSleep" :frames="2" :width="48" :height="48" :x="96" :y="168" duration="2.6s" />
      <SpriteStrip :src="zzz" :frames="3" :width="10" :height="12" :x="130" :y="162" duration="2.4s" />
    </template>
  </PixelRoom>
</template>

<style scoped>
.wr__walker {
  position: absolute;
  left: calc(60px * var(--s));
  top: calc(166px * var(--s));
  width: calc(48px * var(--s));
  height: calc(48px * var(--s));
  animation: walk-back-forth 8s linear infinite;
}

/* 每次位移 1 邏輯像素，兩端轉身 */
@keyframes walk-back-forth {
  0% {
    transform: translateX(0) scaleX(1);
    animation-timing-function: steps(80);
  }
  49.99% {
    transform: translateX(calc(80px * var(--s))) scaleX(1);
  }
  50% {
    transform: translateX(calc(80px * var(--s))) scaleX(-1);
    animation-timing-function: steps(80);
  }
  100% {
    transform: translateX(0) scaleX(-1);
  }
}
</style>
