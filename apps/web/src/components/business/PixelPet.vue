<script setup lang="ts">
import type { PetMood } from '@/composables/usePetReaction'
import petIdle from '@/assets/pixel/pet_idle.png'
import petFaces from '@/assets/pixel/pet_faces.png'

defineProps<{ mood: PetMood }>()
defineEmits<{ touch: [] }>()
</script>

<template>
  <button
    type="button"
    class="pet"
    :class="`pet--${mood}`"
    aria-label="摸摸小夥伴"
    @click="$emit('touch')"
  >
    <span
      class="pet__sprite pixelated"
      :style="{ backgroundImage: `url(${mood === 'idle' ? petIdle : petFaces})` }"
    />
  </button>
</template>

<style scoped>
.pet {
  display: block;
  padding: 0;
  border: 0;
  background: none;
  cursor: pointer;
  width: calc(48px * var(--s));
  height: calc(48px * var(--s));
  -webkit-tap-highlight-color: transparent;
}

.pet__sprite {
  display: block;
  width: 100%;
  height: 100%;
  background-repeat: no-repeat;
  background-size: calc(192px * var(--s)) 100%;
  animation: idle 0.9s steps(4) infinite;
}

.pet--blink .pet__sprite,
.pet--happy .pet__sprite {
  background-size: calc(96px * var(--s)) 100%;
  animation: none;
}

.pet--blink .pet__sprite {
  background-position-x: 0;
}

.pet--happy .pet__sprite {
  background-position-x: calc(-48px * var(--s));
}

.pet--happy {
  animation: hop 0.6s steps(6) 2;
}

@keyframes idle {
  to {
    background-position-x: calc(-192px * var(--s));
  }
}

@keyframes hop {
  50% {
    transform: translateY(calc(-8px * var(--s)));
  }
}
</style>
