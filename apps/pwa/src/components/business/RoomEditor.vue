<script setup lang="ts">
import { computed, ref } from 'vue'
import { ApiError } from '@/api/http'
import { useSubmit } from '@/composables/useSubmit'
import { useGameStore } from '@/stores/useGameStore'
import { useInventoryStore } from '@/stores/useInventoryStore'
import { useRoomStore } from '@/stores/useRoomStore'
import { MAX_PLACEMENTS } from '@/types/room.types'
import type { InventoryItem } from '@/types/pet.types'

// Content of the 布置小屋 card: pick owned things to put in, drag them around the room itself, 完成 saves the whole layout.
const room = useRoomStore()
const bag = useInventoryStore()
const game = useGameStore()

const TABS = [
  { id: 'furniture', label: '家具', match: (i: InventoryItem) => i.category === 'furniture' },
  { id: 'decor', label: '布置', match: (i: InventoryItem) => i.category === 'decor' && !isFloor(i) },
  { id: 'collectible', label: '收藏', match: (i: InventoryItem) => i.category === 'collectible' },
  { id: 'floor', label: '地板', match: isFloor },
] as const
function isFloor(i: InventoryItem) { return i.itemId.startsWith('world.decor.floor_tile_') }

const tab = ref<(typeof TABS)[number]['id']>('furniture')
const error = ref('')
const shown = computed(() => bag.items.filter(TABS.find((t) => t.id === tab.value)!.match))
const draft = computed(() => room.draft!)

/** How many copies are still free to put in: owned minus what already stands in the room (a floor counts as one). */
function left(i: InventoryItem): number {
  const used = draft.value.placements.filter((p) => p.itemId === i.itemId).length + (draft.value.floorItemId === i.itemId ? 1 : 0)
  return i.quantity - used
}
const nameOf = (id: string) => bag.items.find((i) => i.itemId === id)?.name ?? game.assets[id]?.name ?? ''
const selectedName = computed(() => (room.selected >= 0 ? nameOf(draft.value.placements[room.selected]?.itemId ?? '') : ''))

function pick(i: InventoryItem) {
  error.value = ''
  if (isFloor(i)) return room.setFloor(draft.value.floorItemId === i.itemId ? null : i.itemId)
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
  <div v-if="room.draft" class="editor">
    <p v-if="message" class="notice" role="alert">{{ message }}</p>
    <p class="hint" role="status">{{ selectedName ? `已選取「${selectedName}」，直接在小屋裡拖曳移動。` : '點下面的道具放進小屋；點小屋裡的東西可以選取，拖曳移動。' }}</p>
    <div class="tabs" role="tablist" aria-label="分類">
      <button v-for="t in TABS" :key="t.id" type="button" role="tab" :aria-selected="tab === t.id" :class="{ on: tab === t.id }" @click="tab = t.id">{{ t.label }}</button>
    </div>
    <p v-if="!shown.length" class="empty">這一類還沒有道具。</p>
    <ul v-else class="items" aria-label="可擺放的道具">
      <li v-for="i in shown" :key="i.inventoryId">
        <button type="button" class="item" :class="[i.rarity, { used: tab === 'floor' ? draft.floorItemId === i.itemId : left(i) < 1 }]"
          :aria-label="tab === 'floor' ? `${i.name}，${draft.floorItemId === i.itemId ? '使用中，再按一次還原' : '點一下換上'}` : `${i.name}，還有 ${left(i)} 個可以擺`" @click="pick(i)">
          <img v-if="game.url(i.assetKey)" :src="game.url(i.assetKey)" alt="" draggable="false" class="pixelated" />
          <span class="nm">{{ i.name }}</span>
          <em v-if="tab !== 'floor'" class="qty">{{ left(i) }}/{{ i.quantity }}</em>
          <b v-else-if="draft.floorItemId === i.itemId" class="on-tag">使用中</b>
        </button>
      </li>
    </ul>
    <div class="acts">
      <button type="button" class="ghost" :disabled="room.selected < 0" @click="room.remove(room.selected)">收起選取的</button>
      <button v-if="tab === 'floor'" type="button" class="ghost" :disabled="!draft.floorItemId" @click="room.setFloor(null)">還原地板</button>
      <button type="button" class="ghost" @click="room.cancel()">取消</button>
      <button type="button" class="primary" :disabled="saveOp.loading.value" @click="saveOp.submit()">{{ saveOp.loading.value ? '儲存中…' : '完成' }}</button>
    </div>
  </div>
</template>

<style scoped>
.editor { display: grid; gap: 0.5rem; }
.notice { margin: 0; padding: 0.5rem 0.75rem; color: var(--c-error); background: var(--c-error-soft); border: 2px solid currentColor; border-radius: 8px; font-size: 0.85rem; }
.hint, .empty { margin: 0; color: var(--c-ink-soft); font-size: 0.82rem; }
.tabs { display: flex; gap: 0.4rem; }
.tabs button { min-height: 40px; padding: 0 0.8rem; font: inherit; font-size: 0.85rem; color: var(--c-ink); background: var(--c-paper); border: 2px solid var(--c-ink); border-radius: 8px; cursor: pointer; }
.tabs button.on { background: var(--c-yellow); font-weight: 700; }
.items { margin: 0; padding: 0; list-style: none; display: grid; grid-template-columns: repeat(4, 1fr); gap: 0.4rem; }
.item { position: relative; display: grid; justify-items: center; gap: 0.15rem; width: 100%; padding: 0.35rem 0.2rem; font: inherit; color: var(--c-ink); background: var(--c-cream); border: 3px solid var(--c-line); border-radius: 8px; cursor: pointer; }
.item.rare { border-color: #6fb1d6; }
.item.precious { border-color: #e0a830; }
.item.used { opacity: 0.45; }
.item img { width: 48px; height: 48px; object-fit: contain; }
.nm { font-size: 0.7rem; line-height: 1.2; text-align: center; }
.qty { position: absolute; top: 2px; right: 4px; font-style: normal; font-family: var(--font-pixel); font-size: 0.55rem; color: var(--c-ink-soft); }
.on-tag { position: absolute; top: -6px; left: -4px; padding: 0 0.3rem; font-size: 0.6rem; background: var(--c-yellow); border: 2px solid var(--c-ink); border-radius: 6px; }
.acts { display: flex; gap: 0.5rem; flex-wrap: wrap; }
.ghost { min-height: 40px; padding: 0 0.9rem; font: inherit; font-size: 0.9rem; color: var(--c-ink); background: var(--c-paper); border: 2px solid var(--c-ink); border-radius: 8px; cursor: pointer; }
.ghost:disabled { opacity: 0.4; cursor: not-allowed; }
.primary { min-height: 40px; padding: 0 1.2rem; font: inherit; font-weight: 700; color: var(--c-ink); background: var(--c-yellow); border: 3px solid var(--c-ink); border-radius: 8px; box-shadow: 2px 2px 0 var(--c-ink); cursor: pointer; }
.primary:disabled { opacity: 0.4; box-shadow: none; cursor: not-allowed; }
</style>
