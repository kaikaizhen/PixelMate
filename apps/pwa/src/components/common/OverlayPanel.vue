<script setup lang="ts">
import { nextTick, onUnmounted, ref, watch } from 'vue'

// A floating card on top of the room. No dim mask, no blocking: the room stays visible and tappable behind it.
// Phones: above the tool bar. Wide screens: beside the tool column. Esc or ✕ closes; focus moves in and back out.
const props = defineProps<{ open: boolean; title: string; subtitle?: string }>()
const emit = defineEmits<{ close: [] }>()

const card = ref<HTMLElement | null>(null)
let opener: HTMLElement | null = null

function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape') emit('close')
}

watch(
  () => props.open,
  async (open) => {
    if (open) {
      opener = document.activeElement as HTMLElement | null
      window.addEventListener('keydown', onKey)
      await nextTick()
      card.value?.focus()
    } else {
      window.removeEventListener('keydown', onKey)
      opener?.focus?.()
    }
  },
)
onUnmounted(() => window.removeEventListener('keydown', onKey))
</script>

<template>
  <section v-if="open" ref="card" class="panel" role="dialog" aria-modal="false" :aria-label="title" tabindex="-1">
    <header>
      <div class="ttl"><h2>{{ title }}</h2><p v-if="subtitle">{{ subtitle }}</p></div>
      <button type="button" class="x" aria-label="關閉" @click="emit('close')">✕</button>
    </header>
    <slot />
  </section>
</template>

<style scoped>
.panel {
  position: absolute;
  z-index: 5;
  left: 0.75rem;
  right: 0.75rem;
  bottom: calc(0.75rem + var(--bar-h, 5.4rem) + 0.6rem + env(safe-area-inset-bottom)); /* sits right above the tool bar */
  max-width: 28rem;
  max-height: 47dvh;
  margin-inline: auto;
  overflow: auto;
  padding: 0.7rem 1rem 0.9rem;
  color: var(--c-ink);
  background: var(--c-paper);
  border: 3px solid var(--c-ink);
  border-radius: 14px;
  box-shadow: 5px 5px 0 var(--c-ink);
  animation: rise 0.18s steps(4);
}
.panel:focus-visible { outline: 3px solid var(--c-ink); outline-offset: 3px; }
header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.5rem; }
h2 { font-size: 1.15rem; line-height: 1.2; }
.ttl p { margin: 0; color: var(--c-ink-soft); font-size: 0.85rem; }
.x {
  min-width: var(--tap-min);
  min-height: var(--tap-min);
  font: inherit;
  font-size: 1.1rem;
  color: var(--c-ink);
  background: var(--c-pink-soft);
  border: 2px solid var(--c-ink);
  border-radius: 8px;
  cursor: pointer;
}
@keyframes rise { from { transform: translateY(24px); opacity: 0; } }
@media (min-width: 860px) {
  .panel { inset: 50% 10.5rem auto auto; transform: translateY(-50%); width: 24rem; max-width: none; margin: 0; max-height: 80dvh; animation: none; }
}
</style>
