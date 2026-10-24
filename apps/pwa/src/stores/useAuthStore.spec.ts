import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { authApi } from '@/api/authApi'
import { ApiError } from '@/api/http'
import { useAuthStore } from './useAuthStore'

vi.mock('@/api/authApi', () => ({ authApi: { me: vi.fn(), login: vi.fn(), logout: vi.fn(), register: vi.fn() } }))

const user = { userId: 'u1', email: 'a@example.com', displayName: '小栗', emailConfirmed: true }

beforeEach(() => {
  setActivePinia(createPinia())
  vi.resetAllMocks()
})

describe('useAuthStore', () => {
  it('init restores the session once', async () => {
    vi.mocked(authApi.me).mockResolvedValue(user)
    const s = useAuthStore()
    await s.init()
    await s.init()
    expect(s.user).toEqual(user)
    expect(s.ready).toBe(true)
    expect(authApi.me).toHaveBeenCalledTimes(1)
  })

  it('init treats 401 and offline as signed out', async () => {
    vi.mocked(authApi.me).mockRejectedValue(new ApiError(401, 'AUTH_REQUIRED', 'no'))
    const s = useAuthStore()
    await s.init()
    expect(s.user).toBeNull()
    expect(s.ready).toBe(true)
  })

  it('login stores the user, logout clears it even when the request fails', async () => {
    vi.mocked(authApi.login).mockResolvedValue(user)
    const s = useAuthStore()
    await s.login({ email: 'a@example.com', password: 'x' })
    expect(s.user).toEqual(user)

    vi.mocked(authApi.logout).mockRejectedValue(new ApiError(0, 'NETWORK', 'offline'))
    await expect(s.logout()).rejects.toBeInstanceOf(ApiError)
    expect(s.user).toBeNull()
  })
})
