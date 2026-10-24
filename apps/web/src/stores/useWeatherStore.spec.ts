import { beforeEach, describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useWeatherStore } from './useWeatherStore'

describe('useWeatherStore', () => {
  beforeEach(() => setActivePinia(createPinia()))

  it('預設為晴天', () => {
    expect(useWeatherStore().scene).toBe('sunny')
  })

  it('可切換天氣', () => {
    const store = useWeatherStore()
    store.setScene('rain')
    expect(store.scene).toBe('rain')
  })
})
