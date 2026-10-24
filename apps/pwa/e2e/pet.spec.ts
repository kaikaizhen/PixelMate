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
    await expect(page.getByRole('button', { name: '摸摸 小栗' })).toBeVisible()
    await page.goto('adopt')
    await expect(page.getByRole('button', { name: '摸摸 小栗' })).toBeVisible()
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
    await expect(page.getByRole('button', { name: '摸摸 小栗' })).toBeVisible()
  })

  test('小屋是全螢幕的房間；功能用浮動面板顯示：小夥伴、背包、帳號，信箱還沒開放', async ({ page }) => {
    await mockApi(page)
    await signIn(page)
    const vp = page.viewportSize()!
    const full = await page.locator('.full').boundingBox()
    expect(full).toMatchObject({ x: 0, y: 0, width: vp.width, height: vp.height })      // the room is the whole screen

    await expect(page.getByRole('dialog')).toHaveCount(0)                               // nothing covers the room until asked
    await page.getByRole('button', { name: '摸摸 小栗' }).click()
    await expect(page.locator('button.pet img.face')).toHaveAttribute('src', /face_happy/)

    await page.getByRole('button', { name: '小夥伴' }).click()
    const dlg = page.getByRole('dialog', { name: '小栗' })
    await expect(dlg).toBeVisible()
    await expect(dlg.getByText('Lv.1')).toBeVisible()
    for (const s of ['共感', '創作', '探索', '智慧']) await expect(dlg.getByText(s, { exact: true })).toBeVisible()
    await page.keyboard.press('Escape')
    await expect(page.getByRole('dialog')).toHaveCount(0)

    await page.getByRole('button', { name: '小夥伴' }).click()
    await page.getByRole('button', { name: '關閉' }).click()                           // the close button
    await expect(page.getByRole('dialog')).toHaveCount(0)

    await expect(page.getByRole('button', { name: '背包' })).toBeEnabled()
    await expect(page.getByRole('button', { name: '信箱，即將推出' })).toBeDisabled()
  })

  test('面板沒有遮罩：開著面板時房間仍看得到、小夥伴仍可以點；同一個按鈕再按會收起', async ({ page }) => {
    await mockApi(page)
    await signIn(page)
    await page.getByRole('button', { name: '小夥伴' }).click()
    const dlg = page.getByRole('dialog', { name: '小栗' })
    await expect(dlg).toBeVisible()
    await expect(dlg).toHaveAttribute('aria-modal', 'false')
    expect(await page.evaluate(() => document.querySelectorAll('dialog').length)).toBe(0)       // no modal <dialog>, no ::backdrop

    // the whole room is still reachable behind the card
    await page.getByRole('button', { name: '摸摸 小栗' }).click()   // not covered by the card
    await expect(page.locator('button.pet img.face')).toHaveAttribute('src', /face_happy/)

    await page.getByRole('button', { name: '小夥伴' }).click()                              // toggle
    await expect(page.getByRole('dialog')).toHaveCount(0)
  })

  test('帳號面板可以登出', async ({ page }) => {
    await mockApi(page)
    await signIn(page)
    await page.getByRole('button', { name: '帳號' }).click()
    await expect(page.getByRole('dialog', { name: '帳號' })).toContainText('a@example.com')
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
    await expect(page.getByRole('button', { name: '摸摸 麻糬' })).toBeVisible()
    const w = await page.evaluate(() => ({ sw: document.documentElement.scrollWidth, cw: document.documentElement.clientWidth }))
    expect(w.sw).toBeLessThanOrEqual(w.cw)
  })
})
