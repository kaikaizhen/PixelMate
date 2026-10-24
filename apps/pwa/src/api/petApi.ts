import type { CreatePetInput, EquipResult, InventoryItem, Pet, SkillKey, SkillsResult } from '@/types/pet.types'
import { ApiError, request } from './http'

export const petApi = {
  /** The player's pet, or null when they have not adopted one yet (the API answers 404 NOT_FOUND). */
  async mine(): Promise<Pet | null> {
    try {
      return await request<Pet>('GET', '/pets/me')
    } catch (e) {
      if (e instanceof ApiError && e.status === 404) return null
      throw e
    }
  },
  create: (input: CreatePetInput) => request<Pet>('POST', '/pets', input),
  allocate: (skillKey: SkillKey, pointsToAdd: number, expectedRevision: number) =>
    request<SkillsResult>('POST', '/pets/me/skills/allocate', { skillKey, pointsToAdd, expectedRevision }),
  resetSkills: (expectedRevision: number) => request<SkillsResult>('POST', '/pets/me/skills/reset', { expectedRevision }),
  inventory: async () => (await request<{ items: InventoryItem[] }>('GET', '/inventory')).items,
  equip: (slot: string, itemId: string | null, expectedRevision: number) =>
    request<EquipResult>('POST', '/pets/me/equipment', { slot, itemId, expectedRevision }),
  /** Development builds only: put items in the bag (the API maps this only in Development). */
  devGrant: (itemId: string, quantity = 1) => request<{ items: InventoryItem[] }>('POST', '/dev/inventory/grant', { itemId, quantity }),
  /** Development builds only (the API maps this route only in Development). */
  devLevelUp: () => request<Pet>('POST', '/dev/pets/me/level-up'),
}
