<script setup lang="ts">
import { ref } from 'vue'
import FormField from './FormField.vue'

defineProps<{ modelValue: string; label: string; autocomplete: 'current-password' | 'new-password'; error?: string; hint?: string }>()
defineEmits<{ 'update:modelValue': [string] }>()

const shown = ref(false)
</script>

<template>
  <FormField
    :model-value="modelValue"
    :label="label"
    :type="shown ? 'text' : 'password'"
    :autocomplete="autocomplete"
    :error="error"
    :hint="hint"
    :maxlength="128"
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <template #suffix>
      <button type="button" class="toggle" :aria-pressed="shown" @click="shown = !shown">
        {{ shown ? '隱藏' : '顯示' }}
      </button>
    </template>
    <slot />
  </FormField>
</template>

<style scoped>
.toggle {
  position: absolute;
  inset: 4px 4px 4px auto;
  min-width: 56px;
  padding: 0 0.6rem;
  font: inherit;
  font-size: 0.85rem;
  color: var(--c-ink);
  background: var(--c-pink-soft);
  border: 2px solid var(--c-ink);
  border-radius: 6px;
  cursor: pointer;
}
</style>
