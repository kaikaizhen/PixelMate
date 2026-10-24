import { expect, test } from '@playwright/test'
import { BAG, PET, mockApi } from './helpers'

const EQUIP = { hat: null, outfit: null, head: null, front: null, back: null, feet: null, hand: null, costume: null }

async function openBag(page: import('@playwright/test').Page, bag = BAG) {
  await mockApi(page, {}, { pet: { ...structuredClone(PET), equipment: { ...EQUIP } } as typeof PET, bag })
  await page.goto('login')
  await page.getByLabel('Email').fill('a@example.com')
  await page.getByLabel('密碼').fill('Passw0rd-123')
  await page.getByRole('button', { name: '登入' }).click()
  await page.getByRole('button', { name: '背包' }).click()
  return page.getByRole('dialog', { name: '背包' })
}

const worn = (page: import('@playwright/test').Page) => page.locator('button.pet img[data-item]')

test.describe('背包與穿戴', () => {
  test('空背包顯示提示', async ({ page }) => {
    const dlg = await openBag(page, [])
    await expect(dlg.getByText('還沒有道具，小夥伴會自己去找喔。')).toBeVisible()
  })

  test('列出道具、稀有度與數量；穿上帽子後小夥伴頭上真的出現那張圖，再按一次脫下', async ({ page }) => {
    const dlg = await openBag(page)
    await expect(dlg.getByText('4 種道具')).toBeVisible()
    await expect(dlg.getByRole('button', { name: /野餐帆布鞋，稀有/ })).toContainText('×2')
    await expect(worn(page)).toHaveCount(0)

    await dlg.getByRole('button', { name: '郵局草帽，普通，點一下穿上' }).click()
    await expect(worn(page)).toHaveCount(1)
    await expect(worn(page).first()).toHaveAttribute('src', /sprites\/equipment\/hats\/straw_post\.png/)
    await expect(dlg.getByRole('button', { name: '郵局草帽，普通，穿戴中，點一下脫下' })).toHaveAttribute('aria-pressed', 'true')
    expect(await worn(page).first().evaluate((e) => (e as HTMLImageElement).naturalWidth)).toBe(48)          // the art file really loaded

    await dlg.getByRole('button', { name: '郵局草帽，普通，穿戴中，點一下脫下' }).click()
    await expect(worn(page)).toHaveCount(0)
  })

  test('布偶裝與帽子互斥，布偶裝的尾巴畫在身體後面；鞋子可以同時穿', async ({ page }) => {
    const dlg = await openBag(page)
    await dlg.getByRole('button', { name: /郵局草帽/ }).click()
    await dlg.getByRole('button', { name: /野餐帆布鞋/ }).click()
    await expect(worn(page)).toHaveCount(2)

    await dlg.getByRole('button', { name: /小狗布偶裝/ }).click()
    await expect(worn(page)).toHaveCount(2)                                                        // costume + shoes, the hat is gone
    await expect(page.locator('button.pet img[data-item="equip.hat.straw_post"]')).toHaveCount(0)
    await expect(page.locator('button.pet img[src*="dog_tail"]')).toHaveCount(1)                   // tail layer behind the body
    const order = await page.locator('button.pet .gp > *').evaluateAll((els) => els.map((e) => (e as HTMLElement).getAttribute('src') ?? 'body'))
    expect(order.findIndex((s) => s.includes('dog_tail'))).toBeLessThan(order.indexOf('body'))     // behind
    expect(order.findIndex((s) => s.includes('costumes/dog.png'))).toBeGreaterThan(order.indexOf('body'))   // in front

    await dlg.getByRole('button', { name: /郵局草帽/ }).click()
    await expect(page.locator('button.pet img[src*="dog_tail"]')).toHaveCount(0)                   // the hat pushed the costume off
    await expect(page.locator('button.pet img[data-item="equip.hat.straw_post"]')).toHaveCount(1)
  })

  test('分頁會篩選；房間用品點了只提示，不會穿上', async ({ page }) => {
    const dlg = await openBag(page)
    await dlg.getByRole('tab', { name: '房間' }).click()
    await expect(dlg.getByRole('button', { name: /花園單人床/ })).toBeVisible()
    await expect(dlg.getByRole('button', { name: /郵局草帽/ })).toHaveCount(0)
    await dlg.getByRole('button', { name: /花園單人床/ }).click()
    await expect(dlg.getByRole('alert')).toContainText('很快就會開放')
    await expect(worn(page)).toHaveCount(0)
    await dlg.getByRole('tab', { name: '收藏' }).click()
    await expect(dlg.getByText('這一類還沒有道具。')).toBeVisible()
  })

  test('穿戴的狀態重新整理後還在（以後端為準）', async ({ page }) => {
    const dlg = await openBag(page)
    await dlg.getByRole('button', { name: /小狗布偶裝/ }).click()
    await expect(page.locator('button.pet img[data-item="equip.costume.dog"]')).toHaveCount(1)
    await page.reload()
    await expect(page.locator('button.pet img[data-item="equip.costume.dog"]')).toHaveCount(1)
  })

  test('別的裝置換了裝備：回到這個畫面時小夥伴也跟著換', async ({ page }) => {
    await openBag(page)
    await expect(worn(page)).toHaveCount(0)
    await page.route('**/api/v1/pets/me', (r) =>
      r.request().method() === 'GET'
        ? r.fulfill({ json: { ...structuredClone(PET), revision: 9, equipment: { ...EQUIP, head: 'equip.hat.straw_post' } } })
        : r.fallback())
    await page.evaluate(() => window.dispatchEvent(new Event('focus')))
    await expect(worn(page)).toHaveCount(1)
  })

  test('小夥伴現在在做什麼：顯示後端給的活動；哼歌時頭上有音符', async ({ page }) => {
    const now = Date.now()
    const activity = (key: string) => ({ key, startedAt: new Date(now - 60_000).toISOString(), endsAt: new Date(now + 600_000).toISOString() })
    await mockApi(page, {}, { pet: { ...structuredClone(PET), equipment: { ...EQUIP }, activity: activity('read') } as typeof PET, bag: BAG })
    await page.goto('login')
    await page.getByLabel('Email').fill('a@example.com')
    await page.getByLabel('密碼').fill('Passw0rd-123')
    await page.getByRole('button', { name: '登入' }).click()
    await expect(page.getByText('小栗正在看書')).toBeVisible()
    await expect(page.locator('[data-fx="note"]')).toHaveCount(0)
    await expect(page.locator('[data-prop="book"]')).toHaveCount(1)                       // reading: the book opens in its hands
    await expect(page.locator('button.pet img.face')).toHaveAttribute('src', /face_look_down/)

    await page.route('**/api/v1/pets/me', (r) =>
      r.request().method() === 'GET' ? r.fulfill({ json: { ...structuredClone(PET), revision: 9, equipment: { ...EQUIP }, activity: activity('hum') } }) : r.fallback())
    await page.evaluate(() => window.dispatchEvent(new Event('focus')))
    await expect(page.getByText('小栗正在哼歌')).toBeVisible()
    await expect(page.locator('[data-fx="note"]')).toHaveCount(1)
    await expect(page.locator('[data-prop="book"]')).toHaveCount(0)
    await expect(page.locator('button.pet img.face')).toHaveAttribute('src', /face_normal/)

    await page.route('**/api/v1/pets/me', (r) =>
      r.request().method() === 'GET' ? r.fulfill({ json: { ...structuredClone(PET), revision: 10, equipment: { ...EQUIP }, activity: activity('letter') } }) : r.fallback())
    await page.evaluate(() => window.dispatchEvent(new Event('focus')))
    await expect(page.getByText('小栗正在寫信')).toBeVisible()
    await expect(page.locator('[data-prop="letter"]')).toHaveCount(1)                     // writing: paper and a quill
    await expect(page.locator('[data-prop="book"]')).toHaveCount(0)
  })

  test('散步時撿到東西：只對沒看過的事件跳出提示', async ({ page }) => {
    const found = (id: string, at: number) => ({ eventId: id, kind: 'find', itemId: 'equip.hat.straw_post', name: '郵局草帽', assetKey: 'equip.hat.straw_post', rarity: 'common', occurredAt: new Date(at).toISOString() })
    let events = [found('e1', Date.now() - 3_600_000)]
    await mockApi(page, { '/pets/me/events': (r) => r.fulfill({ json: { events } }) }, { pet: { ...structuredClone(PET), equipment: { ...EQUIP } } as typeof PET, bag: BAG })
    await page.goto('login')
    await page.getByLabel('Email').fill('a@example.com')
    await page.getByLabel('密碼').fill('Passw0rd-123')
    await page.getByRole('button', { name: '登入' }).click()
    await expect(page.getByRole('button', { name: '背包' })).toBeVisible()
    await expect(page.getByText('散步時撿到了')).toHaveCount(0)            // the first look only records what is already there

    events = [found('e2', Date.now() + 1000), ...events]
    await page.waitForTimeout(1200)
    await page.evaluate(() => window.dispatchEvent(new Event('focus')))
    await expect(page.getByText('小栗散步時撿到了「郵局草帽」')).toBeVisible()
  })

  test('小夥伴會去陽台和院子：換成全螢幕的戶外場景，天氣用後端說的', async ({ page }) => {
    const world = (scene: string, weather: string) => ({ ...structuredClone(PET), revision: 20, equipment: { ...EQUIP }, world: { mood: 'happy', scene, weather } })
    await mockApi(page, {}, { pet: world('home', 'snow') as typeof PET, bag: BAG })
    await page.goto('login')
    await page.getByLabel('Email').fill('a@example.com')
    await page.getByLabel('密碼').fill('Passw0rd-123')
    await page.getByRole('button', { name: '登入' }).click()
    await expect(page.getByRole('button', { name: '背包' })).toBeVisible()
    await expect(page.locator('[data-scene="outdoor"]')).toHaveCount(0)                    // at home: the room with the window
    await expect(page.locator('.win')).toHaveCSS('background-image', /snow_window/)       // the window shows the server's weather

    for (const [scene, label] of [['balcony', '在陽台'], ['yard', '在院子']] as const) {
      await page.route('**/api/v1/pets/me', (r) => (r.request().method() === 'GET' ? r.fulfill({ json: { ...world(scene, 'thunder'), activity: { key: 'walk', startedAt: new Date().toISOString(), endsAt: new Date(Date.now() + 600_000).toISOString() } } }) : r.fallback()))
      await page.evaluate(() => window.dispatchEvent(new Event('focus')))
      await expect(page.locator('[data-scene="outdoor"]')).toHaveAttribute('src', new RegExp(`thunder_${scene}`))
      await expect(page.getByText(label)).toBeVisible()
      await expect(page.locator('button.pet')).toBeVisible()
    }
    const overflow = await page.evaluate(() => document.documentElement.scrollHeight > window.innerHeight + 1)
    expect(overflow).toBe(false)
  })

  test('發呆時會眨眼；下雨天待在家會看向窗外；去戶外時背景延伸到螢幕邊緣', async ({ page }) => {
    const now = Date.now()
    const pet = (scene: string, weather: string, key: string) => ({ ...structuredClone(PET), revision: 30, equipment: { ...EQUIP }, world: { mood: 'happy', scene, weather },
      activity: { key, startedAt: new Date(now - 60_000).toISOString(), endsAt: new Date(now + 600_000).toISOString() } })
    await mockApi(page, {}, { pet: pet('home', 'cloudy', 'idle') as typeof PET, bag: BAG })
    await page.goto('login')
    await page.getByLabel('Email').fill('a@example.com')
    await page.getByLabel('密碼').fill('Passw0rd-123')
    await page.getByRole('button', { name: '登入' }).click()
    await expect(page.locator('[data-blink]')).toHaveCount(1)
    await expect(page.locator('button.pet img.face').first()).toHaveAttribute('src', /face_normal/)

    const show = async (p: ReturnType<typeof pet>) => {
      await page.route('**/api/v1/pets/me', (r) => (r.request().method() === 'GET' ? r.fulfill({ json: p }) : r.fallback()))
      await page.evaluate(() => window.dispatchEvent(new Event('focus')))
    }
    await show(pet('home', 'shower', 'idle'))
    await expect(page.locator('button.pet img.face').first()).toHaveAttribute('src', /face_look_right/)       // watching the rain
    await expect(page.locator('[data-blink]')).toHaveCount(0)
    await show(pet('home', 'shower', 'read'))
    await expect(page.locator('button.pet img.face').first()).toHaveAttribute('src', /face_look_down/)        // reading is not interrupted by the rain
    await show(pet('yard', 'cloudy', 'walk'))
    await expect(page.locator('[data-scene="outdoor"]')).toHaveCount(1)
    await expect(page.locator('[data-extend]')).toHaveCount(1)
    expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1 || document.documentElement.scrollHeight > window.innerHeight + 1)).toBe(false)
  })
})
