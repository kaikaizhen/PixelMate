import type { Page, Route } from '@playwright/test'

export const PET = {
  petId: '22222222-2222-2222-2222-222222222222', name: '小栗', bodyVariant: 'star_01_cream', level: 1, xp: 0, revision: 1,
  skills: { empathy: 0, creativity: 0, exploration: 0, wisdom: 0 }, remainingSkillPoints: 0, equipment: { hat: null, outfit: null },
  world: { mood: 'happy', scene: 'home', weather: 'sunny' }, updatedAt: '2026-10-25T00:00:00Z',
}

export const PET_L4 = { ...PET, level: 4, revision: 5, remainingSkillPoints: 3 }

export const USER = { userId: '11111111-1111-1111-1111-111111111111', email: 'a@example.com', displayName: '小栗', emailConfirmed: true }

type Handler = (route: Route, body: any) => Promise<void> | void
export const json = (route: Route, status: number, body?: unknown) =>
  route.fulfill({ status, contentType: 'application/json', body: body === undefined ? '' : JSON.stringify(body) })

/** Mock the API. `session` flips to the user after a successful login so /me behaves like the real cookie session. */
export async function mockApi(page: Page, handlers: Record<string, Handler> = {}, opts: { pet?: typeof PET | null } = {}) {
  const state = { signedIn: false, pet: opts.pet === undefined ? (PET as typeof PET | null) : opts.pet, calls: [] as { path: string; body: any; csrf: string | undefined }[] }
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
    if (path === '/pets/me') return state.pet ? json(route, 200, state.pet) : json(route, 404, { code: 'NOT_FOUND', message: '你還沒有小夥伴。', requestId: 'r' })
    if (path === '/pets/me/skills/allocate' && state.pet) {              // a tiny copy of the server rules
      const p = state.pet
      const used = Object.values(p.skills).reduce((a, b) => a + b, 0)
      if (body.expectedRevision !== p.revision) return json(route, 409, { code: 'REVISION_CONFLICT', message: '小夥伴的資料已經在別的地方更新了。', requestId: 'r' })
      if (body.pointsToAdd > p.level - 1 - used) return json(route, 422, { code: 'INSUFFICIENT_POINTS', message: '沒有可以分配的技能點數。', requestId: 'r' })
      p.skills = { ...p.skills, [body.skillKey]: (p.skills as Record<string, number>)[body.skillKey] + body.pointsToAdd }
      p.revision += 1
      p.remainingSkillPoints = p.level - 1 - used - body.pointsToAdd
      return json(route, 200, { revision: p.revision, skills: p.skills, remainingSkillPoints: p.remainingSkillPoints })
    }
    if (path === '/pets/me/skills/reset' && state.pet) {
      const p = state.pet
      if (body.expectedRevision !== p.revision) return json(route, 409, { code: 'REVISION_CONFLICT', message: 'x', requestId: 'r' })
      p.skills = { empathy: 0, creativity: 0, exploration: 0, wisdom: 0 }
      p.revision += 1
      p.remainingSkillPoints = p.level - 1
      return json(route, 200, { revision: p.revision, skills: p.skills, remainingSkillPoints: p.remainingSkillPoints })
    }
    if (path === '/pets' && req.method() === 'POST') {
      state.pet = { ...PET, name: body.name, bodyVariant: body.bodyVariant }
      return json(route, 201, state.pet)
    }
    if (path === '/auth/logout') {
      state.signedIn = false
      return json(route, 204)
    }
    return json(route, 404, { code: 'NOT_FOUND', message: '找不到資源。', requestId: 'r' })
  })
  return state
}
