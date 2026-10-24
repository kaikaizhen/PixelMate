<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import SpriteStrip from '@/components/common/SpriteStrip.vue'
import GamePet from './GamePet.vue'
import { DEFAULT_FURNITURE, PET_SPOT, ROOM_SIZE, WINDOW, WINDOW_VIEW } from '@/constants/defaultRoom'
import { useSceneScale } from '@/composables/useSceneScale'
import { useGameStore } from '@/stores/useGameStore'
import { windowWeatherKey } from '@/utils/roomWeather'
import room from '@/assets/pixel/room.png'
import plant from '@/assets/pixel/plant.png'
import curtainL from '@/assets/pixel/curtain_l.png'
import curtainR from '@/assets/pixel/curtain_r.png'
import bars from '@/assets/pixel/window_bars.png'

// The house: the website's room art + the weather outside (game assets by assetKey) + furniture + the pet.
const props = defineProps<{ color: string; happy: boolean; name: string; weather?: string; bare?: boolean; scale?: number }>()
defineEmits<{ pat: [] }>()

const el = ref<HTMLElement | null>(null)
const measured = useSceneScale(el)
const s = computed(() => props.scale ?? measured.value)   // fullscreen passes its own scale
const game = useGameStore()
onMounted(() => void game.load())

const weatherUrl = computed(() => game.url(windowWeatherKey(new Date(), props.weather)))
const u = (n: number) => `calc(${n} * var(--s) * 1px)`
const night = computed(() => windowWeatherKey(new Date(), props.weather).includes('starry'))
</script>

<template>
  <div ref="el" class="scene" :class="{ bare }" :style="{ '--s': s, aspectRatio: `${ROOM_SIZE} / ${ROOM_SIZE}` }" role="group" aria-label="你的小屋">
    <!-- 1. outside the window -->
    <div class="win pixelated" :style="{ left: u(WINDOW.x), top: u(WINDOW.y), width: u(WINDOW.w), height: u(WINDOW.h),
        backgroundImage: weatherUrl ? `url(${weatherUrl})` : undefined,
        backgroundSize: `${u(ROOM_SIZE)} ${u(ROOM_SIZE)}`, backgroundPosition: `${u(-WINDOW_VIEW.x)} ${u(-WINDOW_VIEW.y)}` }" />
    <!-- 2. the house -->
    <img draggable="false" class="layer pixelated" :src="room" alt="" />
    <img draggable="false" class="abs pixelated" :src="bars" alt="" :style="{ left: u(WINDOW.x), top: u(WINDOW.y), width: u(WINDOW.w), height: u(WINDOW.h) }" />
    <SpriteStrip :src="plant" :frames="4" :width="44" :height="80" :x="15" :y="64" duration="2.4s" />
    <SpriteStrip :src="curtainL" :frames="4" :width="14" :height="84" :x="122" :y="24" duration="3.2s" />
    <SpriteStrip :src="curtainR" :frames="4" :width="14" :height="84" :x="208" :y="24" duration="3.2s" delay="-0.4s" />
    <!-- 3. furniture, by assetKey -->
    <template v-for="f in DEFAULT_FURNITURE" :key="f.key">
      <img draggable="false" v-if="game.url(f.key)" class="abs pixelated" :src="game.url(f.key)" alt="" :style="{ left: u(f.x), top: u(f.y), width: u(f.w), height: u(f.w) }" />
    </template>
    <!-- 4. the pet -->
    <button type="button" class="pet" :class="{ hop: happy }" :style="{ left: u(PET_SPOT.x), top: u(PET_SPOT.y) }" :aria-label="`摸摸 ${name}`" @click="$emit('pat')">
      <GamePet :color="color" :face="happy ? 'happy' : 'normal'" />
    </button>
    <span v-if="night && !bare" class="dim" aria-hidden="true" />
  </div>
</template>

<style scoped>
.scene { position: relative; width: 100%; overflow: hidden; border: 3px solid var(--c-ink); border-radius: 14px; background: var(--c-pink-soft); }
.scene.bare { border: 0; border-radius: 0; background: transparent; overflow: visible; }
.layer, .abs, .win { position: absolute; }
.layer { inset: 0; width: 100%; height: 100%; }
.win { background-repeat: no-repeat; background-color: var(--c-blue); }
.pet { position: absolute; padding: 0; background: none; border: 0; cursor: pointer; border-radius: 8px; }
.hop { animation: hop 0.5s steps(4) 1; }
@keyframes hop { 40% { transform: translateY(calc(-10 * var(--s) * 1px)); } }
@media (prefers-reduced-motion: reduce) { .hop { animation: none; } }
.dim { position: absolute; inset: 0; background: rgba(36, 44, 100, 0.22); pointer-events: none; }
</style>
