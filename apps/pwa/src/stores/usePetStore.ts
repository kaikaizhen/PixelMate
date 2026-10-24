import { defineStore } from 'pinia'
import { ref } from 'vue'
import { petApi } from '@/api/petApi'
import { ApiError } from '@/api/http'
import type { CreatePetInput, Pet, SkillKey, SkillsResult } from '@/types/pet.types'

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

  function apply(r: SkillsResult) {
    if (!pet.value) return
    pet.value = { ...pet.value, skills: r.skills, remainingSkillPoints: r.remainingSkillPoints, revision: r.revision }
  }

  /** Run a write that carries expectedRevision; on 409 re-read the pet so the screen shows what the server has. */
  async function guarded<T>(run: (revision: number) => Promise<T>): Promise<T> {
    try {
      return await run(pet.value!.revision)
    } catch (e) {
      if (e instanceof ApiError && e.code === 'REVISION_CONFLICT') await load(true)
      throw e
    }
  }

  async function allocate(key: SkillKey, points = 1): Promise<void> {
    apply(await guarded((rev) => petApi.allocate(key, points, rev)))
  }

  async function resetSkills(): Promise<void> {
    apply(await guarded((rev) => petApi.resetSkills(rev)))
  }

  async function devLevelUp(): Promise<void> {
    pet.value = await petApi.devLevelUp()
  }

  /** Forget everything (sign out / another account). */
  function clear() {
    pet.value = null
    loaded.value = false
  }

  return { pet, loaded, load, adopt, allocate, resetSkills, devLevelUp, clear }
})
