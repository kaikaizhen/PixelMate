import { expect, test } from '@playwright/test'
import { PET, mockApi } from './helpers'

const EQUIP = { hat: null, outfit: null, head: null, front: null, back: null, feet: null, hand: null, costume: null }
const DEVICE = (over = {}) => ({ deviceId: 'd9', petId: 'p', model: 'Waveshare-AMOLED-2.16', name: '書桌的小栗', state: 'active', createdAt: '2026-10-20T00:00:00', activatedAt: '2026-10-20T00:00:05', lastSeenAt: new Date(Date.now() - 5 * 60_000).toISOString(), ...over })

async function start(page: import('@playwright/test').Page, devices: unknown[] = [], before?: () => Promise<void>) {
  const state = await mockApi(page, {}, { pet: { ...structuredClone(PET), equipment: { ...EQUIP } } as typeof PET })
  state.devices = devices
  await before?.()                                   // routes added after the API mock take precedence (Playwright matches the newest first)
  await page.goto('login')
  await page.getByLabel('Email').fill('a@example.com')
  await page.getByLabel('密碼').fill('Passw0rd-123')
  await page.getByRole('button', { name: '登入' }).click()
  await expect(page.getByRole('button', { name: '背包' })).toBeVisible()
  await page.getByRole('button', { name: '帳號' }).click()
  await page.getByRole('button', { name: /我的裝置/ }).click()
  return { state, dlg: page.getByRole('dialog', { name: '我的裝置' }) }
}

test.describe('配對裝置', () => {
  test('從帳號打開：沒有裝置時說明怎麼做', async ({ page }) => {
    const { dlg } = await start(page)
    await expect(dlg.getByText('還沒有配對裝置')).toBeVisible()
    await expect(dlg.getByRole('button', { name: '確認配對' })).toBeDisabled()          // nothing typed yet
  })

  test('輸入裝置畫面上的配對碼（大小寫、空白都可以）：核准後出現在清單，等裝置連線', async ({ page }) => {
    const { state, dlg } = await start(page)
    await dlg.getByLabel('配對碼', { exact: true }).fill('abcd-2345')
    await dlg.getByLabel('裝置名稱（選填）').fill('客廳的小栗')
    await dlg.getByRole('button', { name: '確認配對' }).click()
    await expect(dlg.getByText('已核准「客廳的小栗」')).toBeVisible()
    await expect(dlg.getByRole('list', { name: '已配對的裝置' })).toContainText('客廳的小栗')
    await expect(dlg.getByText('等裝置連線中…')).toBeVisible()
    expect(state.devices).toHaveLength(1)
    await expect(dlg.getByLabel('配對碼', { exact: true })).toHaveValue('')                // cleared for the next one
  })

  test('錯的配對碼：顯示說明，不會建立裝置', async ({ page }) => {
    const { state, dlg } = await start(page)
    await dlg.getByLabel('配對碼', { exact: true }).fill('ZZZZ-ZZZZ')
    await dlg.getByRole('button', { name: '確認配對' }).click()
    await expect(dlg.getByRole('alert')).toContainText('找不到這組配對碼')
    expect(state.devices).toHaveLength(0)
  })

  test('清單顯示使用中與最後連線；改名；移除要按兩次', async ({ page }) => {
    const { state, dlg } = await start(page, [DEVICE()])
    await expect(dlg.getByText('使用中')).toBeVisible()
    await expect(dlg.getByText('5 分鐘前')).toBeVisible()

    await dlg.getByRole('button', { name: '改名' }).click()
    await dlg.getByLabel('裝置名稱', { exact: true }).fill('臥室')
    await dlg.getByRole('button', { name: '儲存' }).click()
    await expect(dlg.getByRole('list', { name: '已配對的裝置' })).toContainText('臥室')

    await dlg.getByRole('button', { name: '移除' }).click()
    expect(state.devices).toHaveLength(1)                                                   // the first tap only asks
    await dlg.getByRole('button', { name: '確定移除？' }).click()
    await expect(dlg.getByText('還沒有配對裝置')).toBeVisible()
    expect(state.devices).toHaveLength(0)
  })

  test('已滿 3 台：顯示後端的說明', async ({ page }) => {
    const { dlg } = await start(page, [DEVICE({ deviceId: 'a' }), DEVICE({ deviceId: 'b' }), DEVICE({ deviceId: 'c' })])
    await dlg.getByLabel('配對碼', { exact: true }).fill('ABCD-2345')
    await dlg.getByRole('button', { name: '確認配對' }).click()
    await expect(dlg.getByRole('alert')).toContainText('最多配對 3 台')
  })

  test('裝置領到憑證的瞬間：收到 devices.updated 就更新成使用中（不用重新整理）', async ({ page }) => {
    const RS = '\u001e'
    let socket: import('@playwright/test').WebSocketRoute | null = null
    const { state, dlg } = await start(page, [DEVICE({ state: 'waiting', activatedAt: null, lastSeenAt: null })], async () => {
      await page.route('**/api/v1/hub/negotiate**', (r) => r.fulfill({ json: { negotiateVersion: 1, connectionId: 'c1', connectionToken: 't1', availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text', 'Binary'] }] } }))
      await page.routeWebSocket(/\/api\/v1\/hub/, (ws) => { socket = ws; ws.onMessage((m) => { if (String(m).includes('"protocol"')) ws.send('{}' + RS) }) })
    })
    await expect(dlg.getByText('等裝置連線中…')).toBeVisible()
    await expect.poll(() => socket !== null).toBe(true)
    await page.waitForTimeout(300)
    state.devices = [DEVICE()]
    socket!.send(JSON.stringify({ type: 1, target: 'pet.event', arguments: [{ seq: 1, eventId: crypto.randomUUID(), type: 'devices.updated', occurredAt: new Date().toISOString(), payload: {} }] }) + RS)
    await expect(dlg.getByText('使用中')).toBeVisible()
  })

  test('手機上放得下，頁面不捲動；返回會回到帳號面板', async ({ page }) => {
    const { dlg } = await start(page, [DEVICE()])
    expect(await page.evaluate(() => document.documentElement.scrollHeight > innerHeight + 1)).toBe(false)
    await dlg.getByRole('button', { name: '關閉' }).click()
    await expect(page.getByRole('dialog', { name: '帳號' })).toBeVisible()
  })
})
