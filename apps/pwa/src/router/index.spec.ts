import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory } from 'vue-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { authApi } from '@/api/authApi'
import { petApi } from '@/api/petApi'
import { ApiError } from '@/api/http'
import { createAppRouter } from './index'

vi.mock('@/api/authApi', () => ({ authApi: { me: vi.fn() } }))
vi.mock('@/api/petApi', () => ({ petApi: { mine: vi.fn(), create: vi.fn() } }))

const USER = { userId: 'u', email: 'a@b.co', displayName: 'x', emailConfirmed: true }
const PET = { petId: 'p', name: '小栗', bodyVariant: 'star_01_cream', level: 1, xp: 0, revision: 1, skills: { empathy: 0, creativity: 0, exploration: 0, wisdom: 0 }, remainingSkillPoints: 0, equipment: {}, world: { mood: 'happy', scene: 'home', weather: 'sunny' }, updatedAt: '' }

beforeEach(() => {
  setActivePinia(createPinia())
  vi.resetAllMocks()
})

describe('route guards', () => {
  it('sends anonymous visitors from a private page to login', async () => {
    vi.mocked(authApi.me).mockRejectedValue(new ApiError(401, 'AUTH_REQUIRED', 'no'))
    const r = createAppRouter(createMemoryHistory())
    await r.push('/')
    expect(r.currentRoute.value.name).toBe('login')
  })

  it('keeps signed-in users out of the guest pages', async () => {
    vi.mocked(authApi.me).mockResolvedValue(USER)
    vi.mocked(petApi.mine).mockResolvedValue(PET)
    const r = createAppRouter(createMemoryHistory())
    await r.push('/login')
    expect(r.currentRoute.value.name).toBe('home')
  })

  it('sends a signed-in player without a pet to the adoption page, from anywhere private', async () => {
    vi.mocked(authApi.me).mockResolvedValue(USER)
    vi.mocked(petApi.mine).mockResolvedValue(null)
    const r = createAppRouter(createMemoryHistory())
    await r.push('/')
    expect(r.currentRoute.value.name).toBe('adopt')
  })

  it('keeps players who already have a pet away from the adoption page', async () => {
    vi.mocked(authApi.me).mockResolvedValue(USER)
    vi.mocked(petApi.mine).mockResolvedValue(PET)
    const r = createAppRouter(createMemoryHistory())
    await r.push('/adopt')
    expect(r.currentRoute.value.name).toBe('home')
  })

  it('asks anonymous visitors to sign in before the adoption page', async () => {
    vi.mocked(authApi.me).mockRejectedValue(new ApiError(401, 'AUTH_REQUIRED', 'no'))
    const r = createAppRouter(createMemoryHistory())
    await r.push('/adopt')
    expect(r.currentRoute.value.name).toBe('login')
    expect(petApi.mine).not.toHaveBeenCalled()
  })

  it('lets everyone open the e-mail link pages', async () => {
    vi.mocked(authApi.me).mockRejectedValue(new ApiError(401, 'AUTH_REQUIRED', 'no'))
    const r = createAppRouter(createMemoryHistory())
    await r.push('/reset-password?email=a@b.co&token=t')
    expect(r.currentRoute.value.name).toBe('reset-password')
  })
})
