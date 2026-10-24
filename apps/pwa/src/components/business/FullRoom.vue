<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import RoomScene from './RoomScene.vue'
import { weatherKey, windowWeatherKey } from '@/utils/roomWeather'
import { useEdgeColors } from '@/composables/useEdgeColors'
import { useGameStore } from '@/stores/useGameStore'
import wall from '@/assets/pixel/room_wall_tile.png'
import base from '@/assets/pixel/room_base_tile.png'
import floor from '@/assets/pixel/room_floor_tile.png'

// The house at full screen: the 240x240 room is scaled to the short side of the screen and its wall, baseboard and
// floor are continued with seamless tiles cut from the same art, so the screen is one room edge to edge.
const props = defineProps<{ color: string; happy: boolean; name: string; weather?: string; equipment?: Record<string, string | null>; activity?: string; scene?: string }>()
defineEmits<{ pat: [] }>()

const w = ref(typeof window === 'undefined' ? 393 : window.innerWidth)
const h = ref(typeof window === 'undefined' ? 852 : window.innerHeight)
const update = () => ((w.value = window.innerWidth), (h.value = window.innerHeight))
onMounted(() => window.addEventListener('resize', update))
onUnmounted(() => window.removeEventListener('resize', update))

const s = computed(() => Math.min(w.value, h.value) / 240)
const portrait = computed(() => h.value > w.value)
const left = computed(() => (w.value - 240 * s.value) / 2)
const top = computed(() => (portrait.value ? (h.value - 240 * s.value) * 0.05 : 0)) // phones: a little wall above, lots of floor below for the tool bar and the panels
const game = useGameStore()
const outdoors = computed(() => (props.scene === 'balcony' || props.scene === 'yard' ? props.scene : null))
const outdoorUrl = computed(() => (outdoors.value ? game.url(weatherKey(new Date(), props.weather, outdoors.value)) : ''))
const edge = useEdgeColors(outdoorUrl)
const night = computed(() => windowWeatherKey(new Date(), props.weather).includes('starry'))
const vars = computed(() => ({ '--s': s.value, '--l': `${left.value}px`, '--t': `${top.value}px` }))
</script>

<template>
  <div class="full" :style="vars">
    <!-- the house and the outdoors cross-fade when the pet moves between them -->
    <Transition name="scene" mode="out-in">
      <div :key="outdoors ?? 'home'" class="stage" :style="outdoors ? { background: `linear-gradient(${edge.top.value || '#cfe3f5'} 0 50%, ${edge.bottom.value || '#9fcf8f'} 50% 100%)` } : undefined">
        <template v-if="!outdoors">
          <div class="wall pixelated" :style="{ backgroundImage: `url(${wall})` }" />
          <div class="base pixelated" :style="{ backgroundImage: `url(${base})` }" />
          <div class="floor pixelated" :style="{ backgroundImage: `url(${floor})` }" />
        </template>
        <RoomScene class="scene-box" bare :scale="s" :color="color" :happy="happy" :name="name" :weather="weather" :equipment="equipment" :activity="activity" :scene="scene" @pat="$emit('pat')" />
        <span v-if="night && !outdoors" class="dim" aria-hidden="true" />
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.full { position: fixed; inset: 0; overflow: hidden; background: #f6d9c4; }
.wall, .base, .floor { position: absolute; left: 0; right: 0; background-repeat: repeat; }
.wall { top: 0; height: calc(var(--t) + 144 * var(--s) * 1px); background-size: calc(16 * var(--s) * 1px) calc(16 * var(--s) * 1px); background-position: var(--l) 0; }
.base { top: calc(var(--t) + 144 * var(--s) * 1px); height: calc(7 * var(--s) * 1px); background-size: calc(16 * var(--s) * 1px) 100%; background-repeat: repeat-x; background-position: var(--l) 0; }
.floor {
  top: calc(var(--t) + 151 * var(--s) * 1px);
  bottom: 0;
  background-size: calc(40 * var(--s) * 1px) calc(24 * var(--s) * 1px);
  background-position: var(--l) calc(8 * var(--s) * 1px);
}
.stage { position: absolute; inset: 0; }
.scene-enter-active, .scene-leave-active { transition: opacity 0.35s; }
.scene-enter-from, .scene-leave-to { opacity: 0; }
@media (prefers-reduced-motion: reduce) { .scene-enter-active, .scene-leave-active { transition: none; } }
.dim { position: absolute; inset: 0; background: rgba(36, 44, 100, 0.22); pointer-events: none; }
.scene-box { position: absolute; left: var(--l); top: var(--t); width: calc(240 * var(--s) * 1px); }
</style>
