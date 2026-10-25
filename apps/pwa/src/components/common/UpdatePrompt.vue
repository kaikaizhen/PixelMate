<script setup lang="ts">
import { watch } from 'vue'
import { useRegisterSW } from 'virtual:pwa-register/vue'

// registerType "prompt": a new version waits until the player taps "更新", so a form is never wiped mid-typing.
const { needRefresh, offlineReady, updateServiceWorker } = useRegisterSW()

// The "ready offline" note is only a courtesy: let it go after a few seconds so it never covers a button for long.
watch(offlineReady, (v) => {
  if (v) window.setTimeout(() => (offlineReady.value = false), 4000)
})
</script>

<template>
  <div v-if="needRefresh || offlineReady" class="toast" role="status">
    <span>{{ needRefresh ? '有新版本可以使用了。' : '已可離線開啟畫面。' }}</span>
    <button v-if="needRefresh" type="button" @click="updateServiceWorker(true)">更新</button>
    <button type="button" @click="needRefresh = false; offlineReady = false">關閉</button>
  </div>
</template>

<style scoped>
.toast {
  position: fixed;
  inset: auto 1rem calc(1rem + env(safe-area-inset-bottom)) 1rem;
  max-width: 28rem;
  margin-inline: auto;
  display: flex;
  gap: 0.5rem;
  align-items: center;
  padding: 0.75rem 1rem;
  background: var(--c-paper);
  border: 3px solid var(--c-ink);
  border-radius: var(--radius);
  box-shadow: 4px 4px 0 var(--c-ink);
}
span { flex: 1; }
button {
  min-height: 40px;
  padding: 0 0.9rem;
  font: inherit;
  color: var(--c-on-yellow);
  background: var(--c-yellow);
  border: 2px solid var(--c-ink);
  border-radius: 8px;
  cursor: pointer;
}
</style>
