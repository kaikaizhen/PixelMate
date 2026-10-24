<script setup lang="ts">
import { computed, onUnmounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import OverlayPanel from '@/components/common/OverlayPanel.vue'
import PixelButton from '@/components/common/PixelButton.vue'
import FullRoom from '@/components/business/FullRoom.vue'
import { SKILLS, colorOf } from '@/constants/petLooks'
import { useSubmit } from '@/composables/useSubmit'
import { useAuthStore } from '@/stores/useAuthStore'
import { usePetStore } from '@/stores/usePetStore'

type PanelId = 'pet' | 'account' | null

const auth = useAuthStore()
const pets = usePetStore()
const route = useRoute()
const router = useRouter()

const pet = computed(() => pets.pet!)
const panel = ref<PanelId>(null)
const toggle = (id: Exclude<PanelId, null>) => (panel.value = panel.value === id ? null : id)   // same tool again closes it
const welcome = ref(route.query.welcome === '1')
if (welcome.value) window.setTimeout(() => (welcome.value = false), 4500)

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
  panel.value = null
  await auth.logout()
  await router.replace({ name: 'login' })
})
</script>

<template>
  <main class="home game-screen" @contextmenu.prevent>
    <FullRoom :color="colorOf(pet.bodyVariant)" :happy="happy" :name="pet.name" :weather="pet.world.weather" @pat="pat" />

    <!-- HUD: nothing here blocks the room; every function opens in a panel on top of it -->
    <button type="button" class="chip" aria-haspopup="dialog" @click="toggle('pet')">
      <strong>{{ pet.name }}</strong><span>Lv.{{ pet.level }}</span>
    </button>
    <p v-if="welcome" class="toast" role="status">領養成功！{{ pet.name }} 住進你的小屋了。</p>

    <nav class="bar" aria-label="功能">
      <button type="button" class="tool" aria-haspopup="dialog" @click="toggle('pet')"><span aria-hidden="true">🐾</span>小夥伴</button>
      <button type="button" class="tool" disabled aria-label="背包，即將推出"><span aria-hidden="true">🎒</span>背包<small>即將推出</small></button>
      <button type="button" class="tool" disabled aria-label="信箱，即將推出"><span aria-hidden="true">✉️</span>信箱<small>即將推出</small></button>
      <button type="button" class="tool" aria-haspopup="dialog" @click="toggle('account')"><span aria-hidden="true">⚙️</span>帳號</button>
    </nav>

    <OverlayPanel :open="panel === 'pet'" :title="pet.name" @close="panel = null">
      <p class="meta">Lv.{{ pet.level }}　·　經驗 {{ pet.xp }}</p>
      <ul class="skills" aria-label="技能">
        <li v-for="s in SKILLS" :key="s.key">
          <strong>{{ s.label }}</strong><span class="pts">{{ pet.skills[s.key] }}</span><small>{{ s.hint }}</small>
        </li>
      </ul>
      <p class="soon">技能、換裝與寄信很快開放，先陪陪 {{ pet.name }} 吧。</p>
    </OverlayPanel>

    <OverlayPanel :open="panel === 'account'" title="帳號" @close="panel = null">
      <p class="who"><strong>{{ auth.user?.displayName }}</strong><span>{{ auth.user?.email }}</span></p>
      <PixelButton variant="ghost" :loading="loading" @click="submit()">登出</PixelButton>
    </OverlayPanel>
  </main>
</template>

<style scoped>
.home { position: fixed; inset: 0; overflow: hidden; --bar-h: 5.4rem; }

.chip {
  position: absolute;
  top: calc(0.75rem + env(safe-area-inset-top));
  left: 0.75rem;
  display: inline-flex;
  gap: 0.6rem;
  align-items: baseline;
  min-height: var(--tap-min);
  padding: 0 0.9rem;
  font: inherit;
  color: var(--c-ink);
  background: var(--c-paper);
  border: 3px solid var(--c-ink);
  border-radius: var(--radius);
  box-shadow: 3px 3px 0 var(--c-ink);
  cursor: pointer;
  align-items: center;
}
.chip span { font-family: var(--font-pixel); font-size: 0.65rem; color: var(--c-ink-soft); }

.toast {
  position: absolute;
  top: calc(0.75rem + env(safe-area-inset-top));
  left: 50%;
  transform: translateX(-50%);
  margin: 0;
  padding: 0.6rem 1rem;
  max-width: calc(100% - 9rem);
  background: var(--c-green-soft);
  color: var(--c-green);
  border: 3px solid var(--c-green);
  border-radius: var(--radius);
  font-size: 0.9rem;
  text-align: center;
}

.bar {
  position: absolute;
  inset: auto 0.75rem calc(0.75rem + env(safe-area-inset-bottom)) 0.75rem;
  margin-inline: auto;
  max-width: 28rem;
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0.5rem;
}
.tool {
  display: grid;
  justify-items: center;
  align-content: center;
  gap: 0.1rem;
  min-height: var(--bar-h);
  padding: 0.25rem;
  font: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--c-ink);
  background: var(--c-paper);
  border: 3px solid var(--c-ink);
  border-radius: var(--radius);
  box-shadow: 3px 3px 0 var(--c-ink);
  cursor: pointer;
}
.tool span { font-size: 1.25rem; line-height: 1; }
.tool small { font-size: 0.65rem; font-weight: 400; color: var(--c-ink-soft); }
.tool:active:not(:disabled) { transform: translate(2px, 2px); box-shadow: 1px 1px 0 var(--c-ink); }
.tool:disabled { cursor: not-allowed; opacity: 0.7; box-shadow: none; background: var(--c-cream); }

.meta { margin-bottom: 0.5rem; color: var(--c-ink-soft); font-size: 0.95rem; }
.skills { margin: 0 0 0.75rem; padding: 0; list-style: none; display: grid; grid-template-columns: repeat(2, 1fr); gap: 0.5rem; }
.skills li { display: grid; grid-template-columns: 1fr auto; padding: 0.4rem 0.75rem; background: var(--c-cream); border: 2px solid var(--c-line); border-radius: 8px; }
.skills small { grid-column: 1 / -1; color: var(--c-ink-soft); font-size: 0.78rem; line-height: 1.3; }
.pts { font-family: var(--font-pixel); font-size: 0.8rem; }
.soon { color: var(--c-ink-soft); font-size: 0.85rem; }
.who { display: grid; gap: 0.15rem; margin-bottom: 1rem; }
.who span { color: var(--c-ink-soft); font-size: 0.9rem; }

@media (min-width: 860px) {
  /* wide screens: the tools stand in a column at the right edge so they never cover the room */
  .bar { inset: 50% 1.25rem auto auto; transform: translateY(-50%); width: 7.5rem; margin: 0; grid-template-columns: 1fr; }
  .chip { top: 1.25rem; left: 1.25rem; }
}
</style>
