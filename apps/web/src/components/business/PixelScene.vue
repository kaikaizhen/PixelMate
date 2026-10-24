<script setup lang="ts">
import PixelPet from '@/components/business/PixelPet.vue'
import PixelRoom from '@/components/business/PixelRoom.vue'
import { usePetReaction } from '@/composables/usePetReaction'
import { useWeatherStore } from '@/stores/useWeatherStore'

const PHRASES = [
  '今天的陽光好舒服。',
  '你回來啦！',
  '我剛剛睡了一個小午覺。',
  '窗邊好像有小鳥！',
  '再摸一下也可以喔。',
] as const

const weather = useWeatherStore()
const { mood, message, touch } = usePetReaction(PHRASES)
</script>

<template>
  <PixelRoom :weather="weather.scene">
    <div class="scene__pet">
      <PixelPet :mood="mood" @touch="touch" />
      <p v-if="message" class="scene__bubble" role="status">{{ message }}</p>
    </div>
  </PixelRoom>
</template>

<style scoped>
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
</style>
