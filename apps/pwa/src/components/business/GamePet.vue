<script setup lang="ts">
import { computed } from 'vue'
import { useGameStore } from '@/stores/useGameStore'

// The pet drawn from the game manifest (pet.star_01.*) with whatever it wears. Sizes follow the parent's --s.
// Layer order (docs/素材.md): back parts (costume tail, back items) -> body -> face -> worn items by z.
const props = defineProps<{ color: string; face: 'normal' | 'happy'; equipment?: Record<string, string | null>; activity?: string }>()
const game = useGameStore()

// Activities use the manifest animations: walk = the walk sheet, read = look_down face + the book prop opening and closing.
const walking = computed(() => props.activity === 'walk' && props.face === 'normal')
const reading = computed(() => props.activity === 'read' && props.face === 'normal')
const body = computed(() =>
  (walking.value && (game.url(`pet.star_01.body.${props.color}.walk`) || game.url('pet.star_01.body.cream.walk'))) ||
  game.url(`pet.star_01.body.${props.color}.idle`) || game.url('pet.star_01.body.cream.idle'))
const face = computed(() => game.url(`pet.star_01.face.${reading.value ? 'look_down' : props.face}`))
const book = computed(() => (reading.value ? game.url('pet.star_01.prop.book') : ''))

const worn = computed(() =>
  Object.values(props.equipment ?? {})
    .filter((k): k is string => !!k && !!game.assets[k])
    .map((k) => ({ key: k, asset: game.assets[k] })),
)
const behind = computed(() =>
  worn.value.flatMap(({ asset }) => [asset.layer === 'pet_back_accessory' ? asset.path : '', asset.backKey ? game.assets[asset.backKey]?.path : '']).filter(Boolean),
)
const front = computed(() =>
  worn.value.filter(({ asset }) => asset.layer !== 'pet_back_accessory').sort((a, b) => (a.asset.z ?? 0) - (b.asset.z ?? 0)),
)
const src = (path: string) => `${import.meta.env.BASE_URL}game/${path}`
</script>

<template>
  <div v-if="body" class="gp" :class="{ walking, reading }">
    <img v-for="p in behind" :key="p" draggable="false" class="layer bob pixelated" :src="src(p)" alt="" />
    <span class="body pixelated" :style="{ backgroundImage: `url(${body})` }" />
    <img v-if="face" draggable="false" class="layer bob face pixelated" :src="face" alt="" />
    <img v-for="w in front" :key="w.key" draggable="false" class="layer bob pixelated" :src="src(w.asset.path)" alt="" :data-item="w.key" />
    <span v-if="book" class="layer book pixelated" data-prop="book" :style="{ backgroundImage: `url(${book})` }" />
  </div>
</template>

<style scoped>
.gp { position: relative; width: calc(48px * var(--s)); height: calc(48px * var(--s)); }
.body, .layer { position: absolute; inset: 0; }
.layer { width: 100%; height: 100%; }
.body { background-size: calc(192px * var(--s)) 100%; animation: idle 1s steps(1) infinite; }
/* the manifest's idle animation moves face and gear down 1 logical pixel on its last two frames (attach [0,1]) */
/* walk: 4 frames at 6 fps, the head bobs on frames 1 and 3; read: the idle frames 0,0,1,0 at 3 fps with the book opening on the third */
.walking .body { animation: walk 0.667s steps(1) infinite; }
.walking .bob { animation: bobwalk 0.667s steps(1) infinite; }
.reading .body { animation: readbody 1.333s steps(1) infinite; }
.reading .bob { animation: none; }
.book { background-size: calc(96px * var(--s)) 100%; background-repeat: no-repeat; animation: readbook 1.333s steps(1) infinite; }
.bob { animation: bob 1s steps(1) infinite; }
@keyframes idle {
  0% { background-position: 0 0; }
  25% { background-position: calc(-48px * var(--s)) 0; }
  50% { background-position: calc(-96px * var(--s)) 0; }
  75% { background-position: calc(-144px * var(--s)) 0; }
}
@keyframes walk {
  0% { background-position: 0 0; }
  25% { background-position: calc(-48px * var(--s)) 0; }
  50% { background-position: calc(-96px * var(--s)) 0; }
  75% { background-position: calc(-144px * var(--s)) 0; }
}
@keyframes bobwalk { 0%, 24% { transform: translateY(0); } 25%, 49% { transform: translateY(calc(1px * var(--s))); } 50%, 74% { transform: translateY(0); } 75%, 100% { transform: translateY(calc(1px * var(--s))); } }
@keyframes readbody { 0%, 49% { background-position: 0 0; } 50%, 74% { background-position: calc(-48px * var(--s)) 0; } 75%, 100% { background-position: 0 0; } }
@keyframes readbook { 0%, 49% { background-position: 0 0; } 50%, 74% { background-position: calc(-48px * var(--s)) 0; } 75%, 100% { background-position: 0 0; } }
@media (prefers-reduced-motion: reduce) { .walking .body, .walking .bob, .reading .body, .book { animation: none; } }
@keyframes bob { 0%, 49% { transform: translateY(0); } 50%, 100% { transform: translateY(calc(1px * var(--s))); } }
</style>
