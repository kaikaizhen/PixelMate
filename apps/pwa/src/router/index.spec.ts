import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory } from 'vue-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { authApi } from '@/api/authApi'
import { ApiError } from '@/api/http'
import { createAppRouter } from './index'

vi.mock('@/api/authApi', () => ({ authApi: { me: vi.fn() } }))

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
    vi.mocked(authApi.me).mockResolvedValue({ userId: 'u', email: 'a@b.co', displayName: 'x', emailConfirmed: true })
    const r = createAppRouter(createMemoryHistory())
    await r.push('/login')
    expect(r.currentRoute.value.name).toBe('home')
  })

  it('lets everyone open the e-mail link pages', async () => {
    vi.mocked(authApi.me).mockRejectedValue(new ApiError(401, 'AUTH_REQUIRED', 'no'))
    const r = createAppRouter(createMemoryHistory())
    await r.push('/reset-password?email=a@b.co&token=t')
    expect(r.currentRoute.value.name).toBe('reset-password')
  })
})
