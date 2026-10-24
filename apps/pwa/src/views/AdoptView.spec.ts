import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/api/http'
import { petApi } from '@/api/petApi'
import AdoptView from './AdoptView.vue'

vi.mock('@/api/petApi', () => ({ petApi: { mine: vi.fn(), create: vi.fn() } }))

async function setup() {
  setActivePinia(createPinia())
  const router = createRouter({
    history: createMemoryHistory(),
    routes: ['/', '/adopt'].map((path) => ({ path, name: path.slice(1) || 'home', component: { template: '<div/>' } })),
  })
  await router.push('/adopt')
  await router.isReady()
  return { w: mount(AdoptView, { global: { plugins: [router] } }), router }
}

beforeEach(() => vi.resetAllMocks())

describe('AdoptView', () => {
  it('needs a name before it calls the API', async () => {
    const { w } = await setup()
    await w.find('form').trigger('submit')
    expect(w.text()).toContain('請幫小夥伴取個名字')
    expect(petApi.create).not.toHaveBeenCalled()
  })

  it('adopts with the chosen look and goes home with the welcome message', async () => {
    vi.mocked(petApi.create).mockResolvedValue({ petId: 'p', name: '小栗', bodyVariant: 'star_01_mint' } as never)
    const { w, router } = await setup()
    await w.findAll('input[type=radio]')[1].setValue(true)
    await w.find('input[autocomplete=off]').setValue('  小栗 ')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(petApi.create).toHaveBeenCalledWith({ name: '小栗', bodyVariant: 'star_01_mint' })
    expect(router.currentRoute.value.name).toBe('home')
    expect(router.currentRoute.value.query.welcome).toBe('1')
  })

  it('"幫我想一個" fills a valid name', async () => {
    const { w } = await setup()
    await w.find('button.idea').trigger('click')
    const value = (w.find('input[autocomplete=off]').element as HTMLInputElement).value
    expect(value.length).toBeGreaterThan(0)
    expect(value.length).toBeLessThanOrEqual(12)
  })

  it('shows the API message for server-side name errors', async () => {
    vi.mocked(petApi.create).mockRejectedValue(new ApiError(422, 'VALIDATION_FAILED', '請檢查', 'r', { name: ['名字太長。'] }))
    const { w } = await setup()
    await w.find('input[autocomplete=off]').setValue('小栗')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(w.find('.err').text()).toContain('名字太長')
  })

  it('when another tab already adopted (409) it just goes home', async () => {
    vi.mocked(petApi.create).mockRejectedValue(new ApiError(409, 'PET_ALREADY_EXISTS', '你已經有一隻小夥伴了。'))
    vi.mocked(petApi.mine).mockResolvedValue({ petId: 'p', name: '先到的', bodyVariant: 'star_01_cream' } as never)
    const { w, router } = await setup()
    await w.find('input[autocomplete=off]').setValue('小栗')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(router.currentRoute.value.name).toBe('home')
  })
})
