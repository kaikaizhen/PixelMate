<script setup lang="ts">
import { computed } from 'vue'
import { useGameStore } from '@/stores/useGameStore'

// The pet drawn from the game manifest (pet.star_01.*). Sizes follow the parent's --s (pixels per logical pixel).
const props = defineProps<{ color: string; face: 'normal' | 'happy' }>()
const game = useGameStore()
const body = computed(() => game.url(`pet.star_01.body.${props.color}.idle`) || game.url('pet.star_01.body.cream.idle'))
const face = computed(() => game.url(`pet.star_01.face.${props.face}`))
</script>

<template>
  <div v-if="body" class="gp">
    <span class="body pixelated" :style="{ backgroundImage: `url(${body})` }" />
    <img draggable="false" v-if="face" class="face pixelated" :src="face" alt="" />
  </div>
</template>

<style scoped>
.gp { position: relative; width: calc(48px * var(--s)); height: calc(48px * var(--s)); }
.body { position: absolute; inset: 0; background-size: calc(192px * var(--s)) 100%; animation: idle 1s steps(1) infinite; }
.face { position: absolute; inset: 0; width: 100%; height: 100%; }
@keyframes idle {
  0% { background-position: 0 0; }
  25% { background-position: calc(-48px * var(--s)) 0; }
  50% { background-position: calc(-96px * var(--s)) 0; }
  75% { background-position: calc(-144px * var(--s)) 0; }
}
</style>
