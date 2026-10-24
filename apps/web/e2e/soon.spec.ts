import { expect, test } from '@playwright/test'

test.describe('Coming Soon 收尾', () => {
  test('顯示收尾標題與副標', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('heading', { name: /一個小小的世界/ })).toBeVisible()
    await expect(page.getByText('先在手機裡認識牠')).toBeVisible()
  })

  test('CTA 為停用的「即將開放」，不是假的註冊／下載', async ({ page }) => {
    await page.goto('/')
    const cta = page.getByRole('button', { name: '即將開放' })
    await expect(cta).toBeDisabled()
    await expect(page.getByRole('link', { name: '開始養一隻' })).toHaveCount(0)
    await expect(page.locator('input[type="email"]')).toHaveCount(0)
  })

  test('360px 無橫向捲動', async ({ page }) => {
    await page.goto('/')
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
    expect(overflow).toBeLessThanOrEqual(0)
  })
})
