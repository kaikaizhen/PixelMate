import { defineStore } from 'pinia'
import { ref } from 'vue'
import { petApi } from '@/api/petApi'
import type { CreatePetInput, Pet } from '@/types/pet.types'

export const usePetStore = defineStore('pet', () => {
  const pet = ref<Pet | null>(null)
  const loaded = ref(false) // true once /pets/me answered (a missing pet is a valid answer)

  /** Load the pet once per session; pass force to re-read (e.g. after a 409 on adopt). */
  async function load(force = false): Promise<Pet | null> {
    if (loaded.value && !force) return pet.value
    pet.value = await petApi.mine()
    loaded.value = true
    return pet.value
  }

  async function adopt(input: CreatePetInput): Promise<Pet> {
    pet.value = await petApi.create(input)
    loaded.value = true
    return pet.value
  }

  /** Forget everything (sign out / another account). */
  function clear() {
    pet.value = null
    loaded.value = false
  }

  return { pet, loaded, load, adopt, clear }
})
