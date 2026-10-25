import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from './http'
import { petApi } from './petApi'

function stub(status: number, body: unknown) {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: status < 400,
    status,
    text: async () => JSON.stringify(body),
    json: async () => body,
  }))
}

afterEach(() => vi.unstubAllGlobals())

describe('petApi', () => {
  it('treats 404 as "no pet yet"', async () => {
    stub(404, { code: 'NOT_FOUND', message: 'x', requestId: 'r' })
    expect(await petApi.mine()).toBeNull()
  })

  it('returns the pet snapshot', async () => {
    stub(200, { petId: 'p', name: '小栗' })
    expect(await petApi.mine()).toMatchObject({ name: '小栗' })
  })

  it('still throws for real failures', async () => {
    stub(500, { code: 'INTERNAL_ERROR', message: 'x', requestId: 'r' })
    await expect(petApi.mine()).rejects.toBeInstanceOf(ApiError)
  })
})
