<script setup lang="ts">
import { useOnline } from '@/composables/useOnline'

const online = useOnline()
</script>

<template>
  <p v-if="!online" class="offline" role="status">目前沒有網路，登入與同步需要連線。</p>
</template>

<style scoped>
.offline {
  position: sticky;
  top: 0;
  z-index: 10;
  margin: 0;
  padding: 0.5rem 1rem;
  background: var(--c-yellow);
  color: #4a3040;
  text-align: center;
  font-size: 0.9rem;
  border-bottom: 3px solid #4a3040;
}
</style>
