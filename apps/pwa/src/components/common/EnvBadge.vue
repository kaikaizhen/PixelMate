<script setup lang="ts">
// A test server says so on every screen (set at build time: VITE_ENV_LABEL="測試服（會刪檔）"). Nothing is shown on a real release.
const label = (import.meta.env.VITE_ENV_LABEL as string | undefined)?.trim()
</script>

<template>
  <p v-if="label" class="env" role="note" data-testid="env-badge">{{ label }}</p>
</template>

<style scoped>
.env {
  position: fixed;
  top: calc(env(safe-area-inset-top) + 2px);
  left: 50%;
  z-index: 60;
  margin: 0;
  padding: 1px 8px;
  transform: translateX(-50%);
  font-size: 0.65rem;
  font-weight: 700;
  line-height: 1.4;
  white-space: nowrap;
  color: var(--c-on-yellow);
  background: var(--c-yellow);
  border: 2px solid var(--c-on-yellow);
  border-radius: 8px;
  pointer-events: none;           /* never in the way of a tap */
}
</style>
