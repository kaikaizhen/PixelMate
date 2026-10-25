import { expect, test } from '@playwright/test'

test.describe('Coming Soon 收尾', () => {
  test('顯示收尾標題與副標', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('heading', { name: /一個小小的世界/ })).toContainText('現在開放試玩')
    await expect(page.getByText('先在手機裡認識牠')).toBeVisible()
  })

  test('CTA 連到測試服 /web/，並清楚標示「測試服（會刪檔）」；官網本身沒有註冊表單', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('link', { name: '開始養一隻' })).toHaveAttribute('href', '/web/')
    await expect(page.getByTestId('app-stage')).toContainText('測試服（會刪檔）')
    await expect(page.getByTestId('app-stage')).toContainText('可能隨時被清除')
    await expect(page.getByRole('button', { name: '即將開放' })).toHaveCount(0)
    await expect(page.locator('input[type="email"]')).toHaveCount(0)
  })

  test('360px 無橫向捲動', async ({ page }) => {
    await page.goto('/')
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
    expect(overflow).toBeLessThanOrEqual(0)
  })
})
