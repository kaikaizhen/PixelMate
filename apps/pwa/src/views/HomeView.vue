<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import OverlayPanel from '@/components/common/OverlayPanel.vue'
import MailPanel from '@/components/business/MailPanel.vue'
import RoomEditor from '@/components/business/RoomEditor.vue'
import { useRoomStore } from '@/stores/useRoomStore'
import { useSocialStore } from '@/stores/useSocialStore'
import PixelButton from '@/components/common/PixelButton.vue'
import FullRoom from '@/components/business/FullRoom.vue'
import { SKILLS, colorOf } from '@/constants/petLooks'
import { ACTIVITIES } from '@/constants/activities'
import { petApi } from '@/api/petApi'
import { utc } from '@/utils/time'
import { createRealtime } from '@/realtime/connection'
import { batcher } from '@/realtime/routeEvent'
import { ApiError } from '@/api/http'
import { MAX_SKILL_POINTS, type SkillKey } from '@/types/pet.types'
import { useSubmit } from '@/composables/useSubmit'
import { useAuthStore } from '@/stores/useAuthStore'
import { useGameStore } from '@/stores/useGameStore'
import { useInventoryStore } from '@/stores/useInventoryStore'
import { usePetStore } from '@/stores/usePetStore'
import type { InventoryItem } from '@/types/pet.types'

type PanelId = 'pet' | 'bag' | 'mail' | 'account' | null

const auth = useAuthStore()
const pets = usePetStore()
const bag = useInventoryStore()
const social = useSocialStore()
const room = useRoomStore()
const game = useGameStore()
const route = useRoute()
const router = useRouter()

const pet = computed(() => pets.pet!)
const panel = ref<PanelId>(null)
const toggle = (id: Exclude<PanelId, null>) => {
  if (room.editing) room.cancel()
  panel.value = panel.value === id ? null : id   // same tool again closes it
  if (panel.value === 'bag') void loadBag()
}

// The pet lives on the server: re-read it when this screen comes back, every minute, and when its activity slot ends
// (that also makes the server catch up on anything it found meanwhile). Other devices' changes show up the same way.
const found = ref('')
let foundTimer: number | undefined

async function checkFinds() {
  const key = `pm.seen.${pet.value.petId}`
  let seen: number | null = null
  try { seen = Number(localStorage.getItem(key)) || null } catch { /* private mode */ }
  const events = await petApi.events(10)
  const fresh = events.filter((e) => seen !== null && utc(e.occurredAt) > seen)
  const newest = events.reduce((m, e) => Math.max(m, utc(e.occurredAt)), seen ?? Date.now())
  try { localStorage.setItem(key, String(newest)) } catch { /* private mode */ }
  if (!fresh.length) return
  found.value = fresh.length === 1 ? `${pet.value.name}散步時撿到了「${fresh[0].name}」` : `${pet.value.name}散步時撿到了 ${fresh.length} 樣東西`
  window.clearTimeout(foundTimer)
  foundTimer = window.setTimeout(() => (found.value = ''), 6000)
  if (bag.loaded) void bag.load(true).catch(() => {})
}

// A letter arrives when the postman gets there: the badge rises, the pet perks up and a note pops up.
async function checkMail() {
  if (await social.refreshUnread()) {
    found.value = `郵差送來一封信！`
    window.clearTimeout(foundTimer)
    foundTimer = window.setTimeout(() => (found.value = ''), 6000)
    pat()
  }
}

async function refreshLife() {
  if (document.visibilityState !== 'visible') return
  try {
    await Promise.all([pets.load(true), panel.value === 'bag' ? bag.load(true) : null])
    await checkFinds()
    await checkMail()
    if (!room.editing) await room.load(true)
  } catch { /* offline: the next tick tries again */ }
}
// Live link: the API pushes "something changed" and the screen re-reads just that. Without the link the minute timer below does it.
const quietly = (p: Promise<unknown>) => void p.catch(() => {})
const onEvent = batcher({
  pet: () => quietly(pets.load(true)),
  inventory: () => { if (bag.loaded) quietly(bag.load(true)) },
  room: () => { if (!room.editing) quietly(room.load(true)) },
  mail: () => quietly(checkMail().then(() => (panel.value === 'mail' ? social.loadInbox() : undefined))),
  friends: () => quietly(social.loadFriends()),
  finds: () => quietly(checkFinds()),
})
const realtime = createRealtime(onEvent, () => { void refreshLife(); quietly(social.loadFriends()) })
const resyncTimer = window.setInterval(() => { if (!realtime.connected.value) void refreshLife() }, 60_000)
let slotTimer: number | undefined
watch(() => pet.value.activity?.endsAt, (end) => {
  window.clearTimeout(slotTimer)
  if (end) slotTimer = window.setTimeout(refreshLife, Math.max(1000, utc(end) - Date.now() + 1500))
}, { immediate: true })
onMounted(() => {
  document.addEventListener('visibilitychange', refreshLife)
  window.addEventListener('focus', refreshLife)
  void checkFinds().catch(() => {})
  void social.refreshUnread().catch(() => {})
  void room.load().catch(() => {})
  void realtime.start()
})
onUnmounted(() => {
  window.clearInterval(resyncTimer)
  window.clearTimeout(slotTimer)
  window.clearTimeout(foundTimer)
  realtime.stop()
  document.removeEventListener('visibilitychange', refreshLife)
  window.removeEventListener('focus', refreshLife)
})
const doing = computed(() => ACTIVITIES[pet.value.activity?.key ?? ''])
const place = computed(() => ({ balcony: '在陽台', yard: '在院子' } as Record<string, string>)[pet.value.world.scene])

// ---- backpack ---------------------------------------------------------------------------------------------------
const TABS = [
  { id: 'all', label: '全部', match: () => true },
  { id: 'wear', label: '穿戴', match: (i: InventoryItem) => !!i.slot },
  { id: 'room', label: '房間', match: (i: InventoryItem) => i.category === 'furniture' || i.category === 'decor' },
  { id: 'misc', label: '收藏', match: (i: InventoryItem) => i.category === 'collectible' || i.category === 'stationery' },
] as const
const tab = ref<(typeof TABS)[number]['id']>('all')
const shown = computed(() => bag.items.filter(TABS.find((t) => t.id === tab.value)!.match))
const bagError = ref('')
const RARITY = { common: '普通', rare: '稀有', precious: '珍貴' } as const
const thumb = (i: InventoryItem) => game.url(i.assetKey)

async function loadBag() {
  bagError.value = ''
  try {
    await Promise.all([bag.load(true), pets.load(true), game.load()])
  } catch (e) {
    bagError.value = e instanceof ApiError ? e.message : '背包暫時打不開，請稍後再試。'
  }
}

const bagOp = useSubmit(async (run: () => Promise<void>) => {
  bagError.value = ''
  try {
    await run()
  } catch (e) {
    if (e instanceof ApiError && e.code === 'REVISION_CONFLICT') {
      bagError.value = '資料已在別的地方更新，已幫你重新載入，再試一次吧。'
      await bag.load(true)
    } else if (e instanceof ApiError) bagError.value = e.message
    else throw e
  }
})
// Room things (furniture, decor, collectibles) go into the room: tapping one opens the editor with it already placed.
async function decorate(item?: InventoryItem) {
  bagError.value = ''
  try {
    await Promise.all([room.load(), bag.load(true)])   // the editor lists what the player owns, so the bag must be fresh
  } catch (e) {
    bagError.value = e instanceof ApiError ? e.message : '小屋暫時打不開，請稍後再試。'
    found.value = bagError.value                       // visible even when the bag card is closed
    return void window.setTimeout(() => (found.value = ''), 6000)
  }
  panel.value = null
  room.startEdit()
  if (item) {
    if (item.itemId.startsWith('world.decor.floor_tile_')) room.setFloor(item.itemId)
    else room.add(item.itemId)
  }
}
const tap = (i: InventoryItem) => {
  if (!i.slot) return void decorate(i)
  void bagOp.submit(() => bag.equip(i.equipped ? null : i, i.slot!))
}

// development builds: a handful of items so wearing can be tried before pets find anything on their own
const SAMPLE = ['equip.hat.straw_post', 'equip.hat.crown_garden', 'equip.accessory.bowtie_festival', 'equip.accessory.glasses_garden',
  'equip.feet.sneaker_picnic', 'equip.handheld.bouquet_garden', 'equip.accessory.wings_friendship', 'equip.costume.dog',
  'equip.costume.cat', 'world.furniture.bed_garden', 'item.collectible.shell_seaside', 'item.stationery.stamp_post']
const grant = () => bagOp.submit(() => bag.devGrant(SAMPLE))
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

// ---- skill points -----------------------------------------------------------------------------------------------
const notice = ref('')
const confirmReset = ref(false)
const isDev = import.meta.env.DEV
const canAdd = (key: SkillKey) => pet.value.remainingSkillPoints > 0 && pet.value.skills[key] < MAX_SKILL_POINTS

const skillOp = useSubmit(async (run: () => Promise<void>) => {
  notice.value = ''
  try {
    await run()
  } catch (e) {
    if (e instanceof ApiError && e.code === 'REVISION_CONFLICT') notice.value = '資料已在別的地方更新，已幫你重新載入，再試一次吧。'
    else throw e
  }
})
const add = (key: SkillKey) => skillOp.submit(() => pets.allocate(key))
const reset = async () => {
  if (!confirmReset.value) return void (confirmReset.value = true)
  confirmReset.value = false
  await skillOp.submit(() => pets.resetSkills())
}
const spent = computed(() => Object.values(pet.value.skills).reduce((a, b) => a + b, 0))
const levelUp = () => skillOp.submit(() => pets.devLevelUp())

const { loading, submit } = useSubmit(async () => {
  panel.value = null
  await auth.logout()
  await router.replace({ name: 'login' })
})
</script>

<template>
  <main class="home game-screen" @contextmenu.prevent>
    <FullRoom :color="colorOf(pet.bodyVariant)" :happy="happy" :name="pet.name" :weather="pet.world.weather" :equipment="pet.equipment" :activity="pet.activity?.key" :scene="pet.world.scene" :placements="room.view.placements" :floor="room.view.floorItemId ?? undefined" :editing="room.editing" :selected="room.selected"
      @select="room.selected = $event" @move="room.move" @pat="pat" />

    <!-- HUD: nothing here blocks the room; every function opens in a panel on top of it -->
    <button v-if="!room.editing" type="button" class="chip" aria-haspopup="dialog" @click="toggle('pet')">
      <strong>{{ pet.name }}</strong><span>Lv.{{ pet.level }}</span><em v-if="pet.remainingSkillPoints" class="badge">+{{ pet.remainingSkillPoints }}</em>
    </button>
    <p v-if="doing" class="doing" role="status" :data-activity="pet.activity?.key">{{ doing.emoji }} {{ pet.name }}正在{{ doing.label }}<template v-if="place">・{{ place }}</template></p>
    <p v-if="found && !welcome" class="toast" role="status">🎁 {{ found }}</p>
    <p v-if="welcome" class="toast" role="status">領養成功！{{ pet.name }} 住進你的小屋了。</p>

    <nav v-if="!room.editing" class="bar" aria-label="功能">
      <button type="button" class="tool" aria-haspopup="dialog" @click="toggle('pet')"><span aria-hidden="true">🐾</span>小夥伴</button>
      <button type="button" class="tool" aria-haspopup="dialog" @click="toggle('bag')"><span aria-hidden="true">🎒</span>背包</button>
      <button type="button" class="tool" @click="decorate()"><span aria-hidden="true">🏠</span>布置</button>
      <button type="button" class="tool" aria-haspopup="dialog" :aria-label="social.unread ? `信箱，${social.unread} 封未讀` : '信箱'" @click="toggle('mail')"><span aria-hidden="true">✉️</span>信箱<b v-if="social.unread" class="mark">{{ social.unread }}</b></button>
      <button type="button" class="tool" aria-haspopup="dialog" @click="toggle('account')"><span aria-hidden="true">⚙️</span>帳號</button>
    </nav>

    <OverlayPanel :open="panel === 'pet'" :title="pet.name" :subtitle="`Lv.${pet.level}　·　經驗 ${pet.xp}　·　可分配 ${pet.remainingSkillPoints} 點`" @close="panel = null">
      <p v-if="notice" class="notice" role="alert">{{ notice }}</p>
      <p v-else-if="skillOp.error.value" class="notice" role="alert">{{ skillOp.error.value.message }}</p>
      <ul class="skills" aria-label="技能">
        <li v-for="s in SKILLS" :key="s.key">
          <span class="name"><strong>{{ s.label }}</strong><small>{{ s.hint }}</small></span>
          <span class="pips" role="img" :aria-label="`${s.label} ${pet.skills[s.key]} / ${MAX_SKILL_POINTS} 點`">
            <i v-for="n in MAX_SKILL_POINTS" :key="n" :class="{ on: n <= pet.skills[s.key] }" />
          </span>
          <button type="button" class="add" :disabled="!canAdd(s.key) || skillOp.loading.value" :aria-label="`${s.label} 加 1 點`" @click="add(s.key)">＋</button>
        </li>
      </ul>
      <div class="acts">
        <button type="button" class="ghost" :disabled="spent === 0 || skillOp.loading.value" @click="reset">{{ confirmReset ? '確定重置？再按一次' : '重置技能' }}</button>
        <button v-if="isDev" type="button" class="ghost dev" @click="levelUp">（測試）升一級</button>
      </div>
    </OverlayPanel>

    <OverlayPanel :open="panel === 'bag'" title="背包" :subtitle="`${bag.items.length} 種道具`" @close="panel = null">
      <p v-if="bagError" class="notice" role="alert">{{ bagError }}</p>
      <div class="tabs" role="tablist" aria-label="分類">
        <button v-for="t in TABS" :key="t.id" type="button" role="tab" :aria-selected="tab === t.id" :class="{ on: tab === t.id }" @click="tab = t.id">{{ t.label }}</button>
      </div>
      <p v-if="!shown.length" class="empty">{{ bag.items.length ? '這一類還沒有道具。' : '還沒有道具，小夥伴會自己去找喔。' }}</p>
      <ul v-else class="items" aria-label="道具">
        <li v-for="i in shown" :key="i.inventoryId">
          <button type="button" class="item" :class="[i.rarity, { worn: i.equipped }]" :aria-pressed="i.slot ? i.equipped : undefined"
            :aria-label="`${i.name}，${RARITY[i.rarity]}${i.slot ? (i.equipped ? '，穿戴中，點一下脫下' : '，點一下穿上') : '，房間用品'}`" :disabled="bagOp.loading.value" @click="tap(i)">
            <img v-if="thumb(i)" :src="thumb(i)" alt="" draggable="false" class="pixelated" />
            <span class="nm">{{ i.name }}</span>
            <em v-if="i.quantity > 1" class="qty">×{{ i.quantity }}</em>
            <b v-if="i.equipped" class="on-tag">穿戴中</b>
          </button>
        </li>
      </ul>
      <div v-if="isDev" class="acts"><button type="button" class="ghost dev" @click="grant">（測試）領一些道具</button></div>
    </OverlayPanel>

    <RoomEditor />

    <OverlayPanel :open="panel === 'mail'" tall title="信箱" subtitle="寫信給好友，郵差會送過去" @close="panel = null">
      <MailPanel />
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

.doing {
  position: absolute;
  top: calc(0.75rem + env(safe-area-inset-top) + var(--tap-min) + 0.5rem);
  left: 0.75rem;
  margin: 0;
  padding: 0.25rem 0.7rem;
  font-size: 0.8rem;
  color: var(--c-ink);
  background: var(--c-paper);
  border: 3px solid var(--c-ink);
  border-radius: var(--radius);
}
.mark { position: absolute; top: -8px; right: -6px; min-width: 20px; padding: 0 0.3rem; font-family: var(--font-pixel); font-size: 0.6rem; line-height: 20px; color: #fff; background: var(--c-error); border: 2px solid var(--c-ink); border-radius: 10px; text-align: center; }
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
  grid-template-columns: repeat(5, 1fr);
  gap: 0.5rem;
}
.tool {
  position: relative;
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
.left { font-family: var(--font-pixel); font-size: 0.85rem; }
.notice { margin: 0 0 0.5rem; padding: 0.5rem 0.75rem; color: var(--c-error); background: var(--c-error-soft); border: 2px solid currentColor; border-radius: 8px; font-size: 0.85rem; }
.skills { margin: 0 0 0.5rem; padding: 0; list-style: none; display: grid; gap: 0.3rem; }
.skills li { display: grid; grid-template-columns: 1fr auto auto; align-items: center; gap: 0.6rem; padding: 0.2rem 0.4rem 0.2rem 0.75rem; background: var(--c-cream); border: 2px solid var(--c-line); border-radius: 8px; }
.name { display: grid; line-height: 1.25; }
.name small { color: var(--c-ink-soft); font-size: 0.72rem; }
.pips { display: inline-flex; gap: 3px; }
.pips i { width: 12px; height: 12px; background: var(--c-paper); border: 2px solid var(--c-ink); }
.pips i.on { background: var(--c-yellow); }
.add { min-width: 44px; min-height: 44px; font: inherit; font-size: 1.2rem; font-weight: 700; color: var(--c-ink); background: var(--c-yellow); border: 3px solid var(--c-ink); border-radius: 8px; box-shadow: 2px 2px 0 var(--c-ink); cursor: pointer; }
.add:disabled { opacity: 0.35; box-shadow: none; cursor: not-allowed; }
.acts { display: flex; gap: 0.5rem; flex-wrap: wrap; }
.ghost { min-height: 40px; padding: 0 0.9rem; font: inherit; font-size: 0.9rem; color: var(--c-ink); background: var(--c-paper); border: 2px solid var(--c-ink); border-radius: 8px; cursor: pointer; }
.ghost:disabled { opacity: 0.4; cursor: not-allowed; }
.ghost.dev { background: var(--c-pink-soft); }
.tabs { display: flex; gap: 0.4rem; margin-bottom: 0.5rem; }
.tabs button { min-height: 40px; padding: 0 0.8rem; font: inherit; font-size: 0.85rem; color: var(--c-ink); background: var(--c-paper); border: 2px solid var(--c-ink); border-radius: 8px; cursor: pointer; }
.tabs button.on { background: var(--c-yellow); font-weight: 700; }
.empty { color: var(--c-ink-soft); font-size: 0.9rem; padding: 0.5rem 0; }
.items { margin: 0 0 0.5rem; padding: 0; list-style: none; display: grid; grid-template-columns: repeat(4, 1fr); gap: 0.4rem; }
.item { position: relative; display: grid; justify-items: center; gap: 0.15rem; width: 100%; padding: 0.35rem 0.2rem; font: inherit; color: var(--c-ink); background: var(--c-cream); border: 3px solid var(--c-line); border-radius: 8px; cursor: pointer; }
.item.rare { border-color: #6fb1d6; }
.item.precious { border-color: #e0a830; }
.item.worn { background: var(--c-pink-soft); box-shadow: 2px 2px 0 var(--c-ink); }
.item img { width: 48px; height: 48px; object-fit: contain; }
.nm { font-size: 0.68rem; line-height: 1.2; text-align: center; overflow-wrap: anywhere; }
.qty { position: absolute; top: 2px; right: 4px; font-style: normal; font-size: 0.7rem; font-weight: 700; }
.on-tag { position: absolute; top: 2px; left: 2px; font-size: 0.55rem; padding: 0 0.25rem; color: #4a3040; background: var(--c-yellow); border: 1px solid var(--c-ink); border-radius: 4px; }
.badge { font-style: normal; font-family: var(--font-pixel); font-size: 0.6rem; padding: 0.15rem 0.4rem; color: #4a3040; background: var(--c-yellow); border: 2px solid var(--c-ink); border-radius: 6px; }
.who { display: grid; gap: 0.15rem; margin-bottom: 1rem; }
.who span { color: var(--c-ink-soft); font-size: 0.9rem; }

@media (min-width: 860px) {
  /* wide screens: the tools stand in a column at the right edge so they never cover the room */
  .bar { inset: 50% 1.25rem auto auto; transform: translateY(-50%); width: 7.5rem; margin: 0; grid-template-columns: 1fr; }
  .chip { top: 1.25rem; left: 1.25rem; }
  .doing { top: calc(1.25rem + var(--tap-min) + 0.5rem); left: 1.25rem; }
}
</style>
