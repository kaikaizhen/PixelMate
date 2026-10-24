<script setup lang="ts">
import { computed } from 'vue'
import bodyCream from '@/assets/pixel/body_cream_idle.png'
import bodyMint from '@/assets/pixel/body_mint_idle.png'
import faceBlink from '@/assets/pixel/face_blink.png'
import faceHappy from '@/assets/pixel/face_happy.png'
import faceNormal from '@/assets/pixel/face_normal.png'
import faceSleepy from '@/assets/pixel/face_sleepy.png'
import type { PetColor } from '@/constants/petLooks'

const props = withDefaults(defineProps<{ color?: PetColor; face?: 'normal' | 'happy' | 'sleepy' | 'blink'; scale?: number; label?: string; decorative?: boolean }>(), {
  color: 'cream',
  face: 'normal',
  scale: 3,
  label: '小伴星的像素小夥伴',
  decorative: false,
})

const body = computed(() => (props.color === 'mint' ? bodyMint : bodyCream))
const face = computed(() => ({ normal: faceNormal, happy: faceHappy, sleepy: faceSleepy, blink: faceBlink })[props.face])
</script>

<template>
  <div class="px-pet" :style="{ '--s': scale }" :role="decorative ? undefined : 'img'" :aria-label="decorative ? undefined : label" :aria-hidden="decorative || undefined">
    <div class="body pixelated" :style="{ backgroundImage: `url(${body})` }" />
    <img class="face pixelated" :src="face" alt="" width="48" height="48" />
  </div>
</template>

<style scoped>
.px-pet {
  position: relative;
  width: calc(48px * var(--s));
  height: calc(48px * var(--s));
  flex: none;
}
.body {
  position: absolute;
  inset: 0;
  background-size: calc(192px * var(--s)) calc(48px * var(--s));
  animation: idle 1s steps(1) infinite;
}
.face {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
}
@keyframes idle {
  0% { background-position: 0 0; }
  25% { background-position: calc(-48px * var(--s)) 0; }
  50% { background-position: calc(-96px * var(--s)) 0; }
  75% { background-position: calc(-144px * var(--s)) 0; }
}
</style>
