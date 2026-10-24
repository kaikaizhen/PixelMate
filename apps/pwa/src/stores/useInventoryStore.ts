import { defineStore } from 'pinia'
import { ref } from 'vue'
import { petApi } from '@/api/petApi'
import type { InventoryItem } from '@/types/pet.types'
import { usePetStore } from './usePetStore'

export const useInventoryStore = defineStore('inventory', () => {
  const items = ref<InventoryItem[]>([])
  const loaded = ref(false)

  async function load(force = false): Promise<void> {
    if (loaded.value && !force) return
    items.value = await petApi.inventory()
    loaded.value = true
  }

  /** Wear an item, or take its slot off (itemId = null). The pet store keeps revision + equipment; this keeps the flags. */
  async function equip(item: InventoryItem | null, slot: string): Promise<void> {
    const r = await usePetStore().equip(slot, item?.itemId ?? null)
    items.value = items.value.map((i) => ({ ...i, equipped: Object.values(r.equipment).includes(i.itemId) }))
  }

  async function devGrant(itemIds: string[]): Promise<void> {
    let last: InventoryItem[] = items.value
    for (const id of itemIds) last = (await petApi.devGrant(id)).items
    items.value = last
    loaded.value = true
  }

  function clear() {
    items.value = []
    loaded.value = false
  }

  return { items, loaded, load, equip, devGrant, clear }
})
