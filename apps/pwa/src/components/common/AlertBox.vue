<script setup lang="ts">
withDefaults(defineProps<{ tone?: 'error' | 'success' | 'info' }>(), { tone: 'info' })
</script>

<template>
  <div class="alert" :class="tone" :role="tone === 'error' ? 'alert' : 'status'">
    <slot />
  </div>
</template>

<style scoped>
.alert {
  padding: 0.75rem 1rem;
  border-radius: var(--radius);
  border: 2px solid currentColor;
  font-size: 0.95rem;
}
.error { background: var(--c-error-soft); color: var(--c-error); }
.success { background: var(--c-green-soft); color: var(--c-green); }
.info { background: var(--c-pink-soft); color: var(--c-ink); }
</style>
