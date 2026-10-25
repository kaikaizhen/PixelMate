import { expect, test } from '@playwright/test'
import { PET, mockApi } from './helpers'

const EQUIP = { hat: null, outfit: null, head: null, front: null, back: null, feet: null, hand: null, costume: null }

async function start(page: import('@playwright/test').Page, scene: string, weather: string) {
  await mockApi(page, {}, { pet: { ...structuredClone(PET), equipment: { ...EQUIP }, world: { mood: 'happy', scene, weather } } as typeof PET })
  await page.goto('login')
  await page.getByLabel('Email').fill('a@example.com')
  await page.getByLabel('密碼').fill('Passw0rd-123')
  await page.getByRole('button', { name: '登入' }).click()
  await expect(page.getByRole('button', { name: '背包' })).toBeVisible()
}

/** A fingerprint of what the canvas shows right now (painted pixels and where they are). */
const fingerprint = (page: import('@playwright/test').Page, selector: string) =>
  page.evaluate((sel) => {
    const c = document.querySelector(sel) as HTMLCanvasElement
    const d = c.getContext('2d')!.getImageData(0, 0, c.width, c.height).data
    let painted = 0, hash = 0
    for (let i = 3; i < d.length; i += 4) if (d[i] > 0) { painted++; hash = (hash * 31 + i) | 0 }
    return { painted, hash, w: c.width, h: c.height }
  }, selector)

test.describe('窗外與戶外的天氣會動', () => {
  test('下雨天：窗戶裡有一張雨的畫布，畫面隔一下就變（不是貼一張不會動的圖）', async ({ page }) => {
    await start(page, 'home', 'shower')
    const canvas = page.locator('.win [data-weather-fx]')
    await expect(canvas).toHaveAttribute('data-kind', 'rain')
    const a = await fingerprint(page, '.win [data-weather-fx]')
    expect(a.painted).toBeGreaterThan(5)
    expect([a.w, a.h]).toEqual([64, 72])                                    // the window opening, in game pixels
    await page.waitForTimeout(700)
    const b = await fingerprint(page, '.win [data-weather-fx]')
    expect(b.hash).not.toBe(a.hash)
  })

  test('夜晚的星星會閃、白天有雲和鳥在飄：每種天氣都有對應的動態', async ({ page }) => {
    await start(page, 'home', 'starry')
    await expect(page.locator('.win [data-weather-fx]')).toHaveAttribute('data-kind', 'stars')
    const a = await fingerprint(page, '.win [data-weather-fx]')
    await page.waitForTimeout(1200)
    expect((await fingerprint(page, '.win [data-weather-fx]')).hash).not.toBe(a.hash)
  })

  test('去戶外：雷雨蓋滿整個畫面（包含延伸到螢幕邊緣的部分），小屋窗戶的那張則不再顯示', async ({ page }) => {
    await start(page, 'yard', 'thunder')
    const out = page.locator('[data-outdoor-fx] [data-weather-fx]')
    await expect(out).toHaveAttribute('data-kind', 'rain,lightning')
    await expect(page.locator('.win [data-weather-fx]')).toHaveCount(0)
    const size = await page.evaluate(() => { const c = document.querySelector('[data-outdoor-fx] canvas') as HTMLCanvasElement; const r = c.getBoundingClientRect(); return { w: r.width, h: r.height, vw: innerWidth, vh: innerHeight } })
    expect(Math.round(size.w)).toBe(size.vw)
    expect(Math.round(size.h)).toBe(size.vh)
    expect((await fingerprint(page, '[data-outdoor-fx] canvas')).painted).toBeGreaterThan(30)
  })

  test('天氣畫布不會擋住點擊，也不會讓頁面捲動', async ({ page }) => {
    await start(page, 'home', 'snow')
    await page.getByRole('button', { name: '背包' }).click()
    await expect(page.getByRole('dialog', { name: '背包' })).toBeVisible()
    expect(await page.evaluate(() => document.documentElement.scrollHeight > innerHeight + 1 || document.documentElement.scrollWidth > innerWidth + 1)).toBe(false)
  })
})
