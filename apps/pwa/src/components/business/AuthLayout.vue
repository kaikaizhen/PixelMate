<script setup lang="ts">
import PetMascot from './PetMascot.vue'

defineProps<{ title: string; subtitle?: string }>()
</script>

<template>
  <main class="auth">
    <header class="brand">
      <PetMascot :scale="3" />
      <p class="logo" aria-hidden="true">PixelMate</p>
    </header>
    <section class="card" :aria-labelledby="'auth-title'">
      <h1 id="auth-title">{{ title }}</h1>
      <p v-if="subtitle" class="sub">{{ subtitle }}</p>
      <slot />
    </section>
    <footer class="links"><slot name="links" /></footer>
  </main>
</template>

<style scoped>
.auth {
  min-height: 100dvh;
  display: grid;
  align-content: center;
  justify-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-2);
}
.brand { display: grid; justify-items: center; gap: 0.25rem; }
.logo { font-family: var(--font-pixel); font-size: 0.8rem; letter-spacing: 0.05em; color: var(--c-ink); }
.card {
  width: min(100%, 26rem);
  display: grid;
  gap: var(--space-2);
  padding: var(--space-3);
  background: var(--c-paper);
  border: 3px solid var(--c-ink);
  border-radius: 14px;
  box-shadow: 6px 6px 0 var(--c-ink);
}
h1 { font-size: 1.4rem; line-height: 1.3; }
.sub { color: var(--c-ink-soft); font-size: 0.95rem; }
.links { display: grid; gap: 0.5rem; justify-items: center; font-size: 0.95rem; text-align: center; }
@media (min-width: 640px) {
  .card { padding: var(--space-4); }
  h1 { font-size: 1.6rem; }
}
@media (max-height: 640px) and (orientation: landscape) {
  .auth { align-content: start; }
}
</style>
