import type { Page, Route } from '@playwright/test'

export const USER = { userId: '11111111-1111-1111-1111-111111111111', email: 'a@example.com', displayName: '小栗', emailConfirmed: true }

type Handler = (route: Route, body: any) => Promise<void> | void
export const json = (route: Route, status: number, body?: unknown) =>
  route.fulfill({ status, contentType: 'application/json', body: body === undefined ? '' : JSON.stringify(body) })

/** Mock the API. `session` flips to the user after a successful login so /me behaves like the real cookie session. */
export async function mockApi(page: Page, handlers: Record<string, Handler> = {}) {
  const state = { signedIn: false, calls: [] as { path: string; body: any; csrf: string | undefined }[] }
  await page.route('**/api/v1/**', async (route) => {
    const req = route.request()
    const path = new URL(req.url()).pathname.replace('/api/v1', '')
    const body = req.postData() ? JSON.parse(req.postData()!) : undefined
    state.calls.push({ path, body, csrf: req.headers()['x-requested-with'] })
    const h = handlers[path]
    if (h) return h(route, body)
    if (path === '/auth/me') return state.signedIn ? json(route, 200, USER) : json(route, 401, { code: 'AUTH_REQUIRED', message: '請先登入。', requestId: 'r' })
    if (path === '/auth/login') {
      state.signedIn = true
      return json(route, 200, USER)
    }
    if (path === '/auth/logout') {
      state.signedIn = false
      return json(route, 204)
    }
    return json(route, 404, { code: 'NOT_FOUND', message: '找不到資源。', requestId: 'r' })
  })
  return state
}
