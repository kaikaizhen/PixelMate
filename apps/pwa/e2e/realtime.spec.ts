import { expect, test } from '@playwright/test'
import { PET, mockApi } from './helpers'

const EQUIP = { hat: null, outfit: null, head: null, front: null, back: null, feet: null, hand: null, costume: null }
const RS = '\u001e'                      // SignalR's record separator

/** A tiny SignalR server over the page's WebSocket: answers the handshake, then lets the test push invocations. */
async function fakeHub(page: import('@playwright/test').Page) {
  let socket: import('@playwright/test').WebSocketRoute | null = null
  await page.route('**/api/v1/hub/negotiate**', (r) => r.fulfill({ json: { negotiateVersion: 1, connectionId: 'c1', connectionToken: 't1', availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text', 'Binary'] }] } }))
  await page.routeWebSocket(/\/api\/v1\/hub/, (ws) => {
    socket = ws
    ws.onMessage((m) => { if (String(m).includes('"protocol"')) ws.send('{}' + RS) })
  })
  return {
    push: (type: string, payload: object = {}, eventId = crypto.randomUUID()) =>
      socket!.send(JSON.stringify({ type: 1, target: 'pet.event', arguments: [{ seq: 1, eventId, type, occurredAt: new Date().toISOString(), payload }] }) + RS),
    connected: () => socket !== null,
  }
}

async function start(page: import('@playwright/test').Page, pet = {}) {
  const state = await mockApi(page, {}, { pet: { ...structuredClone(PET), equipment: { ...EQUIP }, ...pet } as typeof PET })
  const hub = await fakeHub(page)
  await page.goto('login')
  await page.getByLabel('Email').fill('a@example.com')
  await page.getByLabel('密碼').fill('Passw0rd-123')
  await page.getByRole('button', { name: '登入' }).click()
  await expect(page.getByRole('button', { name: '背包' })).toBeVisible()
  await expect.poll(() => hub.connected()).toBe(true)
  await page.waitForTimeout(300)             // handshake done
  return { state, hub }
}

test.describe('即時同步（SignalR）', () => {
  test('別的裝置換了裝備：收到 pet.updated 立刻重新讀取，小夥伴馬上換裝，不用等一分鐘', async ({ page }) => {
    const { state, hub } = await start(page)
    await expect(page.locator('button.pet img[data-item]')).toHaveCount(0)
    ;(state.pet as { equipment: Record<string, string | null> }).equipment = { ...EQUIP, head: 'equip.hat.straw_post' }
    hub.push('pet.updated', { reason: 'equipment' })
    await expect(page.locator('button.pet img[data-item="equip.hat.straw_post"]')).toHaveCount(1)
  })

  test('mail.received：信箱出現未讀數與提示；同一個事件重複送來只算一次', async ({ page }) => {
    const { state, hub } = await start(page)
    state.friends = [{ relationshipId: 'r1', kind: 'friend', since: '', other: { userId: 'u2', displayName: '阿福', petName: null, bodyVariant: null } }]
    state.inbox = [{ letterId: 'l1', otherUserId: 'u2', otherDisplayName: '阿福', preview: 'hi', sentAt: '', deliverAt: '2026-10-25T01:10:00', delivered: true, readAt: null, hasGift: false }]
    const id = crypto.randomUUID()
    hub.push('mail.received', { letterId: 'l1' }, id)
    hub.push('mail.received', { letterId: 'l1' }, id)
    await expect(page.getByText('郵差送來一封信！')).toBeVisible()
    await expect(page.getByRole('button', { name: '信箱，1 封未讀' })).toBeVisible()
    expect(state.calls.filter((c) => c.path === '/mail/unread').length).toBeLessThanOrEqual(3)
  })

  test('room.updated：另一台裝置布置了小屋，這台自動更新；正在布置的這台不會被打斷', async ({ page }) => {
    const { state, hub } = await start(page)
    await expect(page.locator('[data-placed="world.furniture.table_garden"]')).toBeVisible()
    state.room = { revision: 2, floorItemId: null, placements: [{ placementId: 'x', itemId: 'world.furniture.bed_garden', x: 40, y: 100 }] }
    hub.push('room.updated')
    await expect(page.locator('[data-placed="world.furniture.bed_garden"]')).toBeVisible()
    await expect(page.locator('[data-placed="world.furniture.table_garden"]')).toHaveCount(0)
  })

  test('連不上即時連線也沒關係：畫面照常，仍用焦點重新讀取', async ({ page }) => {
    await mockApi(page, {}, { pet: { ...structuredClone(PET), equipment: { ...EQUIP } } as typeof PET })     // no hub route: negotiate answers 404
    await page.goto('login')
    await page.getByLabel('Email').fill('a@example.com')
    await page.getByLabel('密碼').fill('Passw0rd-123')
    await page.getByRole('button', { name: '登入' }).click()
    await expect(page.getByRole('button', { name: '背包' })).toBeVisible()
    await page.route('**/api/v1/pets/me', (r) => (r.request().method() === 'GET' ? r.fulfill({ json: { ...structuredClone(PET), revision: 9, equipment: { ...EQUIP, head: 'equip.hat.straw_post' } } }) : r.fallback()))
    await page.evaluate(() => window.dispatchEvent(new Event('focus')))
    await expect(page.locator('button.pet img[data-item="equip.hat.straw_post"]')).toHaveCount(1)
  })
})
