<script setup lang="ts">
import PetMascot from './PetMascot.vue'

defineProps<{ title: string; subtitle?: string }>()

// phones get a small pet; wide screens get a big one beside the card
const mascotScale = typeof window !== 'undefined' && window.matchMedia?.('(min-width: 860px)').matches ? 5 : 2
</script>

<template>
  <main class="auth game-screen">
    <header class="brand">
      <PetMascot :scale="mascotScale" />
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
  gap: 0.75rem;
  padding: 1rem;
}
.brand { display: grid; justify-items: center; gap: 0.15rem; }
.logo { font-family: var(--font-pixel); font-size: 0.7rem; letter-spacing: 0.05em; color: var(--c-ink); }
.card {
  width: min(100%, 26rem);
  display: grid;
  gap: 0.85rem;
  padding: 1.25rem;
  background: var(--c-paper);
  border: 3px solid var(--c-ink);
  border-radius: 14px;
  box-shadow: 6px 6px 0 var(--c-ink);
}
h1 { font-size: 1.3rem; line-height: 1.3; }
.sub { color: var(--c-ink-soft); font-size: 0.9rem; line-height: 1.5; }
.links { display: grid; gap: 0.25rem; justify-items: center; font-size: 0.95rem; text-align: center; }

/* wide screens: the pet sits next to the card, so nothing needs to scroll */
@media (min-width: 860px) {
  .auth { grid-template-columns: auto minmax(0, 26rem); grid-template-areas: 'brand card' 'brand links'; align-content: center; justify-content: center; column-gap: 4rem; }
  .brand { grid-area: brand; align-self: center; }
  .card { grid-area: card; }
  .links { grid-area: links; }
  h1 { font-size: 1.5rem; }
}
/* phones (iPhone 16: 393x852) and short windows: drop the subtitle so a form with every error still fits without scrolling */
@media (max-height: 900px) and (max-width: 859px), (max-height: 760px) { .sub { display: none; } }
</style>
