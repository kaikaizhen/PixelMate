import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { authApi } from '@/api/authApi'
import { petApi } from '@/api/petApi'
import { useAuthStore } from './useAuthStore'
import { usePetStore } from './usePetStore'

vi.mock('@/api/authApi', () => ({ authApi: { logout: vi.fn(), login: vi.fn(), me: vi.fn(), register: vi.fn() } }))
vi.mock('@/api/petApi', () => ({ petApi: { mine: vi.fn(), create: vi.fn() } }))

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
})
