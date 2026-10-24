<script setup lang="ts">
import { computed } from 'vue'
import { checkPassword } from '@/utils/validators'

const props = defineProps<{ password: string }>()
const c = computed(() => checkPassword(props.password))
const items = computed(() => [
  { ok: c.value.length, text: '至少 10 個字元' },
  { ok: c.value.letter, text: '包含英文字母' },
  { ok: c.value.digit, text: '包含數字' },
])
</script>

<template>
  <ul class="rules" aria-label="密碼規則">
    <li v-for="i in items" :key="i.text" :class="{ ok: i.ok }">
      <span aria-hidden="true">{{ i.ok ? '✔' : '○' }}</span>
      {{ i.text }}<span class="sr-only">{{ i.ok ? '（已符合）' : '（未符合）' }}</span>
    </li>
  </ul>
</template>

<style scoped>
.rules { margin: 0.25rem 0 0; padding: 0; list-style: none; display: flex; flex-wrap: wrap; gap: 0.25rem 1rem; font-size: 0.85rem; color: var(--c-ink-soft); }
.ok { color: var(--c-green); }
</style>
