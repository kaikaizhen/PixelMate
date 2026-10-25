<script setup lang="ts">
import { computed, ref } from 'vue'
import { ApiError } from '@/api/http'
import { useSubmit } from '@/composables/useSubmit'
import { useGameStore } from '@/stores/useGameStore'
import { useInventoryStore } from '@/stores/useInventoryStore'
import { useRoomStore } from '@/stores/useRoomStore'
import { MAX_PLACEMENTS } from '@/types/room.types'
import type { InventoryItem } from '@/types/pet.types'

// The decorating dock. Phones: it takes the tool bar's place at the bottom. Wide screens: a card at the right edge.
// Top row: 取消 · title · 完成. Then what is selected (with a clear 收起 button), the categories, and a row of things to put in.
const room = useRoomStore()
const bag = useInventoryStore()
const game = useGameStore()

function isFloor(i: InventoryItem) { return i.itemId.startsWith('world.decor.floor_tile_') }
const TABS = [
  { id: 'furniture', label: '家具', match: (i: InventoryItem) => i.category === 'furniture' },
  { id: 'decor', label: '擺飾', match: (i: InventoryItem) => i.category === 'decor' && !isFloor(i) },
  { id: 'collectible', label: '收藏', match: (i: InventoryItem) => i.category === 'collectible' },
  { id: 'floor', label: '地板', match: isFloor },
] as const

const tab = ref<(typeof TABS)[number]['id']>('furniture')
const error = ref('')
const shown = computed(() => bag.items.filter(TABS.find((t) => t.id === tab.value)!.match))
const draft = computed(() => room.draft!)

/** Copies still free to put in: owned minus what already stands in the room (a floor counts as one). */
function left(i: InventoryItem): number {
  const used = draft.value.placements.filter((p) => p.itemId === i.itemId).length + (draft.value.floorItemId === i.itemId ? 1 : 0)
  return i.quantity - used
}
const nameOf = (id: string) => bag.items.find((i) => i.itemId === id)?.name ?? game.assets[id]?.name ?? ''
const selectedName = computed(() => (room.selected >= 0 ? nameOf(draft.value.placements[room.selected]?.itemId ?? '') : ''))
const floorOn = (i: InventoryItem) => draft.value.floorItemId === i.itemId

function pick(i: InventoryItem) {
  error.value = ''
  if (isFloor(i)) return room.setFloor(floorOn(i) ? null : i.itemId)
  if (left(i) < 1) return void (error.value = `「${i.name}」都已經擺出來了。`)
  if (!room.add(i.itemId)) error.value = `小屋最多放 ${MAX_PLACEMENTS} 件。`
}

const saveOp = useSubmit(async () => {
  error.value = ''
  try {
    await room.save()
  } catch (e) {
    if (e instanceof ApiError && e.code === 'REVISION_CONFLICT') error.value = '小屋已在別的地方更新，已幫你重新載入。'
    else throw e
  }
})
const message = computed(() => error.value || saveOp.error.value?.message || '')
</script>

<template>
  <section v-if="room.draft" class="dock" role="dialog" aria-modal="false" aria-label="布置小屋">
    <header>
      <button type="button" class="cancel" @click="room.cancel()">取消</button>
      <h2>布置小屋<small>{{ draft.placements.length }} 件</small></h2>
      <button type="button" class="done" :disabled="saveOp.loading.value" @click="saveOp.submit()">{{ saveOp.loading.value ? '儲存中…' : '完成' }}</button>
    </header>

    <p v-if="message" class="notice" role="alert">{{ message }}</p>
    <div v-if="selectedName" class="picked" role="status">
      <span>已選取：<strong>{{ selectedName }}</strong>　用手指在小屋裡拖曳移動</span>
      <button type="button" class="remove" @click="room.remove(room.selected)">🗑 收起</button>
    </div>
    <p v-else class="hint" role="status">點下面的東西放進小屋；點小屋裡的東西可以選取、拖曳。</p>

    <div class="tabs" role="tablist" aria-label="分類">
      <button v-for="t in TABS" :key="t.id" type="button" role="tab" :aria-selected="tab === t.id" :class="{ on: tab === t.id }" @click="tab = t.id">{{ t.label }}</button>
    </div>

    <p v-if="!shown.length" class="empty">這一類還沒有道具，小夥伴撿到就會出現在這裡。</p>
    <ul v-else class="tray" aria-label="可擺放的道具">
      <li v-if="tab === 'floor'">
        <button type="button" class="card plain" :class="{ on: !draft.floorItemId }" aria-label="原本的地板" @click="room.setFloor(null)"><span class="ph">🏠</span><span class="nm">原本的</span></button>
      </li>
      <li v-for="i in shown" :key="i.inventoryId">
        <button type="button" class="card" :class="[i.rarity, { on: tab === 'floor' && floorOn(i), gone: tab !== 'floor' && left(i) < 1 }]"
          :aria-label="tab === 'floor' ? `${i.name}，${floorOn(i) ? '使用中' : '點一下換上'}` : `${i.name}，還有 ${left(i)} 個可以擺`" @click="pick(i)">
          <img v-if="game.url(i.assetKey)" :src="game.url(i.assetKey)" alt="" draggable="false" class="pixelated" />
          <span class="nm">{{ i.name }}</span>
          <em v-if="tab !== 'floor'" class="qty">剩 {{ left(i) }}</em>
          <b v-else-if="floorOn(i)" class="tag">使用中</b>
        </button>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.dock {
  position: absolute;
  z-index: 5;
  inset: auto 0.75rem calc(0.75rem + env(safe-area-inset-bottom)) 0.75rem;
  max-width: 28rem;
  margin-inline: auto;
  display: grid;
  gap: 0.45rem;
  padding: 0.6rem 0.75rem 0.7rem;
  color: var(--c-ink);
  background: var(--c-paper);
  border: 3px solid var(--c-ink);
  border-radius: 14px;
  box-shadow: 5px 5px 0 var(--c-ink);
  animation: rise 0.18s steps(4);
}
@keyframes rise { from { transform: translateY(24px); opacity: 0; } }
header { display: grid; grid-template-columns: auto 1fr auto; align-items: center; gap: 0.5rem; }
h2 { margin: 0; text-align: center; font-size: 1rem; line-height: 1.2; }
h2 small { display: block; font-size: 0.7rem; font-weight: 400; color: var(--c-ink-soft); }
.cancel, .done { min-height: 44px; padding: 0 1rem; font: inherit; font-weight: 700; color: var(--c-ink); border: 3px solid var(--c-ink); border-radius: 8px; cursor: pointer; }
.cancel { background: var(--c-paper); }
.done { background: var(--c-yellow); box-shadow: 2px 2px 0 var(--c-ink); }
.done:disabled { opacity: 0.5; box-shadow: none; cursor: wait; }
.notice { margin: 0; padding: 0.4rem 0.7rem; color: var(--c-error); background: var(--c-error-soft); border: 2px solid currentColor; border-radius: 8px; font-size: 0.82rem; }
.hint, .empty { margin: 0; text-align: center; color: var(--c-ink-soft); font-size: 0.8rem; }
.picked { display: flex; align-items: center; justify-content: space-between; gap: 0.5rem; padding: 0.35rem 0.4rem 0.35rem 0.7rem; font-size: 0.8rem; background: var(--c-pink-soft); border: 2px solid var(--c-ink); border-radius: 8px; }
.remove { flex: none; min-height: 40px; padding: 0 0.8rem; font: inherit; font-weight: 700; font-size: 0.85rem; color: #fff; background: var(--c-error); border: 2px solid var(--c-ink); border-radius: 8px; cursor: pointer; }
.tabs { display: grid; grid-template-columns: repeat(4, 1fr); gap: 0.35rem; }
.tabs button { min-height: 38px; font: inherit; font-size: 0.85rem; color: var(--c-ink); background: var(--c-paper); border: 2px solid var(--c-ink); border-radius: 8px; cursor: pointer; }
.tabs button.on { background: var(--c-yellow); font-weight: 700; }
.tray { margin: 0; padding: 0.2rem 0.1rem 0.35rem; list-style: none; display: flex; gap: 0.4rem; overflow-x: auto; scroll-snap-type: x proximity; }
.tray li { flex: none; scroll-snap-align: start; }
.card { position: relative; display: grid; justify-items: center; align-content: start; gap: 0.1rem; width: 4.6rem; height: 5.6rem; padding: 0.3rem 0.2rem; font: inherit; color: var(--c-ink); background: var(--c-cream); border: 3px solid var(--c-line); border-radius: 8px; cursor: pointer; }
.card.rare { border-color: #6fb1d6; }
.card.precious { border-color: #e0a830; }
.card.on { background: var(--c-pink-soft); box-shadow: 2px 2px 0 var(--c-ink); }
.card.gone { opacity: 0.4; }
.card img { width: 48px; height: 48px; object-fit: contain; }
.card .ph { font-size: 1.6rem; line-height: 48px; }
.nm { max-width: 100%; font-size: 0.68rem; line-height: 1.15; text-align: center; overflow: hidden; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
.qty { position: absolute; right: 3px; bottom: 2px; font-style: normal; font-size: 0.6rem; color: var(--c-ink-soft); }
.tag { position: absolute; top: -7px; left: -5px; padding: 0 0.3rem; font-size: 0.6rem; background: var(--c-yellow); border: 2px solid var(--c-ink); border-radius: 6px; }
@media (min-width: 860px) {
  .dock { inset: 50% 1.25rem auto auto; transform: translateY(-50%); width: 24rem; max-width: none; margin: 0; animation: none; }
  .tray { flex-wrap: wrap; overflow: visible; }
}
@media (prefers-reduced-motion: reduce) { .dock { animation: none; } }
</style>
