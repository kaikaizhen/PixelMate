<script setup lang="ts">
import { computed } from 'vue'
import MailScene from '@/components/business/MailScene.vue'
import LetterDialog from '@/components/business/LetterDialog.vue'
import { useMailDelivery } from '@/composables/useMailDelivery'

const { phase, isPostmanLeaving, startDelivery, openLetter, closeLetter, reset } = useMailDelivery()

const hint = computed(() => {
  switch (phase.value) {
    case 'idle':
      return '點點看紅色的信箱。'
    case 'delivering':
      return '小郵差正從路的那頭跑來⋯⋯'
    case 'arrived':
      return '信到了！點一下信封打開它。'
    case 'reading':
      return '牠正陪你一起看信。'
    default:
      return '信已經收好了。想再收一封也可以。'
  }
})
</script>

<template>
  <section id="mail" class="mail" aria-labelledby="mail-title">
    <div class="page-width mail__inner">
      <div class="mail__copy">
        <h2 id="mail-title" class="mail__title">遠方的心意，也能被接住</h2>
        <p class="mail__lead">遠方的那個人，送來了一封小小的信。</p>
        <p class="mail__hint" aria-live="polite">{{ hint }}</p>
        <button v-if="phase === 'done'" type="button" class="mail__again" @click="reset">再收一封</button>
      </div>
      <div class="mail__scene">
        <MailScene :phase="phase" :is-postman-leaving="isPostmanLeaving" @ring="startDelivery" @open="openLetter" />
      </div>
    </div>
    <LetterDialog :open="phase === 'reading'" @close="closeLetter" />
  </section>
</template>

<style scoped>
.mail {
  padding-block: var(--space-5);
}

.mail__inner {
  display: grid;
  gap: var(--space-4);
  align-items: center;
}

.mail__copy {
  display: grid;
  gap: var(--space-2);
  justify-items: start;
}

.mail__title {
  text-wrap: balance;
  margin: 0;
  font-size: clamp(1.7rem, 5vw, 2.4rem);
  font-weight: 900;
  text-shadow: 3px 3px 0 var(--c-green);
}

.mail__lead {
  margin: 0;
  color: var(--c-ink-soft);
  font-size: 1.05rem;
}

.mail__hint {
  margin: 0;
  min-height: 3.2em;
  font-weight: 700;
}

.mail__again {
  min-height: var(--tap-min);
  padding: 0 1.2rem;
  border: 3px solid var(--c-ink);
  background: var(--c-yellow);
  color: var(--c-ink);
  font: inherit;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 3px 3px 0 var(--c-ink);
}

.mail__scene {
  padding-bottom: 2rem;
}

@media (min-width: 960px) {
  .mail__inner {
    grid-template-columns: minmax(22rem, 1fr) auto;
    gap: var(--space-5);
  }
}
</style>
