import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { roomApi } from '@/api/roomApi'
import { DEFAULT_FURNITURE } from '@/constants/defaultRoom'
import { MAX_PLACEMENTS, type Room, type RoomPlacement } from '@/types/room.types'

let seq = 0
const tempId = () => `new-${++seq}`

export const useRoomStore = defineStore('room', () => {
  const room = ref<Room | null>(null)
  const draft = ref<Room | null>(null)        // the layout being edited; null when not editing
  const selected = ref(-1)

  const editing = computed(() => draft.value !== null)
  /** What the scene draws: the draft while editing, else the saved room, else the starter table until the first answer arrives. */
  const view = computed<Room>(() =>
    draft.value ?? room.value ?? { revision: 0, floorItemId: null, placements: DEFAULT_FURNITURE.map((f) => ({ placementId: f.key, itemId: f.key, x: f.x, y: f.y })) })

  async function load(force = false): Promise<void> {
    if (room.value && !force) return
    const fresh = await roomApi.get()
    if (!draft.value) room.value = fresh          // never pull the floor from under an edit in progress
  }

  function startEdit() {
    if (!room.value) return
    draft.value = { ...room.value, placements: room.value.placements.map((p) => ({ ...p })) }
    selected.value = -1
  }

  function cancel() {
    draft.value = null
    selected.value = -1
  }

  function add(itemId: string, x = 100, y = 130): boolean {
    if (!draft.value || draft.value.placements.length >= MAX_PLACEMENTS) return false
    draft.value.placements.push({ placementId: tempId(), itemId, x, y })
    selected.value = draft.value.placements.length - 1
    return true
  }

  function move(index: number, x: number, y: number) {
    const p = draft.value?.placements[index]
    if (p) { p.x = x; p.y = y }
  }

  function remove(index: number) {
    draft.value?.placements.splice(index, 1)
    selected.value = -1
  }

  function setFloor(itemId: string | null) {
    if (draft.value) draft.value.floorItemId = itemId
  }

  /** Save the draft. On 409 (another device saved first) the saved room is re-read and the edit is dropped. */
  async function save(): Promise<void> {
    if (!draft.value) return
    try {
      room.value = await roomApi.save(draft.value.revision, draft.value.floorItemId, draft.value.placements)
      draft.value = null
      selected.value = -1
    } catch (e) {
      if ((e as { code?: string }).code === 'REVISION_CONFLICT') {
        draft.value = null
        selected.value = -1
        await load(true)
      }
      throw e
    }
  }

  function clear() {
    room.value = null
    draft.value = null
    selected.value = -1
  }

  return { room, draft, selected, editing, view, load, startEdit, cancel, add, move, remove, setFloor, save, clear }
})

export type { RoomPlacement }
