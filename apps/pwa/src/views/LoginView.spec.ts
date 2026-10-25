import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { authApi } from '@/api/authApi'
import { ApiError } from '@/api/http'
import LoginView from './LoginView.vue'

vi.mock('@/api/authApi', () => ({ authApi: { login: vi.fn(), me: vi.fn(), resendConfirmation: vi.fn() } }))

async function setup(query = '') {
  setActivePinia(createPinia())
  const router = createRouter({
    history: createMemoryHistory(),
    routes: ['/', '/login', '/register', '/forgot-password'].map((path) => ({ path, name: path.slice(1) || 'home', component: { template: '<div/>' } })),
  })
  await router.push('/login' + query)
  await router.isReady()
  const w = mount(LoginView, { global: { plugins: [router] } })
  return { w, router }
}

const fill = async (w: Awaited<ReturnType<typeof setup>>['w'], email: string, password: string) => {
  await w.find('input[type=email]').setValue(email)
  await w.find('input[autocomplete=current-password]').setValue(password)
}

beforeEach(() => vi.resetAllMocks())

describe('LoginView', () => {
  it('blocks an invalid e-mail before calling the API', async () => {
    const { w } = await setup()
    await fill(w, 'nope', 'x')
    await w.find('form').trigger('submit')
    expect(w.text()).toContain('Email 格式不正確')
    expect(authApi.login).not.toHaveBeenCalled()
  })

  it('signs in and follows a safe redirect', async () => {
    vi.mocked(authApi.login).mockResolvedValue({ userId: 'u', email: 'a@example.com', displayName: 'x', emailConfirmed: true })
    const { w, router } = await setup('?redirect=/')
    await fill(w, 'a@example.com', 'Passw0rd-123')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(authApi.login).toHaveBeenCalledWith({ email: 'a@example.com', password: 'Passw0rd-123' })
    expect(router.currentRoute.value.name).toBe('home')
  })

  it('shows the API message and offers a resend when the e-mail is not confirmed', async () => {
    vi.mocked(authApi.login).mockRejectedValue(new ApiError(403, 'EMAIL_NOT_CONFIRMED', '請先到信箱點擊確認連結。'))
    vi.mocked(authApi.resendConfirmation).mockResolvedValue(undefined)
    const { w } = await setup()
    await fill(w, 'a@example.com', 'Passw0rd-123')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(w.text()).toContain('請先到信箱點擊確認連結')
    await w.find('button.linkish').trigger('click')
    await flushPromises()
    expect(authApi.resendConfirmation).toHaveBeenCalledWith('a@example.com')
    expect(w.text()).toContain('已重新寄出確認信')
  })

  it('ignores a second submit while the first is still running', async () => {
    let release!: () => void
    vi.mocked(authApi.login).mockReturnValue(new Promise((r) => (release = () => r({ userId: 'u', email: 'a', displayName: 'x', emailConfirmed: true }))))
    const { w } = await setup()
    await fill(w, 'a@example.com', 'Passw0rd-123')
    await w.find('form').trigger('submit')
    await w.find('form').trigger('submit')
    expect(authApi.login).toHaveBeenCalledTimes(1)
    expect(w.find('button[type=submit]').attributes('disabled')).toBeDefined()
    release()
    await flushPromises()
  })
})
