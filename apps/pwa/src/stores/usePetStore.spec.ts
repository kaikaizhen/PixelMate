import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { authApi } from '@/api/authApi'
import { ApiError } from '@/api/http'
import { petApi } from '@/api/petApi'
import type { Pet } from '@/types/pet.types'
import { useAuthStore } from './useAuthStore'
import { usePetStore } from './usePetStore'

vi.mock('@/api/authApi', () => ({ authApi: { logout: vi.fn(), login: vi.fn(), me: vi.fn(), register: vi.fn() } }))
vi.mock('@/api/petApi', () => ({ petApi: { mine: vi.fn(), create: vi.fn(), allocate: vi.fn(), resetSkills: vi.fn(), devLevelUp: vi.fn() } }))

const PET = { petId: 'p', name: '小栗' } as never

beforeEach(() => {
  setActivePinia(createPinia())
  vi.resetAllMocks()
})

describe('usePetStore', () => {
  it('loads once and remembers a missing pet', async () => {
    vi.mocked(petApi.mine).mockResolvedValue(null)
    const s = usePetStore()
    expect(await s.load()).toBeNull()
    await s.load()
    expect(petApi.mine).toHaveBeenCalledTimes(1)
    await s.load(true)
    expect(petApi.mine).toHaveBeenCalledTimes(2)
  })

  it('adopt stores the new pet', async () => {
    vi.mocked(petApi.create).mockResolvedValue(PET)
    const s = usePetStore()
    await s.adopt({ name: '小栗', bodyVariant: 'star_01_cream' })
    expect(s.pet).toEqual(PET)
    expect(s.loaded).toBe(true)
  })

  it('signing out or in as someone else forgets the previous pet', async () => {
    const pets = usePetStore()
    const auth = useAuthStore()
    pets.pet = PET
    pets.loaded = true
    vi.mocked(authApi.logout).mockResolvedValue(undefined)
    await auth.logout()
    expect(pets.pet).toBeNull()
    expect(pets.loaded).toBe(false)

    pets.pet = PET
    pets.loaded = true
    vi.mocked(authApi.login).mockResolvedValue({ userId: 'u2', email: 'b@b.co', displayName: 'b', emailConfirmed: true })
    await auth.login({ email: 'b@b.co', password: 'x' })
    expect(pets.pet).toBeNull()
  })

  describe_skills()
})

function describe_skills() {
  const base = { petId: 'p', name: '小栗', level: 4, revision: 5, skills: { empathy: 0, creativity: 0, exploration: 0, wisdom: 0 }, remainingSkillPoints: 3 } as unknown as Pet

  it('allocate sends the current revision and applies the server result', async () => {
    const s = usePetStore()
    s.pet = base
    vi.mocked(petApi.allocate).mockResolvedValue({ revision: 6, skills: { empathy: 1, creativity: 0, exploration: 0, wisdom: 0 }, remainingSkillPoints: 2 })
    await s.allocate('empathy')
    expect(petApi.allocate).toHaveBeenCalledWith('empathy', 1, 5)
    expect(s.pet).toMatchObject({ revision: 6, remainingSkillPoints: 2, skills: { empathy: 1 } })
  })

  it('a revision conflict re-reads the pet and still reports the error', async () => {
    const s = usePetStore()
    s.pet = base
    vi.mocked(petApi.allocate).mockRejectedValue(new ApiError(409, 'REVISION_CONFLICT', 'x'))
    vi.mocked(petApi.mine).mockResolvedValue({ ...base, revision: 9 })
    await expect(s.allocate('wisdom')).rejects.toMatchObject({ code: 'REVISION_CONFLICT' })
    expect(s.pet?.revision).toBe(9)
  })

  it('other errors do not reload anything', async () => {
    const s = usePetStore()
    s.pet = base
    vi.mocked(petApi.allocate).mockRejectedValue(new ApiError(422, 'INSUFFICIENT_POINTS', 'x'))
    await expect(s.allocate('wisdom')).rejects.toBeInstanceOf(ApiError)
    expect(petApi.mine).not.toHaveBeenCalled()
  })

  it('reset sends the revision and applies the result', async () => {
    const s = usePetStore()
    s.pet = base
    vi.mocked(petApi.resetSkills).mockResolvedValue({ revision: 6, skills: { empathy: 0, creativity: 0, exploration: 0, wisdom: 0 }, remainingSkillPoints: 3 })
    await s.resetSkills()
    expect(petApi.resetSkills).toHaveBeenCalledWith(5)
    expect(s.pet?.revision).toBe(6)
  })
}
