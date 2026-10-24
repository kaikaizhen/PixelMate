import { expect, test } from '@playwright/test'
import { mockApi } from './helpers'

test.describe('RWD', () => {
  for (const path of ['login', 'register', 'forgot-password']) {
    test(`${path} 在這個視窗沒有橫向捲動，按鈕與輸入框夠大`, async ({ page }) => {
      await mockApi(page)
      await page.goto(path)
      const w = await page.evaluate(() => ({ sw: document.documentElement.scrollWidth, cw: document.documentElement.clientWidth }))
      expect(w.sw).toBeLessThanOrEqual(w.cw)
      for (const el of await page.locator('input, button[type=submit]').all()) {
        const box = await el.boundingBox()
        expect(box!.height).toBeGreaterThanOrEqual(44)
      }
      await expect(page.locator('input').first()).toHaveCSS('font-size', '16px')   // no iOS zoom on focus
    })
  }
})

test.describe('PWA', () => {
  test('manifest 完整且 icon 可載入', async ({ page, request }) => {
    await mockApi(page)
    await page.goto('login')
    const href = await page.locator('link[rel=manifest]').getAttribute('href')
    const res = await request.get(new URL(href!, page.url()).toString())
    const m = await res.json()
    expect(m).toMatchObject({ name: '小伴星 PixelMate', display: 'standalone', scope: '/web/', start_url: '/web/', lang: 'zh-Hant' })
    expect(m.icons.some((i: any) => i.sizes === '512x512' && i.purpose === 'maskable')).toBe(true)
    for (const i of m.icons) {
      expect((await request.get(new URL(i.src, new URL(href!, page.url())).toString())).status()).toBe(200)
    }
    await expect(page.locator('meta[name=theme-color]')).toHaveAttribute('content', '#fff8ec')
  })

  test('Service Worker 啟用後可以離線開啟畫面並顯示離線提示，API 不被快取', async ({ page, context }) => {
    await mockApi(page)
    await page.goto('login')
    await page.evaluate(() => navigator.serviceWorker.ready)
    await page.reload()
    await page.waitForFunction(() => !!navigator.serviceWorker.controller)

    await context.setOffline(true)
    await page.goto('login')
    await expect(page.getByRole('heading', { name: '歡迎回來' })).toBeVisible()
    await expect(page.getByRole('status')).toContainText('目前沒有網路')
    await expect(page.getByRole('button', { name: '目前沒有網路' })).toBeDisabled()
    await context.setOffline(false)
    await expect(page.getByRole('button', { name: '登入' })).toBeEnabled()

    const cached = await page.evaluate(async () => {
      const keys = await caches.keys()
      const urls: string[] = []
      for (const k of keys) urls.push(...(await (await caches.open(k)).keys()).map((r) => r.url))
      return urls
    })
    expect(cached.some((u) => u.includes('/api/'))).toBe(false)
  })
})
