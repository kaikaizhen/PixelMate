<script setup lang="ts">
import { PET_LOOKS } from '@/constants/petLooks'
import PixelPet from './PixelPet.vue'

defineProps<{ modelValue: string; error?: string }>()
defineEmits<{ 'update:modelValue': [string] }>()
</script>

<template>
  <fieldset class="looks" :aria-invalid="!!error">
    <legend>選一個外觀</legend>
    <div class="grid">
      <label v-for="l in PET_LOOKS" :key="l.id" class="look" :class="{ on: modelValue === l.id }">
        <input type="radio" name="look" class="sr-only" :value="l.id" :checked="modelValue === l.id" @change="$emit('update:modelValue', l.id)" />
        <PixelPet :color="l.color" :scale="2" decorative />
        <span>{{ l.label }}</span>
      </label>
    </div>
    <p v-if="error" class="err">{{ error }}</p>
  </fieldset>
</template>

<style scoped>
.looks { margin: 0; padding: 0; border: 0; min-width: 0; }
legend { padding: 0; margin-bottom: 0.4rem; font-weight: 700; font-size: 0.95rem; }
.grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 0.75rem; }
.look {
  display: grid;
  justify-items: center;
  gap: 0.25rem;
  padding: 0.75rem 0.5rem;
  background: var(--c-paper);
  border: 3px solid var(--c-line);
  border-radius: var(--radius);
  cursor: pointer;
  min-height: var(--tap-min);
}
.look.on { border-color: var(--c-ink); background: var(--c-pink-soft); box-shadow: 3px 3px 0 var(--c-ink); }
.look:has(input:focus-visible) { outline: 3px solid var(--c-ink); outline-offset: 3px; }
.err { margin-top: 0.35rem; color: var(--c-error); font-size: 0.9rem; }
</style>
