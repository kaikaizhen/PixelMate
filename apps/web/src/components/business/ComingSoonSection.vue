<script setup lang="ts">
import SpriteStrip from '@/components/common/SpriteStrip.vue'
import { APP_PATH, IS_APP_OPEN } from '@/constants/launch'
import petIdle from '@/assets/pixel/pet_idle.png'
import star from '@/assets/pixel/star.png'
import moon from '@/assets/pixel/moon.png'
import grass from '@/assets/pixel/grass_night.png'

// 固定位置（0–100%），避免 SSR 與前端不一致
const STARS = [
  { x: 8, y: 14 }, { x: 22, y: 38 }, { x: 36, y: 10 }, { x: 60, y: 6 }, { x: 66, y: 12 },
  { x: 80, y: 36 }, { x: 92, y: 18 }, { x: 14, y: 62 }, { x: 88, y: 58 }, { x: 30, y: 84 },
]
</script>

<template>
  <section id="soon" class="soon" aria-labelledby="soon-title">
    <div class="soon__sky" aria-hidden="true">
      <span
        v-for="(s, i) in STARS"
        :key="i"
        class="soon__star"
        :style="{ left: `${s.x}%`, top: `${s.y}%`, '--delay': `${i * 0.4}s`, '--s': 3 }"
      >
        <SpriteStrip :src="star" :frames="4" :width="5" :height="5" :duration="`${1.8 + (i % 3) * 0.5}s`" :delay="`${i * -0.37}s`" />
      </span>
      <img class="soon__moon pixelated" :src="moon" alt="" width="16" height="16" />
    </div>

    <div class="page-width soon__inner">
      <div class="soon__pet" aria-hidden="true">
        <SpriteStrip :src="petIdle" :frames="4" :width="48" :height="48" duration="0.9s" />
      </div>
      <h2 id="soon-title" class="soon__title">一個小小的世界，<br />即將與你見面。</h2>
      <p class="soon__lead">先在手機裡認識牠，未來，也能讓牠住進你的桌面。</p>

      <a v-if="IS_APP_OPEN" class="soon__cta" :href="APP_PATH">開始養一隻</a>
      <button v-else type="button" class="soon__cta soon__cta--soon" disabled>即將開放</button>
    </div>

    <div class="soon__grass" aria-hidden="true">
      <SpriteStrip v-for="x in 24" :key="x" :src="grass" :frames="4" :width="64" :height="16" :x="(x - 1) * 64" duration="2.4s" />
    </div>
  </section>
</template>

<style scoped>
.soon {
  --s: 3;
  position: relative;
  overflow: hidden;
  padding-block: var(--space-5) calc(16px * 3 + var(--space-3));
  background: linear-gradient(#232a50, #3a4575);
  color: #fff8ec;
  text-align: center;
}

.soon__sky {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.soon__star {
  position: absolute;
  width: calc(5px * 3);
  height: calc(5px * 3);
}

.soon__moon {
  position: absolute;
  right: 8%;
  top: 12%;
  width: 64px;
  height: 64px;
  filter: drop-shadow(0 0 12px rgba(255, 233, 168, 0.7));
}

.soon__inner {
  position: relative;
  display: grid;
  justify-items: center;
  gap: var(--space-2);
}

.soon__pet {
  --s: 4;
  position: relative;
  width: calc(48px * 4);
  height: calc(48px * 4);
}

.soon__title {
  margin: 0;
  font-size: clamp(1.8rem, 6vw, 2.8rem);
  line-height: 1.35;
  font-weight: 900;
  text-wrap: balance;
  text-shadow: 3px 3px 0 #f4a6a0;
}

.soon__lead {
  margin: 0;
  max-width: 26em;
  font-size: 1.1rem;
  color: #e9ddcf;
}

.soon__cta {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 11rem;
  min-height: var(--tap-min);
  margin-top: var(--space-2);
  padding: 0 1.8rem;
  border: 3px solid #fff8ec;
  background: var(--c-yellow);
  color: var(--c-ink);
  font: inherit;
  font-size: 1.1rem;
  font-weight: 900;
  text-decoration: none;
  box-shadow: 4px 4px 0 #fff8ec;
}

.soon__cta--soon {
  background: #5b6694;
  color: #f1ecf7;
  border-style: dashed;
  box-shadow: none;
  cursor: not-allowed;
}

.soon__grass {
  position: absolute;
  inset-inline: 0;
  bottom: 0;
  height: calc(16px * 3);
  overflow: hidden;
  pointer-events: none;
}
</style>
