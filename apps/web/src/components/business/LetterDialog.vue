<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'

const props = defineProps<{ open: boolean }>()
const emit = defineEmits<{ close: [] }>()

const dialog = ref<HTMLDialogElement | null>(null)

watch(
  () => props.open,
  async (isOpen) => {
    await nextTick()
    if (isOpen && !dialog.value?.open) dialog.value?.showModal()
    if (!isOpen && dialog.value?.open) dialog.value.close()
  },
)
</script>

<template>
  <dialog ref="dialog" class="letter" aria-labelledby="letter-title" @close="emit('close')">
    <article class="letter__paper">
      <p class="letter__tag">示範信件</p>
      <h3 id="letter-title" class="letter__title">給正在忙的你</h3>
      <p>
        今天路上看到一朵很像你會喜歡的雲，所以先替你收進口袋了。<br />
        晚上記得吃飯，也別忘了喝水。<br />
        我在這裡，牠也在這裡。
      </p>
      <p class="letter__sign">— 遠方的某個人</p>
      <button type="button" class="letter__close" autofocus @click="dialog?.close()">收進口袋</button>
    </article>
  </dialog>
</template>

<style scoped>
.letter {
  max-width: min(92vw, 26rem);
  padding: 0;
  border: 4px solid var(--c-ink);
  background: #fffaf0;
  color: var(--c-ink);
  box-shadow: 8px 8px 0 var(--c-ink);
}

.letter::backdrop {
  background: rgba(74, 48, 64, 0.55);
}

.letter__paper {
  padding: var(--space-3);
  background-image: repeating-linear-gradient(transparent 0 27px, #eadfc8 27px 28px);
  line-height: 28px;
}

.letter__paper > p {
  margin: 0 0 14px;
}

.letter__tag {
  display: inline-block;
  padding: 0 0.6rem;
  background: var(--c-yellow);
  border: 2px solid var(--c-ink);
  font-size: 0.8rem;
  font-weight: 700;
  line-height: 24px !important;
}

.letter__title {
  margin: 0 0 14px;
  font-size: 1.3rem;
  line-height: 28px;
}

.letter__sign {
  text-align: right;
  color: var(--c-ink-soft);
}

.letter__close {
  min-height: var(--tap-min);
  padding: 0 1.4rem;
  border: 3px solid var(--c-ink);
  background: var(--c-pink);
  color: var(--c-ink);
  font: inherit;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 3px 3px 0 var(--c-ink);
}
</style>
