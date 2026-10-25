<script setup lang="ts">
import { computed, useId } from 'vue'

const props = withDefaults(
  defineProps<{
    modelValue: string
    label: string
    type?: string
    autocomplete?: string
    error?: string
    hint?: string
    inputmode?: 'text' | 'email' | 'numeric'
    maxlength?: number
    required?: boolean
  }>(),
  { type: 'text', error: '', hint: '', inputmode: 'text', required: true },
)
defineEmits<{ 'update:modelValue': [string] }>()

const id = useId()
const describedBy = computed(() => [props.error && `${id}-err`, props.hint && `${id}-hint`].filter(Boolean).join(' ') || undefined)
</script>

<template>
  <div class="field" :class="{ invalid: !!error }">
    <label :for="id">{{ label }}</label>
    <div class="control">
      <input
        :id="id"
        :value="modelValue"
        :type="type"
        :autocomplete="autocomplete"
        :inputmode="inputmode"
        :maxlength="maxlength"
        :required="required"
        :aria-invalid="!!error"
        :aria-describedby="describedBy"
        autocapitalize="off"
        spellcheck="false"
        @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
      />
      <slot name="suffix" />
    </div>
    <p v-if="hint && !error" :id="`${id}-hint`" class="hint">{{ hint }}</p>
    <p v-if="error" :id="`${id}-err`" class="err">{{ error }}</p>
    <slot />
  </div>
</template>

<style scoped>
.field { display: grid; gap: 0.25rem; }
label { font-weight: 700; font-size: 0.95rem; }
.control { position: relative; display: flex; }
input {
  width: 100%;
  min-height: var(--tap-min);
  padding: 0 0.9rem;
  font: inherit;
  font-size: 1rem; /* 16px+ stops iOS zooming into the field */
  color: var(--c-ink);
  background: var(--c-paper);
  border: 3px solid var(--c-ink);
  border-radius: var(--radius);
}
.invalid input { border-color: var(--c-error); }
input:focus-visible { outline-offset: 2px; }
.hint { color: var(--c-ink-soft); font-size: 0.85rem; }
.err { color: var(--c-error); font-size: 0.9rem; }
</style>
