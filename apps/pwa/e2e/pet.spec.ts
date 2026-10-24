import { expect, test } from '@playwright/test'
import { PET, json, mockApi } from './helpers'

async function signIn(page: import('@playwright/test').Page) {
  await page.goto('login')
  await page.getByLabel('Email').fill('a@example.com')
  await page.getByLabel('密碼').fill('Passw0rd-123')
  await page.getByRole('button', { name: '登入' }).click()
}

test.describe('領養小夥伴', () => {
  test('登入後還沒有小夥伴會被帶到領養頁，領養成功後進小屋', async ({ page }) => {
    const api = await mockApi(page, {}, { pet: null })
    await signIn(page)
    await expect(page).toHaveURL(/\/web\/adopt/)
    await expect(page.getByRole('heading', { name: '領養你的第一隻小夥伴' })).toBeVisible()

    await page.getByRole('radio', { name: '薄荷色' }).check({ force: true })
    await page.getByLabel('小夥伴的名字').fill('麻糬')
    await page.getByRole('button', { name: '領養牠！' }).click()

    await expect(page).toHaveURL(/\/web\/\?welcome=1/)
    await expect(page.getByRole('heading', { name: '麻糬' })).toBeVisible()
    await expect(page.getByText('領養成功！麻糬 住進你的小屋了。')).toBeVisible()
    expect(api.calls.find((c) => c.path === '/pets')?.body).toEqual({ name: '麻糬', bodyVariant: 'star_01_mint' })
  })

  test('名字空白或太長不會送出，「幫我想一個」會填入名字', async ({ page }) => {
    const api = await mockApi(page, {}, { pet: null })
    await signIn(page)
    await page.getByRole('button', { name: '領養牠！' }).click()
    await expect(page.getByText('請幫小夥伴取個名字。')).toBeVisible()
    await page.getByLabel('小夥伴的名字').fill('<b>小栗</b>')
    await page.getByRole('button', { name: '領養牠！' }).click()
    await expect(page.getByText('名字只能用文字')).toBeVisible()
    await page.getByRole('button', { name: /幫我想一個/ }).click()
    await expect(page.getByLabel('小夥伴的名字')).not.toHaveValue('')
    expect(api.calls.some((c) => c.path === '/pets' && c.body)).toBe(false)
  })

  test('已經有小夥伴的人直接進小屋，開領養頁會被導回', async ({ page }) => {
    await mockApi(page)
    await signIn(page)
    await expect(page.getByRole('heading', { name: '小栗' })).toBeVisible()
    await page.goto('adopt')
    await expect(page.getByRole('heading', { name: '小栗' })).toBeVisible()
    await expect(page).not.toHaveURL(/adopt/)
  })

  test('另一個分頁已經領養（409）時直接顯示那隻', async ({ page }) => {
    const api = await mockApi(page, {
      '/pets': (r) => {
        api.pet = PET // the other tab got there first
        return json(r, 409, { code: 'PET_ALREADY_EXISTS', message: '你已經有一隻小夥伴了。', requestId: 'r' })
      },
    }, { pet: null })
    await signIn(page)
    await page.getByLabel('小夥伴的名字').fill('麻糬')
    await page.getByRole('button', { name: '領養牠！' }).click()
    await expect(page.getByRole('heading', { name: '小栗' })).toBeVisible()
  })

  test('小屋：顯示等級與技能，點小夥伴會變開心，登出回到登入頁', async ({ page }) => {
    await mockApi(page)
    await signIn(page)
    await expect(page.getByText('Lv.1')).toBeVisible()
    for (const s of ['共感', '創作', '探索', '智慧']) await expect(page.getByText(s, { exact: true })).toBeVisible()
    await page.getByRole('button', { name: '摸摸 小栗' }).click()
    await expect(page.locator('.pet img.face')).toHaveAttribute('src', /face_happy/)
    await page.getByRole('button', { name: '登出' }).click()
    await expect(page).toHaveURL(/\/web\/login/)
  })

  test('小屋用我的房子與素材：窗外天氣、家具與小夥伴都來自 manifest', async ({ page }) => {
    await page.clock.install({ time: new Date('2026-10-25T07:30:00') })       // morning -> sunrise outside the window
    await mockApi(page)
    const assets: string[] = []
    page.on('response', (r) => r.url().includes('/web/game/') && assets.push(new URL(r.url()).pathname))
    await signIn(page)
    await expect(page.getByRole('group', { name: '你的小屋' })).toBeVisible()
    await expect.poll(() => assets.length).toBeGreaterThan(3)
    expect(assets.some((a) => a.endsWith('/game/manifest.json'))).toBe(true)
    expect(assets).toContain('/web/game/backgrounds/sunrise_window.png')
    expect(assets).toContain('/web/game/sprites/furniture/table_garden.png')
    expect(assets).toContain('/web/game/sprites/pets/star_01/body_cream_idle.png')
    for (const path of assets) {
      const res = await page.request.get(path)
      expect(res.status(), path).toBe(200)
    }
  })

  test('夜裡窗外是星空', async ({ page }) => {
    await page.clock.install({ time: new Date('2026-10-25T23:00:00') })
    await mockApi(page)
    const assets: string[] = []
    page.on('response', (r) => r.url().includes('/web/game/') && assets.push(new URL(r.url()).pathname))
    await signIn(page)
    await expect.poll(() => assets.some((a) => a.endsWith('starry_window.png'))).toBe(true)
  })

  test('領養頁與小屋在手機和桌機都沒有橫向捲動', async ({ page }) => {
    await mockApi(page, {}, { pet: null })
    await signIn(page)
    for (const [path] of [['adopt']]) {
      await page.goto(path)
      const w = await page.evaluate(() => ({ sw: document.documentElement.scrollWidth, cw: document.documentElement.clientWidth }))
      expect(w.sw).toBeLessThanOrEqual(w.cw)
    }
    await page.getByLabel('小夥伴的名字').fill('麻糬')
    await page.getByRole('button', { name: '領養牠！' }).click()
    await expect(page.getByRole('heading', { name: '麻糬' })).toBeVisible()
    const w = await page.evaluate(() => ({ sw: document.documentElement.scrollWidth, cw: document.documentElement.clientWidth }))
    expect(w.sw).toBeLessThanOrEqual(w.cw)
  })
})
