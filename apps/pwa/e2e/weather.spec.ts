import { expect, test } from '@playwright/test'
import { PET, mockApi } from './helpers'

const EQUIP = { hat: null, outfit: null, head: null, front: null, back: null, feet: null, hand: null, costume: null }
const world = (weather: string, extra = {}) => ({ mood: 'happy', scene: 'home', weather, temperatureC: 24.4, weatherSource: 'cwa', timeZone: 'Asia/Taipei', ...extra })

async function start(page: import('@playwright/test').Page, opts: { weather?: string; location?: unknown } = {}) {
  const state = await mockApi(page, {}, { pet: { ...structuredClone(PET), equipment: { ...EQUIP }, world: world(opts.weather ?? 'shower') } as typeof PET })
  if ('location' in opts) state.location = opts.location
  await page.goto('login')
  await page.getByLabel('Email').fill('a@example.com')
  await page.getByLabel('密碼').fill('Passw0rd-123')
  await page.getByRole('button', { name: '登入' }).click()
  await expect(page.getByRole('button', { name: '背包' })).toBeVisible()
  return state
}

test.describe('所在地與天氣', () => {
  test('畫面上有天氣標籤（天氣、溫度），窗外也跟著伺服器的天氣', async ({ page }) => {
    await start(page, { weather: 'shower' })
    await expect(page.getByRole('button', { name: /天氣：.*陣雨 24°/ })).toBeVisible()
    await expect(page.locator('.win')).toHaveCSS('background-image', /shower_window/)
    await expect(page.locator('.win [data-weather-fx]')).toHaveAttribute('data-kind', 'rain')
  })

  test('第一次進來沒有位置：問一次，選縣市後套用，天氣跟著換，之後不再問', async ({ page }) => {
    const state = await start(page, { location: null })
    const dlg = page.getByRole('dialog', { name: '所在地與天氣' })
    await expect(dlg).toBeVisible()
    await expect(dlg.getByText('還沒設定')).toBeVisible()
    await dlg.getByLabel('選擇縣市或城市').selectOption({ label: '東京' })
    await dlg.getByRole('button', { name: '套用' }).click()
    await expect(dlg).toBeHidden()
    expect(state.location).toMatchObject({ label: '東京', timeZone: 'Asia/Tokyo', source: 'city' })
    await expect(page.getByRole('button', { name: /天氣：.*下雪/ })).toBeVisible()

    await page.reload()
    await expect(page.getByRole('button', { name: '背包' })).toBeVisible()
    await expect(page.getByRole('dialog', { name: '所在地與天氣' })).toHaveCount(0)
  })

  test('「之後再說」關掉後不會一直跳出來，但隨時能從天氣標籤打開', async ({ page }) => {
    await start(page, { location: null })
    const dlg = page.getByRole('dialog', { name: '所在地與天氣' })
    await dlg.getByRole('button', { name: '之後再說' }).click()
    await expect(dlg).toBeHidden()
    await page.reload()
    await expect(page.getByRole('button', { name: '背包' })).toBeVisible()
    await expect(dlg).toHaveCount(0)
    await page.getByRole('button', { name: /天氣：/ }).click()
    await expect(dlg).toBeVisible()
    await expect(dlg.getByRole('status', { name: '目前天氣' })).toContainText('陣雨 24°C')
    await expect(dlg.getByRole('status', { name: '目前天氣' })).toContainText('中央氣象署')
  })

  test.describe('瀏覽器定位', () => {
    test.use({ geolocation: { latitude: 22.6301, longitude: 120.3012 }, permissions: ['geolocation'] })

    test('用目前位置：存起來，名字取最近的縣市', async ({ page }) => {
      const state = await start(page, { location: null })
      const dlg = page.getByRole('dialog', { name: '所在地與天氣' })
      await dlg.getByRole('button', { name: /使用目前位置/ }).click()
      await expect(dlg).toBeHidden()
      expect(state.location).toMatchObject({ label: '高雄市附近', source: 'gps' })
      expect(state.location.latitude).toBe(22.6)                                   // the server keeps a grid cell, not the exact fix
    })
  })

  test.describe('不給定位權限', () => {
    test.use({ permissions: [] })

    test('被拒絕時說明並請他改選縣市，不會卡住', async ({ page }) => {
      await page.addInitScript(() => {
        navigator.geolocation.getCurrentPosition = (_ok, err) => err?.({ code: 1, PERMISSION_DENIED: 1, message: 'denied' } as GeolocationPositionError)
      })
      await start(page, { location: null })
      const dlg = page.getByRole('dialog', { name: '所在地與天氣' })
      await dlg.getByRole('button', { name: /使用目前位置/ }).click()
      await expect(dlg.getByRole('alert')).toContainText('改選一個縣市')
      await dlg.getByLabel('選擇縣市或城市').selectOption({ label: '臺北市' })
      await dlg.getByRole('button', { name: '套用' }).click()
      await expect(dlg).toBeHidden()
    })
  })
})
