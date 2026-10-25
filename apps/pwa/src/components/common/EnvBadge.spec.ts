import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import EnvBadge from './EnvBadge.vue'

describe('EnvBadge', () => {
  afterEach(() => vi.unstubAllEnvs())

  it('says so on a test server', () => {
    vi.stubEnv('VITE_ENV_LABEL', '測試服（會刪檔）')
    const w = mount(EnvBadge)
    expect(w.get('[data-testid="env-badge"]').text()).toBe('測試服（會刪檔）')
  })

  it('shows nothing on a real release', () => {
    vi.stubEnv('VITE_ENV_LABEL', '')
    expect(mount(EnvBadge).find('[data-testid="env-badge"]').exists()).toBe(false)
    vi.stubEnv('VITE_ENV_LABEL', '   ')
    expect(mount(EnvBadge).find('[data-testid="env-badge"]').exists()).toBe(false)
  })
})
