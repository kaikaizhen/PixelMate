import { expect, test } from '@playwright/test'

const animationName = (page: import('@playwright/test').Page, selector: string) =>
  page.locator(selector).first().evaluate((el) => getComputedStyle(el).animationName)

test.describe('動態世界', () => {
  test('草、植物與窗簾有逐格動畫', async ({ page }) => {
    await page.goto('/')
    const names = await page.locator('.sprite').evaluateAll((els) => els.map((el) => getComputedStyle(el).animationName))
    expect(names.length).toBeGreaterThanOrEqual(4)
    for (const name of names) expect(name).not.toBe('none')
  })

  test('雲在移動', async ({ page }) => {
    await page.goto('/')
    expect(await animationName(page, '.win__cloud')).not.toBe('none')
  })

  test('切換雨天會出現雨滴，切到夜晚出現星星', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('radio', { name: '雨天' }).click()
    await expect(page.locator('.win__drop').first()).toBeAttached()
    expect(await animationName(page, '.win__drop')).not.toBe('none')
    await page.getByRole('radio', { name: '夜晚' }).click()
    await expect(page.locator('#weather .win__moon')).toBeAttached()
    await expect(page.locator('#weather .win__drop')).toHaveCount(0)
    await expect(page.getByRole('radio', { name: '夜晚' })).toHaveAttribute('aria-checked', 'true')
  })

  test('偏好減少動態時動畫停止', async ({ browser }) => {
    const ctx = await browser.newContext({ reducedMotion: 'reduce' })
    const page = await ctx.newPage()
    await page.goto('http://localhost:4173/')
    const dur = await page.locator('.sprite').first().evaluate((el) => getComputedStyle(el).animationDuration)
    expect(parseFloat(dur)).toBeLessThan(0.01)
    await ctx.close()
  })
})
