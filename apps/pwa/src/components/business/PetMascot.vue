<script setup lang="ts">
import body from '@/assets/pixel/body_cream_idle.png'
import face from '@/assets/pixel/face_normal.png'

withDefaults(defineProps<{ scale?: number }>(), { scale: 3 })
</script>

<template>
  <div class="mascot" :style="{ '--s': scale }" role="img" aria-label="小伴星的像素小夥伴">
    <div class="body pixelated" :style="{ backgroundImage: `url(${body})` }" />
    <img class="face pixelated" :src="face" alt="" width="48" height="48" />
  </div>
</template>

<style scoped>
.mascot {
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
/* 4 frames, one per quarter second */
@keyframes idle {
  0% { background-position: 0 0; }
  25% { background-position: calc(-48px * var(--s)) 0; }
  50% { background-position: calc(-96px * var(--s)) 0; }
  75% { background-position: calc(-144px * var(--s)) 0; }
}
</style>
