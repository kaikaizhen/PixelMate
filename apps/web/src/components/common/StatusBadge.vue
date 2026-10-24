<script setup lang="ts"></script>

<template>
  <span class="status-badge"><span class="status-badge__dot" aria-hidden="true" /><slot /></span>
</template>

<style scoped>
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.35rem 0.9rem;
  background: var(--c-yellow);
  border: 3px solid var(--c-ink);
  box-shadow: 4px 4px 0 var(--c-ink);
  font-weight: 700;
  font-size: 0.95rem;
}

.status-badge__dot {
  width: 10px;
  height: 10px;
  background: var(--c-ink);
  animation: pulse 1.6s steps(2) infinite;
}

@keyframes pulse {
  50% {
    opacity: 0.2;
  }
}
</style>
