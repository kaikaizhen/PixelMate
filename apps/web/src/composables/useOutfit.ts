import { computed, ref } from 'vue'
import type { DressItem, DressSlot } from '@/types/dress.types'

// 同一欄位只能穿一件；再點一次同一件即脫下。官網示範用，不儲存。
export function useOutfit(items: readonly DressItem[]) {
  const worn = ref<Record<DressSlot, DressItem['id'] | null>>({ hat: null, neck: null, feet: null })
  const changeCount = ref(0)

  function toggle(item: DressItem) {
    worn.value[item.slot] = worn.value[item.slot] === item.id ? null : item.id
    changeCount.value++
  }

  function clear() {
    worn.value = { hat: null, neck: null, feet: null }
    changeCount.value++
  }

  const isWearing = (item: DressItem) => worn.value[item.slot] === item.id
  const wornItems = computed(() => items.filter(isWearing))

  return { worn, wornItems, changeCount, toggle, clear, isWearing }
}
