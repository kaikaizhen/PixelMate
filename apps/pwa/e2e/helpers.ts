import type { Page, Route } from '@playwright/test'

export const PET = {
  petId: '22222222-2222-2222-2222-222222222222', name: '小栗', bodyVariant: 'star_01_cream', level: 1, xp: 0, revision: 1,
  skills: { empathy: 0, creativity: 0, exploration: 0, wisdom: 0 }, remainingSkillPoints: 0, equipment: { hat: null, outfit: null },
  world: { mood: 'happy', scene: 'home', weather: 'sunny' }, updatedAt: '2026-10-25T00:00:00Z',
}

export const PET_L4 = { ...PET, level: 4, revision: 5, remainingSkillPoints: 3 }

export const BAG = [
  { inventoryId: 'i1', itemId: 'equip.hat.straw_post', name: '郵局草帽', category: 'hat', rarity: 'common', slot: 'head', assetKey: 'equip.hat.straw_post', quantity: 1, equipped: false },
  { inventoryId: 'i2', itemId: 'equip.costume.dog', name: '小狗布偶裝', category: 'costume', rarity: 'precious', slot: 'costume', assetKey: 'equip.costume.dog', quantity: 1, equipped: false },
  { inventoryId: 'i3', itemId: 'equip.feet.sneaker_picnic', name: '野餐帆布鞋', category: 'shoes', rarity: 'rare', slot: 'feet', assetKey: 'equip.feet.sneaker_picnic', quantity: 2, equipped: false },
  { inventoryId: 'i4', itemId: 'world.furniture.bed_garden', name: '花園單人床', category: 'furniture', rarity: 'rare', slot: null, assetKey: 'world.furniture.bed_garden', quantity: 1, equipped: false },
]

export const USER = { userId: '11111111-1111-1111-1111-111111111111', email: 'a@example.com', displayName: '小栗', emailConfirmed: true }

type Handler = (route: Route, body: any) => Promise<void> | void
export const json = (route: Route, status: number, body?: unknown) =>
  route.fulfill({ status, contentType: 'application/json', body: body === undefined ? '' : JSON.stringify(body) })

/** Mock the API. `session` flips to the user after a successful login so /me behaves like the real cookie session. */
export async function mockApi(page: Page, handlers: Record<string, Handler> = {}, opts: { pet?: typeof PET | null; bag?: typeof BAG } = {}) {
  const state = { signedIn: false, bag: structuredClone(opts.bag ?? []), pet: opts.pet === undefined ? (PET as typeof PET | null) : opts.pet, room: { revision: 1, floorItemId: null as string | null, placements: [{ placementId: 'p-table', itemId: 'world.furniture.table_garden', x: 150, y: 100 }] as any[] }, location: { latitude: 25, longitude: 121.5, timeZone: 'Asia/Taipei', label: '臺北市', source: 'city', updatedAt: '2026-10-20T00:00:00Z' } as any, devices: [] as any[], friends: [] as any[], inbox: [] as any[], sent: [] as any[], invites: 0, calls: [] as { path: string; body: any; csrf: string | undefined; key?: string }[] }
  await page.route('**/api/v1/**', async (route) => {
    const req = route.request()
    const path = new URL(req.url()).pathname.replace('/api/v1', '')
    const body = req.postData() ? JSON.parse(req.postData()!) : undefined
    state.calls.push({ path, body, csrf: req.headers()['x-requested-with'], key: req.headers()['idempotency-key'] })
    const h = handlers[path]
    if (h) return h(route, body)
    if (path === '/auth/me') return state.signedIn ? json(route, 200, USER) : json(route, 401, { code: 'AUTH_REQUIRED', message: '請先登入。', requestId: 'r' })
    if (path === '/auth/login') {
      state.signedIn = true
      return json(route, 200, USER)
    }
    if (path === '/pets/me') return state.pet ? json(route, 200, state.pet) : json(route, 404, { code: 'NOT_FOUND', message: '你還沒有小夥伴。', requestId: 'r' })
    if (path === '/room' && req.method() === 'GET') return json(route, 200, state.room)
    if (path === '/room' && req.method() === 'PUT') {
      if (body.expectedRevision !== state.room.revision) return json(route, 409, { code: 'REVISION_CONFLICT', message: 'x', requestId: 'r' })
      state.room = { revision: state.room.revision + 1, floorItemId: body.floorItemId, placements: body.placements.map((p: any, i: number) => ({ placementId: `p${state.room.revision}-${i}`, ...p })) }
      return json(route, 200, state.room)
    }
    if (path === '/weather/cities') return json(route, 200, { cities: [
      { id: 'tpe', name: '臺北市', latitude: 25.04, longitude: 121.56, timeZone: 'Asia/Taipei', group: 'taiwan' },
      { id: 'khh', name: '高雄市', latitude: 22.63, longitude: 120.3, timeZone: 'Asia/Taipei', group: 'taiwan' },
      { id: 'tyo', name: '東京', latitude: 35.68, longitude: 139.69, timeZone: 'Asia/Tokyo', group: 'world' }] })
    if (path === '/me/location' && req.method() === 'GET') return json(route, 200, { location: state.location })
    if (path === '/me/location' && req.method() === 'PUT') {
      state.location = { ...body, latitude: Math.round(body.latitude * 10) / 10, longitude: Math.round(body.longitude * 10) / 10, updatedAt: new Date().toISOString() }
      if (state.pet) (state.pet as any).world = { ...(state.pet as any).world, weather: body.timeZone === 'Asia/Tokyo' ? 'snow' : 'shower', temperatureC: 18.4, weatherSource: body.timeZone === 'Asia/Tokyo' ? 'open-meteo' : 'cwa', timeZone: body.timeZone }
      return json(route, 200, state.location)
    }
    if (path === '/devices' && req.method() === 'GET') return json(route, 200, { devices: state.devices })
    if (path === '/devices/pair') {
      if (body.code.toUpperCase() !== 'ABCD-2345') return json(route, 404, { code: 'NOT_FOUND', message: '找不到這組配對碼，或已經過期了。', requestId: 'r' })
      if (state.devices.length >= 3) return json(route, 422, { code: 'INVALID_STATE', message: '一隻小夥伴最多配對 3 台裝置，請先移除不用的。', requestId: 'r' })
      const d = { deviceId: 'd' + (state.devices.length + 1), petId: 'p', model: 'Waveshare-AMOLED-2.16', name: body.name || 'Waveshare-AMOLED-2.16', state: 'waiting', createdAt: new Date().toISOString(), activatedAt: null, lastSeenAt: null }
      state.devices.push(d)
      return json(route, 200, d)
    }
    const dev = path.match(/^\/devices\/([^/]+)$/)
    if (dev) {
      const d = state.devices.find((x) => x.deviceId === dev[1])
      if (!d) return json(route, 404, { code: 'NOT_FOUND', message: '找不到這台裝置。', requestId: 'r' })
      if (req.method() === 'PUT') d.name = body.name
      else state.devices = state.devices.filter((x) => x !== d)
      return json(route, 204)
    }
    // ---- friends and letters: a small stateful copy of the server rules ----
    if (path === '/pairing/relationships') return json(route, 200, { relationships: state.friends })
    if (path === '/pairing/invites' && req.method() === 'POST') return json(route, 201, { inviteId: 'inv' + ++state.invites, code: 'ABCD-2345', kind: body.kind, expiresAt: new Date(Date.now() + 86_400_000).toISOString() })
    if (path === '/pairing/preview') return body.code.toUpperCase() === 'ABCD-2345' ? json(route, 200, { inviterName: '阿福', inviterPetName: '麻糬', kind: 'friend', expiresAt: '2026-10-26T00:00:00Z' }) : json(route, 404, { code: 'NOT_FOUND', message: '找不到這組邀請碼，或已經過期了。', requestId: 'r' })
    if (path === '/pairing/accept') {
      const rel = { relationshipId: 'r1', kind: 'friend', since: new Date().toISOString(), other: { userId: 'u-fu', displayName: '阿福', petName: '麻糬', bodyVariant: 'star_01_mint' } }
      state.friends = [rel]
      return json(route, 200, rel)
    }
    if (path.startsWith('/pairing/relationships/') && req.method() === 'DELETE') { state.friends = []; return json(route, 204) }
    if (path === '/mail/unread') return json(route, 200, { unread: state.inbox.filter((l) => !l.readAt).length })
    if (path === '/mail' && req.method() === 'GET') {
      const sent = new URL(req.url()).searchParams.get('box') === 'sent'
      return json(route, 200, { letters: sent ? state.sent : state.inbox, unread: state.inbox.filter((l) => !l.readAt).length, nextCursor: null })
    }
    if (path === '/mail' && req.method() === 'POST') {
      if (!state.friends.length) return json(route, 403, { code: 'NOT_CONNECTED', message: '你們還不是好友，不能寄信。', requestId: 'r' })
      state.sent = [{ letterId: 's' + (state.sent.length + 1), otherUserId: body.recipientUserId, otherDisplayName: state.friends[0].other.displayName, preview: body.body.slice(0, 30), sentAt: new Date().toISOString(), deliverAt: new Date(Date.now() + 600_000).toISOString(), delivered: false, readAt: null, hasGift: !!body.giftItemId, paperItemId: body.paperItemId ?? null }, ...state.sent]
      return json(route, 201, { letterId: 's1' })
    }
    const mailId = path.match(/^\/mail\/([^/]+)(\/read)?$/)
    if (mailId) {
      const l = state.inbox.find((x) => x.letterId === mailId[1])
      if (!l) return json(route, 404, { code: 'NOT_FOUND', message: '找不到這封信。', requestId: 'r' })
      if (mailId[2]) { l.readAt ??= new Date().toISOString(); return json(route, 200, { readAt: l.readAt, gift: l.gift ?? null, giftGranted: !!l.gift }) }
      if (req.method() === 'DELETE') { state.inbox = state.inbox.filter((x) => x !== l); return json(route, 204) }
      return json(route, 200, { letterId: l.letterId, senderUserId: l.otherUserId, senderDisplayName: l.otherDisplayName, recipientUserId: 'me', body: l.body, sentAt: l.sentAt, deliverAt: l.deliverAt, readAt: l.readAt, paperItemId: null, gift: l.gift ?? null, mine: false })
    }
    if (path === '/pets/me/events') return json(route, 200, { events: [] })
    if (path === '/inventory') return json(route, 200, { items: state.bag })
    if (path === '/pets/me/equipment' && state.pet) {                       // a small copy of the server rules
      const p = state.pet as typeof PET & { equipment: Record<string, string | null> }
      if (body.expectedRevision !== p.revision) return json(route, 409, { code: 'REVISION_CONFLICT', message: 'x', requestId: 'r' })
      if (body.itemId) {
        const item = state.bag.find((i) => i.itemId === body.itemId)
        if (!item) return json(route, 404, { code: 'NOT_FOUND', message: '背包裡沒有這個道具。', requestId: 'r' })
        if (item.slot !== body.slot) return json(route, 422, { code: 'INVALID_STATE', message: '這個道具不能穿在這個位置。', requestId: 'r' })
        if (body.slot === 'costume') p.equipment.head = null
        if (body.slot === 'head') p.equipment.costume = null
      }
      p.equipment = { ...p.equipment, [body.slot]: body.itemId }
      p.revision += 1
      state.bag.forEach((i) => (i.equipped = Object.values(p.equipment).includes(i.itemId)))
      return json(route, 200, { revision: p.revision, equipment: p.equipment })
    }
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
