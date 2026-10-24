import type { CreatePetInput, Pet } from '@/types/pet.types'
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
}
