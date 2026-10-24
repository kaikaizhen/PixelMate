<script setup lang="ts">
import PixelPet from '@/components/business/PixelPet.vue'
import { usePetReaction } from '@/composables/usePetReaction'
import { useWeatherStore } from '@/stores/useWeatherStore'
import room from '@/assets/pixel/room.png'
import SpriteStrip from '@/components/common/SpriteStrip.vue'
import PixelWindow from '@/components/business/PixelWindow.vue'
import plant from '@/assets/pixel/plant.png'
import curtainL from '@/assets/pixel/curtain_l.png'
import curtainR from '@/assets/pixel/curtain_r.png'

const PHRASES = [
  '今天的陽光好舒服。',
  '你回來啦！',
  '我剛剛睡了一個小午覺。',
  '窗邊好像有小鳥！',
  '再摸一下也可以喔。',
] as const

const weather = useWeatherStore()
const { mood, message, touch } = usePetReaction(PHRASES)

const sceneLabel = { sunny: '晴天', rain: '雨天', night: '夜晚' } as const
</script>

<template>
  <div class="scene" :class="`scene--${weather.scene}`" role="group" :aria-label="`小房間，窗外是${sceneLabel[weather.scene]}`">
    <div class="scene__viewport">
      <div class="scene__room">
        <div class="scene__window"><PixelWindow :weather="weather.scene" /></div>
        <img class="scene__layer scene__room-img pixelated" :src="room" alt="" width="240" height="240" />
        <SpriteStrip :src="plant" :frames="4" :width="44" :height="80" :x="15" :y="64" duration="calc(var(--wind) * 2)" />
        <SpriteStrip :src="curtainL" :frames="4" :width="14" :height="84" :x="122" :y="24" duration="calc(var(--wind) * 2)" />
        <SpriteStrip :src="curtainR" :frames="4" :width="14" :height="84" :x="208" :y="24" duration="calc(var(--wind) * 2)" delay="-0.4s" />
        <div class="scene__pet">
          <PixelPet :mood="mood" @touch="touch" />
          <p v-if="message" class="scene__bubble" role="status">{{ message }}</p>
        </div>
      </div>
    </div>
    <p class="scene__tag">互動預覽</p>
  </div>
</template>

<style scoped>
.scene {
  --s: 2;
  --crop-x: 56;
  --view-w: 160;
  --wind: 1.6s;
  position: relative;
  width: fit-content;
  margin-inline: auto;
}

.scene__viewport {
  position: relative;
  overflow: hidden;
  width: calc(var(--view-w) * var(--s) * 1px);
  height: calc(240px * var(--s));
  border: 4px solid var(--c-ink);
  box-shadow: 8px 8px 0 var(--c-ink);
  background: var(--c-blue);
}

.scene__room {
  position: absolute;
  top: 0;
  left: calc(var(--crop-x) * var(--s) * -1px);
  width: calc(240px * var(--s));
  height: calc(240px * var(--s));
}

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

.scene__pet {
  position: absolute;
  left: calc(96px * var(--s));
  top: calc(166px * var(--s));
}

.scene__bubble {
  position: absolute;
  left: 50%;
  bottom: calc(100% + 6px);
  transform: translateX(-50%);
  margin: 0;
  padding: 0.35rem 0.7rem;
  width: max-content;
  max-width: 11em;
  background: #fff;
  border: 3px solid var(--c-ink);
  font-size: 0.85rem;
  font-weight: 700;
  line-height: 1.4;
  pointer-events: none;
}

.scene__tag {
  position: absolute;
  right: -4px;
  bottom: -2.2rem;
  margin: 0;
  padding: 0.1rem 0.6rem;
  font-size: 0.8rem;
  color: var(--c-ink-soft);
}

@media (min-width: 480px) {
  .scene {
    --crop-x: 0;
    --view-w: 240;
  }
}

@media (min-width: 1400px) {
  .scene {
    --s: 3;
  }
}
</style>
