<script setup lang="ts">
import SpriteStrip from '@/components/common/SpriteStrip.vue'
import type { WeatherScene } from '@/types/weather.types'
import skySunny from '@/assets/pixel/window_sky_sunny.png'
import skyRain from '@/assets/pixel/window_sky_rain.png'
import skyNight from '@/assets/pixel/window_sky_night.png'
import cloudWhite from '@/assets/pixel/cloud_white.png'
import cloudGrey from '@/assets/pixel/cloud_grey.png'
import cloudDark from '@/assets/pixel/cloud_dark.png'
import sun from '@/assets/pixel/sun.png'
import moon from '@/assets/pixel/moon.png'
import star from '@/assets/pixel/star.png'
import grassDay from '@/assets/pixel/grass_day.png'
import grassRain from '@/assets/pixel/grass_rain.png'
import grassNight from '@/assets/pixel/grass_night.png'
import ripple from '@/assets/pixel/ripple.png'
import bars from '@/assets/pixel/window_bars.png'

defineProps<{ weather: WeatherScene }>()

const sky = { sunny: skySunny, rain: skyRain, night: skyNight } as const
const grass = { sunny: grassDay, rain: grassRain, night: grassNight } as const

// 固定的位置與延遲，讓 SSR 與前端 hydration 一致。
const DROPS = [4, 11, 17, 23, 29, 35, 41, 47, 53, 59, 8, 20, 38, 50]
const STARS = [
  { x: 6, y: 8 }, { x: 24, y: 4 }, { x: 40, y: 14 }, { x: 54, y: 6 }, { x: 14, y: 26 }, { x: 34, y: 28 }, { x: 56, y: 30 },
]
const FIREFLIES = [{ x: 12, y: 50 }, { x: 36, y: 46 }, { x: 52, y: 52 }]
</script>

<template>
  <div class="win" :class="`win--${weather}`">
    <img class="win__sky pixelated" :src="sky[weather]" alt="" width="64" height="72" />

    <template v-if="weather === 'sunny'">
      <SpriteStrip :src="sun" :frames="2" :width="24" :height="24" :x="36" :y="2" duration="1.6s" />
      <span class="win__cloud win__cloud--a"><img class="pixelated" :src="cloudWhite" alt="" width="26" height="11" /></span>
      <span class="win__cloud win__cloud--b"><img class="pixelated" :src="cloudWhite" alt="" width="26" height="11" /></span>
    </template>

    <template v-else-if="weather === 'rain'">
      <span class="win__cloud win__cloud--a win__cloud--fast"><img class="pixelated" :src="cloudDark" alt="" width="30" height="13" /></span>
      <span class="win__cloud win__cloud--b win__cloud--fast"><img class="pixelated" :src="cloudGrey" alt="" width="30" height="13" /></span>
      <span
        v-for="(x, i) in DROPS"
        :key="x"
        class="win__drop"
        :style="{ '--dx': x, '--dd': `${(i % 7) * 0.11}s` }"
      />
      <SpriteStrip v-for="(x, i) in [8, 30, 48]" :key="x" :src="ripple" :frames="3" :width="9" :height="3" :x="x" :y="59 + (i % 2)" duration="0.9s" :delay="`${i * 0.3}s`" />
      <span class="win__flash" />
    </template>

    <template v-else>
      <img class="win__moon pixelated" :src="moon" alt="" width="16" height="16" />
      <SpriteStrip
        v-for="(s, i) in STARS"
        :key="i"
        :src="star"
        :frames="4"
        :width="5"
        :height="5"
        :x="s.x"
        :y="s.y"
        :duration="`${1.6 + (i % 3) * 0.5}s`"
        :delay="`${i * 0.37}s`"
      />
      <span v-for="(f, i) in FIREFLIES" :key="i" class="win__firefly" :style="{ '--fx': f.x, '--fy': f.y, '--fd': `${i * 1.1}s` }" />
    </template>

    <SpriteStrip :src="grass[weather]" :frames="4" :width="64" :height="16" :x="0" :y="56" duration="var(--wind)" />
    <img class="win__bars pixelated" :src="bars" alt="" width="64" height="72" />
  </div>
</template>

<style scoped>
.win {
  position: absolute;
  inset: 0;
  overflow: hidden;
}

.win img,
.win__cloud,
.win__drop,
.win__firefly,
.win__flash {
  position: absolute;
}

.win__sky,
.win__bars {
  left: 0;
  top: 0;
  width: 100%;
  height: 100%;
}

.win__moon {
  left: calc(8px * var(--s));
  top: calc(8px * var(--s));
  width: calc(16px * var(--s));
  height: calc(16px * var(--s));
  filter: drop-shadow(0 0 calc(3px * var(--s)) rgba(255, 233, 168, 0.7));
}

/* 雲：每次移動 1 邏輯像素，維持像素對齊 */
.win__cloud {
  top: calc(var(--cy, 8) * var(--s) * 1px);
  left: calc(-32px * var(--s));
  animation: drift var(--cloud-dur, 70s) steps(100) infinite;
  animation-delay: var(--cloud-delay, 0s);
}

.win__cloud img {
  position: static;
  width: calc(var(--cw, 26) * var(--s) * 1px);
  height: auto;
}

.win__cloud--a {
  --cy: 12;
  --cw: 26;
}

.win__cloud--b {
  --cy: 30;
  --cw: 26;
  --cloud-delay: -35s;
}

.win--rain .win__cloud {
  --cw: 30;
  --cy: 2;
  --cloud-dur: 36s;
}

.win--rain .win__cloud--b {
  --cy: 10;
  --cloud-delay: -18s;
}

@keyframes drift {
  to {
    transform: translateX(calc(100px * var(--s)));
  }
}

.win__drop {
  left: calc(var(--dx) * var(--s) * 1px);
  top: calc(-6px * var(--s));
  width: calc(1px * var(--s));
  height: calc(5px * var(--s));
  background: #d9ecff;
  animation: fall 0.6s steps(14) infinite;
  animation-delay: var(--dd);
}

@keyframes fall {
  to {
    transform: translate(calc(-5px * var(--s)), calc(66px * var(--s)));
  }
}

.win__flash {
  inset: 0;
  background: #fff;
  opacity: 0;
  animation: flash 9s steps(1) infinite;
}

@keyframes flash {
  0%,
  91%,
  93%,
  96%,
  100% {
    opacity: 0;
  }
  92%,
  95% {
    opacity: 0.55;
  }
}

.win__firefly {
  left: calc(var(--fx) * var(--s) * 1px);
  top: calc(var(--fy) * var(--s) * 1px);
  width: calc(2px * var(--s));
  height: calc(2px * var(--s));
  background: #ffe97a;
  box-shadow: 0 0 calc(3px * var(--s)) #ffe97a;
  animation: firefly 3.2s steps(8) infinite;
  animation-delay: var(--fd);
}

@keyframes firefly {
  0%,
  100% {
    opacity: 0.1;
    transform: translate(0, 0);
  }
  50% {
    opacity: 1;
    transform: translate(calc(4px * var(--s)), calc(-5px * var(--s)));
  }
}
</style>
