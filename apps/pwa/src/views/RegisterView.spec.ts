import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { authApi } from '@/api/authApi'
import { ApiError } from '@/api/http'
import RegisterView from './RegisterView.vue'

vi.mock('@/api/authApi', () => ({ authApi: { register: vi.fn(), me: vi.fn() } }))

async function setup() {
  setActivePinia(createPinia())
  const router = createRouter({
    history: createMemoryHistory(),
    routes: ['/', '/login', '/register'].map((path) => ({ path, name: path.slice(1) || 'home', component: { template: '<div/>' } })),
  })
  await router.push('/register')
  await router.isReady()
  return mount(RegisterView, { global: { plugins: [router] } })
}

const fill = async (w: Awaited<ReturnType<typeof setup>>, name: string, email: string, password: string) => {
  await w.find('input[autocomplete=nickname]').setValue(name)
  await w.find('input[type=email]').setValue(email)
  await w.find('input[autocomplete=new-password]').setValue(password)
}

beforeEach(() => vi.resetAllMocks())

describe('RegisterView', () => {
  it('shows the live password checklist', async () => {
    const w = await setup()
    await w.find('input[autocomplete=new-password]').setValue('abc')
    expect(w.findAll('.rules li.ok')).toHaveLength(1)
    await w.find('input[autocomplete=new-password]').setValue('abcdefghij1')
    expect(w.findAll('.rules li.ok')).toHaveLength(3)
  })

  it('reports every invalid field and does not call the API', async () => {
    const w = await setup()
    await fill(w, '', 'bad', 'short')
    await w.find('form').trigger('submit')
    expect(w.text()).toContain('請輸入暱稱')
    expect(w.text()).toContain('Email 格式不正確')
    expect(w.text()).toContain('至少 10 個字元')
    expect(authApi.register).not.toHaveBeenCalled()
  })

  it('registers and tells the player to check the mailbox when confirmation is required', async () => {
    vi.mocked(authApi.register).mockResolvedValue({ userId: 'u', emailConfirmationRequired: true })
    const w = await setup()
    await fill(w, '小栗', 'a@example.com', 'Passw0rd-123')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(authApi.register).toHaveBeenCalledWith({ email: 'a@example.com', password: 'Passw0rd-123', displayName: '小栗' })
    expect(w.text()).toContain('確認信')
  })

  it('maps a duplicate e-mail to the API message', async () => {
    vi.mocked(authApi.register).mockRejectedValue(new ApiError(409, 'EMAIL_ALREADY_REGISTERED', '這個 Email 已經註冊過了。'))
    const w = await setup()
    await fill(w, '小栗', 'a@example.com', 'Passw0rd-123')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(w.find('[role=alert]').text()).toContain('已經註冊過')
  })

  it('shows server field errors next to the field', async () => {
    vi.mocked(authApi.register).mockRejectedValue(new ApiError(422, 'VALIDATION_FAILED', '請檢查', 'r', { email: ['Email 格式不正確。'] }))
    const w = await setup()
    await fill(w, '小栗', 'a@example.com', 'Passw0rd-123')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(w.find('.err').text()).toContain('Email 格式不正確')
  })
})
