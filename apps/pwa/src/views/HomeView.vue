<script setup lang="ts">
import { computed, onUnmounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AlertBox from '@/components/common/AlertBox.vue'
import PixelButton from '@/components/common/PixelButton.vue'
import PixelPet from '@/components/business/PixelPet.vue'
import { SKILLS, colorOf } from '@/constants/petLooks'
import { useSubmit } from '@/composables/useSubmit'
import { useAuthStore } from '@/stores/useAuthStore'
import { usePetStore } from '@/stores/usePetStore'

const auth = useAuthStore()
const pets = usePetStore()
const route = useRoute()
const router = useRouter()

const pet = computed(() => pets.pet!)
const wide = typeof window !== 'undefined' && window.matchMedia?.('(min-width: 860px)').matches
const petScale = wide ? 7 : 4
const welcome = route.query.welcome === '1'

// Tap the pet: it smiles and hops for a moment (reduced-motion users only get the smile).
const happy = ref(false)
let timer: number | undefined
function pat() {
  happy.value = true
  window.clearTimeout(timer)
  timer = window.setTimeout(() => (happy.value = false), 1500)
}
onUnmounted(() => window.clearTimeout(timer))

const { loading, submit } = useSubmit(async () => {
  await auth.logout()
  await router.replace({ name: 'login' })
})
</script>

<template>
  <main class="home">
    <AlertBox v-if="welcome" tone="success">領養成功！{{ pet.name }} 住進你的小屋了。</AlertBox>

    <section class="room" aria-label="你的小屋">
      <button type="button" class="pet" :class="{ hop: happy }" :aria-label="`摸摸 ${pet.name}`" @click="pat">
        <PixelPet :color="colorOf(pet.bodyVariant)" :face="happy ? 'happy' : 'normal'" :scale="petScale" :label="pet.name" />
      </button>
      <div class="floor" aria-hidden="true" />
    </section>

    <section class="card" aria-labelledby="pet-name">
      <h1 id="pet-name">{{ pet.name }}</h1>
      <p class="meta">Lv.{{ pet.level }}　·　經驗 {{ pet.xp }}</p>
      <ul class="skills" aria-label="技能">
        <li v-for="s in SKILLS" :key="s.key">
          <strong>{{ s.label }}</strong><span class="pts">{{ pet.skills[s.key] }}</span><small>{{ s.hint }}</small>
        </li>
      </ul>
      <p class="soon">技能、換裝與寄信很快開放，先陪陪 {{ pet.name }} 吧。</p>
    </section>

    <footer class="who">
      <p>{{ auth.user?.displayName }}　<span>{{ auth.user?.email }}</span></p>
      <div class="actions"><PixelButton variant="ghost" :loading="loading" @click="submit()">登出</PixelButton></div>
    </footer>
  </main>
</template>

<style scoped>
.home {
  min-height: 100dvh;
  display: grid;
  align-content: center;
  justify-items: center;
  gap: 0.75rem;
  padding: 1rem;
}
.home > * { width: min(100%, 28rem); }
.room {
  position: relative;
  display: grid;
  justify-items: center;
  padding-top: 1rem;
  background: linear-gradient(var(--c-pink-soft) 0 68%, transparent 68%);
  border: 3px solid var(--c-ink);
  border-radius: 14px;
  overflow: hidden;
}
.floor { position: absolute; inset: auto 0 0 0; height: 32%; background: #e6c9a0; border-top: 3px solid var(--c-ink); z-index: 0; }
.pet {
  position: relative;
  z-index: 1;
  margin-bottom: 0.7rem;
  padding: 0;
  background: none;
  border: 0;
  cursor: pointer;
  border-radius: var(--radius);
}
.hop { animation: hop 0.5s steps(4) 1; }
@keyframes hop { 40% { transform: translateY(-18px); } }
.card {
  display: grid;
  gap: 0.4rem;
  padding: 1rem 1.25rem;
  background: var(--c-paper);
  border: 3px solid var(--c-ink);
  border-radius: 14px;
  box-shadow: 6px 6px 0 var(--c-ink);
}
h1 { font-size: 1.4rem; }
.meta { color: var(--c-ink-soft); font-size: 0.9rem; }
.skills { margin: 0.15rem 0; padding: 0; list-style: none; display: grid; grid-template-columns: repeat(2, 1fr); gap: 0.5rem; }
.skills li { display: grid; grid-template-columns: 1fr auto; padding: 0.4rem 0.75rem; background: var(--c-cream); border: 2px solid var(--c-line); border-radius: 8px; }
.skills small { grid-column: 1 / -1; color: var(--c-ink-soft); font-size: 0.78rem; line-height: 1.3; }
.pts { font-family: var(--font-pixel); font-size: 0.8rem; }
.soon { color: var(--c-ink-soft); font-size: 0.85rem; }
.who { display: grid; gap: 0.4rem; text-align: center; font-size: 0.9rem; color: var(--c-ink-soft); }
.who p { margin: 0; }
.who span { font-size: 0.8rem; }
.actions { width: min(100%, 12rem); margin-inline: auto; }
@media (min-width: 860px) {
  .home { grid-template-columns: minmax(0, 24rem) minmax(0, 26rem); grid-template-areas: 'room card' 'room who'; column-gap: 2.5rem; justify-content: center; align-content: center; }
  .home > * { width: auto; }
  .room { grid-area: room; align-self: center; padding-top: 2rem; }
  .card { grid-area: card; }
  .who { grid-area: who; }
}
@media (prefers-reduced-motion: reduce) { .hop { animation: none; } }
</style>
