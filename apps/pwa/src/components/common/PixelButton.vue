<script setup lang="ts">
withDefaults(defineProps<{ loading?: boolean; disabled?: boolean; variant?: 'primary' | 'ghost'; type?: 'button' | 'submit' }>(), {
  loading: false,
  disabled: false,
  variant: 'primary',
  type: 'button',
})
</script>

<template>
  <button class="btn" :class="variant" :type="type" :disabled="loading || disabled" :aria-busy="loading">
    <span v-if="loading" class="spin" aria-hidden="true" />
    <slot />
  </button>
</template>

<style scoped>
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  min-height: var(--tap-min);
  width: 100%;
  padding: 0 1.25rem;
  font: inherit;
  font-weight: 700;
  color: #4a3040;
  border: 3px solid var(--c-ink);
  border-radius: var(--radius);
  box-shadow: 4px 4px 0 var(--c-ink);
  cursor: pointer;
  transition: transform 0.08s steps(2), box-shadow 0.08s steps(2);
}
.primary { background: var(--c-yellow); }
.ghost { background: var(--c-paper); color: var(--c-ink); }
.btn:active:not(:disabled) { transform: translate(3px, 3px); box-shadow: 1px 1px 0 var(--c-ink); }
.btn:disabled { opacity: 0.65; cursor: progress; }
.spin {
  width: 1rem;
  height: 1rem;
  border: 3px solid currentColor;
  border-right-color: transparent;
  border-radius: 50%;
  animation: spin 0.8s steps(8) infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
</style>
