import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, request } from './http'

function mockFetch(res: { ok?: boolean; status?: number; jsonBody?: unknown; textBody?: string }) {
  const fn = vi.fn().mockResolvedValue({
    ok: res.ok ?? true,
    status: res.status ?? 200,
    text: async () => res.textBody ?? '',
    json: async () => {
      if (res.jsonBody === undefined) throw new Error('not json')
      return res.jsonBody
    },
  })
  vi.stubGlobal('fetch', fn)
  return fn
}

afterEach(() => vi.unstubAllGlobals())

describe('request', () => {
  it('sends the CSRF header and JSON on unsafe calls, same-origin cookies always', async () => {
    const fn = mockFetch({ textBody: '' })
    await request('POST', '/auth/login', { a: 1 })
    const [url, init] = fn.mock.calls[0]
    expect(url).toBe('/api/v1/auth/login')
    expect(init.credentials).toBe('same-origin')
    expect(init.headers['X-Requested-With']).toBe('pixelmate')
    expect(init.headers['Content-Type']).toBe('application/json')
    expect(init.body).toBe('{"a":1}')
  })

  it('does not add the CSRF header to GET', async () => {
    const fn = mockFetch({ textBody: '{"ok":true}' })
    expect(await request('GET', '/auth/me')).toEqual({ ok: true })
    expect(fn.mock.calls[0][1].headers['X-Requested-With']).toBeUndefined()
  })

  it('turns the API error envelope into ApiError with field errors', async () => {
    mockFetch({ ok: false, status: 422, jsonBody: { code: 'VALIDATION_FAILED', message: '請檢查', requestId: 'req-1', errors: { password: ['太短'] } } })
    const e = await request('POST', '/auth/register', {}).catch((x) => x)
    expect(e).toBeInstanceOf(ApiError)
    expect(e).toMatchObject({ status: 422, code: 'VALIDATION_FAILED', requestId: 'req-1', fieldErrors: { password: ['太短'] } })
  })

  it('survives non-JSON error pages (proxy 502)', async () => {
    mockFetch({ ok: false, status: 502 })
    const e = await request('GET', '/auth/me').catch((x) => x)
    expect(e).toMatchObject({ status: 502, code: 'INTERNAL_ERROR' })
  })

  it('reports network failures as NETWORK', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('fail')))
    const e = await request('GET', '/auth/me').catch((x) => x)
    expect(e).toMatchObject({ status: 0, code: 'NETWORK' })
  })
})
