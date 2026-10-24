<script setup lang="ts">
import { computed } from 'vue'
import { useGameStore } from '@/stores/useGameStore'

// The pet drawn from the game manifest (pet.star_01.*) with whatever it wears. Sizes follow the parent's --s.
// Layer order (docs/素材.md): back parts (costume tail, back items) -> body -> face -> worn items by z.
const props = defineProps<{ color: string; face: 'normal' | 'happy'; equipment?: Record<string, string | null> }>()
const game = useGameStore()

const body = computed(() => game.url(`pet.star_01.body.${props.color}.idle`) || game.url('pet.star_01.body.cream.idle'))
const face = computed(() => game.url(`pet.star_01.face.${props.face}`))

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
  <div v-if="body" class="gp">
    <img v-for="p in behind" :key="p" draggable="false" class="layer bob pixelated" :src="src(p)" alt="" />
    <span class="body pixelated" :style="{ backgroundImage: `url(${body})` }" />
    <img v-if="face" draggable="false" class="layer bob face pixelated" :src="face" alt="" />
    <img v-for="w in front" :key="w.key" draggable="false" class="layer bob pixelated" :src="src(w.asset.path)" alt="" :data-item="w.key" />
  </div>
</template>

<style scoped>
.gp { position: relative; width: calc(48px * var(--s)); height: calc(48px * var(--s)); }
.body, .layer { position: absolute; inset: 0; }
.layer { width: 100%; height: 100%; }
.body { background-size: calc(192px * var(--s)) 100%; animation: idle 1s steps(1) infinite; }
/* the manifest's idle animation moves face and gear down 1 logical pixel on its last two frames (attach [0,1]) */
.bob { animation: bob 1s steps(1) infinite; }
@keyframes idle {
  0% { background-position: 0 0; }
  25% { background-position: calc(-48px * var(--s)) 0; }
  50% { background-position: calc(-96px * var(--s)) 0; }
  75% { background-position: calc(-144px * var(--s)) 0; }
}
@keyframes bob { 0%, 49% { transform: translateY(0); } 50%, 100% { transform: translateY(calc(1px * var(--s))); } }
</style>
