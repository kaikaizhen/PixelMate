import { expect, test } from '@playwright/test'

test.describe('兩個平台預告', () => {
  test('手機 PWA 標示為測試服並連到 /web/；桌面 ESP32 仍是即將登場', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('heading', { name: '兩個地方，都找得到牠' })).toBeVisible()
    await expect(page.getByRole('heading', { name: /手機・網頁 App（PWA）/ })).toBeVisible()
    await expect(page.getByRole('heading', { name: /桌面小螢幕（ESP32）/ })).toBeVisible()
    await expect(page.locator('#platforms').getByText('即將登場')).toHaveCount(1)                        // only the ESP32 is still coming
    await expect(page.locator('#platforms').getByText('開放測試中・測試服（會刪檔）')).toBeVisible()
    await expect(page.locator('#platforms').getByText('測試期間帳號與角色可能隨時被清除')).toBeVisible()
    const go = page.locator('#platforms').getByRole('link', { name: '進入測試服' })
    await expect(go).toHaveAttribute('href', '/web/')
    await expect(page.getByText('以官方公告為準')).toBeVisible()
  })

  test('螢幕內角色有動畫', async ({ page }) => {
    await page.goto('/')
    const n = await page.locator('#platforms .device .sprite').evaluateAll((els) => els.map((el) => getComputedStyle(el).animationName))
    expect(n).toHaveLength(2)
    for (const name of n) expect(name).not.toBe('none')
  })

  test('沒有下載或購買字樣', async ({ page }) => {
    await page.goto('/')
    const text = await page.locator('#platforms').innerText()
    for (const w of ['下載', '購買', '預購', '$', 'NT']) expect(text).not.toContain(w)
  })

  test('360px 無橫向捲動', async ({ page }) => {
    await page.goto('/')
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
    expect(overflow).toBeLessThanOrEqual(0)
  })
})
