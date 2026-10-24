<script setup lang="ts">
import { useRouter } from 'vue-router'
import PixelButton from '@/components/common/PixelButton.vue'
import PetMascot from '@/components/business/PetMascot.vue'
import { useSubmit } from '@/composables/useSubmit'
import { useAuthStore } from '@/stores/useAuthStore'

const auth = useAuthStore()
const router = useRouter()

const { loading, submit } = useSubmit(async () => {
  await auth.logout()
  await router.replace({ name: 'login' })
})
</script>

<template>
  <main class="home">
    <PetMascot :scale="4" />
    <h1>嗨，{{ auth.user?.displayName }}！</h1>
    <p>你的小屋正在整理中，領養小夥伴的功能很快就會開放。</p>
    <p class="mail">{{ auth.user?.email }}</p>
    <div class="actions"><PixelButton variant="ghost" :loading="loading" @click="submit()">登出</PixelButton></div>
  </main>
</template>

<style scoped>
.home {
  min-height: 100dvh;
  display: grid;
  align-content: center;
  justify-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-2);
  text-align: center;
}
h1 { font-size: 1.5rem; }
.mail { color: var(--c-ink-soft); font-size: 0.9rem; }
.actions { width: min(100%, 14rem); }
</style>
