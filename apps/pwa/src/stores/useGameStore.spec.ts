import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useGameStore } from './useGameStore'

beforeEach(() => setActivePinia(createPinia()))
afterEach(() => vi.unstubAllGlobals())

describe('useGameStore', () => {
  it('resolves assetKeys through the manifest, never hard-coded paths', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => ({ assets: { 'pet.star_01.face.happy': { path: 'sprites/pets/star_01/face_happy.png' } } }) }))
    const g = useGameStore()
    expect(g.url('pet.star_01.face.happy')).toBe('')          // before the manifest arrives
    await g.load()
    expect(g.url('pet.star_01.face.happy')).toBe('/game/sprites/pets/star_01/face_happy.png')
    expect(g.url('no.such.key')).toBe('')
  })

  it('loads once and survives a failed fetch', async () => {
    const f = vi.fn().mockRejectedValue(new Error('offline'))
    vi.stubGlobal('fetch', f)
    const g = useGameStore()
    await g.load()
    await g.load()
    expect(f).toHaveBeenCalledTimes(1)
    expect(g.ready).toBe(true)
    expect(g.url('x')).toBe('')
  })
})
