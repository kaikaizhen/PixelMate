<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import SpriteStrip from '@/components/common/SpriteStrip.vue'
import GamePet from './GamePet.vue'
import { OUTDOOR_SPOT, PET_SPOT, ROOM_SIZE, WINDOW, WINDOW_VIEW } from '@/constants/defaultRoom'
import { useSceneScale } from '@/composables/useSceneScale'
import { useGameStore } from '@/stores/useGameStore'
import { weatherKey, windowWeatherKey } from '@/utils/roomWeather'
import room from '@/assets/pixel/room.png'
import plant from '@/assets/pixel/plant.png'
import curtainL from '@/assets/pixel/curtain_l.png'
import curtainR from '@/assets/pixel/curtain_r.png'
import bars from '@/assets/pixel/window_bars.png'

// The house: the website's room art + the weather outside (game assets by assetKey) + furniture + the pet.
const props = defineProps<{ color: string; happy: boolean; name: string; weather?: string; bare?: boolean; scale?: number; equipment?: Record<string, string | null>; activity?: string; scene?: string
  placements?: { placementId: string; itemId: string; x: number; y: number }[]; floor?: string; editing?: boolean; selected?: number }>()
const emit = defineEmits<{ pat: []; select: [index: number]; move: [index: number, x: number, y: number] }>()

const el = ref<HTMLElement | null>(null)
const measured = useSceneScale(el)
const s = computed(() => props.scale ?? measured.value)   // fullscreen passes its own scale
const game = useGameStore()
onMounted(() => void game.load())

const weatherUrl = computed(() => game.url(windowWeatherKey(new Date(), props.weather)))
const u = (n: number) => `calc(${n} * var(--s) * 1px)`
const floorUrl = computed(() => (props.floor ? game.url(props.floor) : ''))
const size = (key: string) => ({ w: game.assets[key]?.frameWidth ?? 48, h: game.assets[key]?.frameHeight ?? 48 })

// Edit mode: drag a piece around on the 240x240 canvas (pixel-aligned), tap it to select it.
let grab: { id: number; px: number; py: number; x: number; y: number; moved: boolean } | null = null
function down(e: PointerEvent, i: number) {
  if (!props.editing) return
  const p = props.placements![i]
  grab = { id: e.pointerId, px: e.clientX, py: e.clientY, x: p.x, y: p.y, moved: false }
  ;(e.target as HTMLElement).setPointerCapture(e.pointerId)
  emit('select', i)
}
function drag(e: PointerEvent, i: number) {
  if (!grab || grab.id !== e.pointerId || !s.value) return
  const dx = (e.clientX - grab.px) / s.value, dy = (e.clientY - grab.py) / s.value
  if (!grab.moved && Math.hypot(dx, dy) < 1) return
  grab.moved = true
  const { w, h } = size(props.placements![i].itemId)
  const clamp = (v: number, max: number) => Math.min(Math.max(Math.round(v), 0), Math.round(max))
  emit('move', i, clamp(grab.x + dx, ROOM_SIZE - w / 2), clamp(grab.y + dy, ROOM_SIZE - h / 2))
}
const up = () => { grab = null }
const noteUrl = computed(() => game.url('fx.note_drift'))
const outdoors = computed(() => (props.scene === 'balcony' || props.scene === 'yard' ? props.scene : null))
const outdoorUrl = computed(() => (outdoors.value ? game.url(weatherKey(new Date(), props.weather, outdoors.value)) : ''))
const spot = computed(() => (outdoors.value ? OUTDOOR_SPOT[outdoors.value] : PET_SPOT))
const night = computed(() => windowWeatherKey(new Date(), props.weather).includes('starry'))
</script>

<template>
  <div ref="el" class="scene" :class="{ bare }" :style="{ '--s': s, aspectRatio: `${ROOM_SIZE} / ${ROOM_SIZE}` }" role="group" aria-label="你的小屋">
    <!-- away from home: the balcony or the yard is the whole picture -->
    <img v-if="outdoors && outdoorUrl" draggable="false" class="layer pixelated" data-scene="outdoor" :data-view="outdoors" :src="outdoorUrl" alt="" />
    <template v-else>
    <!-- 1. outside the window -->
    <div class="win pixelated" :style="{ left: u(WINDOW.x), top: u(WINDOW.y), width: u(WINDOW.w), height: u(WINDOW.h),
        backgroundImage: weatherUrl ? `url(${weatherUrl})` : undefined,
        backgroundSize: `${u(ROOM_SIZE * WINDOW_VIEW.scale)} ${u(ROOM_SIZE * WINDOW_VIEW.scale)}`, backgroundPosition: `${u(-WINDOW_VIEW.x)} ${u(-WINDOW_VIEW.y)}` }" />
    <!-- 2. the house -->
    <img draggable="false" class="layer pixelated" :src="room" alt="" />
      <!-- a floor tile the player chose covers the whole floor of the house art (rug included) -->
      <div v-if="floorUrl" class="abs pixelated floor-ov" data-floor :style="{ left: 0, top: u(151), width: u(240), height: u(89), backgroundImage: `url(${floorUrl})`, backgroundSize: `${u(48)} ${u(48)}` }" />
    <img draggable="false" class="abs pixelated" :src="bars" alt="" :style="{ left: u(WINDOW.x), top: u(WINDOW.y), width: u(WINDOW.w), height: u(WINDOW.h) }" />
    <SpriteStrip :src="plant" :frames="4" :width="44" :height="80" :x="15" :y="64" duration="2.4s" />
    <SpriteStrip :src="curtainL" :frames="4" :width="14" :height="84" :x="122" :y="24" duration="3.2s" />
    <SpriteStrip :src="curtainR" :frames="4" :width="14" :height="84" :x="208" :y="24" duration="3.2s" delay="-0.4s" />
    <!-- 3. furniture, by assetKey: stacked by how far down they stand, the pet among them -->
    <template v-for="(p, i) in placements ?? []" :key="p.placementId">
      <img v-if="game.url(p.itemId)" draggable="false" class="abs pixelated item" :class="{ edit: editing, sel: editing && selected === i }" :src="game.url(p.itemId)" alt="" :data-placed="p.itemId"
        :style="{ left: u(p.x), top: u(p.y), width: u(size(p.itemId).w), height: u(size(p.itemId).h), zIndex: Math.round(p.y + size(p.itemId).h) }"
        @pointerdown="down($event, i)" @pointermove="drag($event, i)" @pointerup="up" @pointercancel="up" />
    </template>
    </template>
    <!-- 4. the pet -->
    <button type="button" class="pet" :class="[{ hop: happy }, activity && `act-${activity}`]" :style="{ left: u(spot.x), top: u(spot.y), zIndex: Math.round(spot.y + 46) }" :disabled="editing" :aria-label="`摸摸 ${name}`" @click="$emit('pat')">
      <GamePet :color="color" :face="happy ? 'happy' : 'normal'" :equipment="equipment" :activity="activity" :weather="weather" :scene="scene" />
      <span v-if="activity === 'hum' && noteUrl" class="fx pixelated" data-fx="note" :style="{ backgroundImage: `url(${noteUrl})` }" aria-hidden="true" />
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
.pet { position: absolute; z-index: 1; padding: 0; background: none; border: 0; cursor: pointer; border-radius: 8px; }
/* walking: paces a little to the left and right of its spot; humming: notes drift above its head */
.act-walk { animation: pace 8s steps(16) infinite; }
@keyframes pace { 0%, 100% { transform: translateX(calc(-14 * var(--s) * 1px)); } 50% { transform: translateX(calc(14 * var(--s) * 1px)); } }
.fx { position: absolute; left: 30%; top: -45%; width: calc(48px * var(--s)); height: calc(48px * var(--s)); background-size: calc(192px * var(--s)) 100%; animation: note 0.667s steps(1) infinite; pointer-events: none; }
@keyframes note { 0% { background-position: 0 0; } 25% { background-position: calc(-48px * var(--s)) 0; } 50% { background-position: calc(-96px * var(--s)) 0; } 75% { background-position: calc(-144px * var(--s)) 0; } }
.item.edit { cursor: grab; touch-action: none; pointer-events: auto; }   /* base.css turns pointer events off for every image; only pieces being edited take them back */
.item.sel { filter: drop-shadow(0 0 0 #fff) drop-shadow(calc(1px * var(--s)) 0 0 #ff6fa5) drop-shadow(calc(-1px * var(--s)) 0 0 #ff6fa5) drop-shadow(0 calc(1px * var(--s)) 0 #ff6fa5) drop-shadow(0 calc(-1px * var(--s)) 0 #ff6fa5); }
.floor-ov { background-repeat: repeat; }
.pet:disabled { cursor: default; pointer-events: none; }
.hop { animation: hop 0.5s steps(4) 1; }
@keyframes hop { 40% { transform: translateY(calc(-10 * var(--s) * 1px)); } }
@media (prefers-reduced-motion: reduce) { .hop, .act-walk, .fx { animation: none; } }
.dim { position: absolute; inset: 0; background: rgba(36, 44, 100, 0.22); pointer-events: none; }
</style>
