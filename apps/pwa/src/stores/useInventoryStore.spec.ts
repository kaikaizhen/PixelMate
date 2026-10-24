import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/api/http'
import { petApi } from '@/api/petApi'
import type { InventoryItem, Pet } from '@/types/pet.types'
import { useAuthStore } from './useAuthStore'
import { useInventoryStore } from './useInventoryStore'
import { usePetStore } from './usePetStore'

vi.mock('@/api/authApi', () => ({ authApi: { logout: vi.fn().mockResolvedValue(undefined), login: vi.fn(), me: vi.fn(), register: vi.fn() } }))
vi.mock('@/api/petApi', () => ({ petApi: { mine: vi.fn(), create: vi.fn(), allocate: vi.fn(), resetSkills: vi.fn(), devLevelUp: vi.fn(), inventory: vi.fn(), equip: vi.fn(), devGrant: vi.fn() } }))

const item = (id: string, slot: string | null, equipped = false): InventoryItem =>
  ({ inventoryId: id, itemId: id, name: id, category: 'hat', rarity: 'common', slot, assetKey: id, quantity: 1, equipped })
const PET = { petId: 'p', revision: 3, equipment: { head: null, costume: null } } as unknown as Pet

beforeEach(() => {
  setActivePinia(createPinia())
  vi.resetAllMocks()
})

describe('useInventoryStore', () => {
  it('loads the bag once unless forced', async () => {
    vi.mocked(petApi.inventory).mockResolvedValue([item('a', 'head')])
    const s = useInventoryStore()
    await s.load()
    await s.load()
    expect(petApi.inventory).toHaveBeenCalledTimes(1)
    await s.load(true)
    expect(petApi.inventory).toHaveBeenCalledTimes(2)
  })

  it('wearing sends the pet revision and marks exactly the worn items', async () => {
    const pets = usePetStore()
    pets.pet = PET
    const s = useInventoryStore()
    s.items = [item('hat', 'head'), item('dog', 'costume', true)]
    vi.mocked(petApi.equip).mockResolvedValue({ revision: 4, equipment: { head: 'hat', costume: null } })

    await s.equip(s.items[0], 'head')
    expect(petApi.equip).toHaveBeenCalledWith('head', 'hat', 3)
    expect(pets.pet?.revision).toBe(4)
    expect(pets.pet?.equipment.head).toBe('hat')
    expect(s.items.map((i) => i.equipped)).toEqual([true, false])        // the costume came off server-side
  })

  it('taking off sends a null item', async () => {
    const pets = usePetStore()
    pets.pet = PET
    const s = useInventoryStore()
    s.items = [item('hat', 'head', true)]
    vi.mocked(petApi.equip).mockResolvedValue({ revision: 4, equipment: { head: null } })
    await s.equip(null, 'head')
    expect(petApi.equip).toHaveBeenCalledWith('head', null, 3)
    expect(s.items[0].equipped).toBe(false)
  })

  it('a revision conflict re-reads the pet and reports the error', async () => {
    const pets = usePetStore()
    pets.pet = PET
    vi.mocked(petApi.equip).mockRejectedValue(new ApiError(409, 'REVISION_CONFLICT', 'x'))
    vi.mocked(petApi.mine).mockResolvedValue({ ...PET, revision: 9 })
    const s = useInventoryStore()
    s.items = [item('hat', 'head')]
    await expect(s.equip(s.items[0], 'head')).rejects.toMatchObject({ code: 'REVISION_CONFLICT' })
    expect(pets.pet?.revision).toBe(9)
  })

  it('signing out empties the bag', async () => {
    const s = useInventoryStore()
    s.items = [item('hat', 'head')]
    s.loaded = true
    await useAuthStore().logout()
    expect(s.items).toEqual([])
    expect(s.loaded).toBe(false)
  })
})
