import { expect, test } from '@playwright/test'

test.describe('Hero 首屏', () => {
  test('顯示主標與「即將與你見面」，且沒有橫向捲動', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('heading', { level: 1 })).toContainText('有些想念')
    await expect(page.getByText('即將與你見面').first()).toBeVisible()
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
    expect(overflow).toBeLessThanOrEqual(0)
  })

  test('點擊寵物會出現短句並開心', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('button', { name: '摸摸小夥伴' }).click()
    await expect(page.getByRole('status')).not.toBeEmpty()
  })

  test('預覽標示與頁尾透明說明存在', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByText('互動預覽').first()).toBeVisible()
    await expect(page.getByText('各項功能仍在準備中')).toBeVisible()
  })

  test('沒有內部術語與假的註冊／下載按鈕', async ({ page }) => {
    await page.goto('/')
    const text = await page.locator('body').innerText()
    for (const word of ['Roadmap', 'ESP-IDF', 'API', '市場', '競品', 'Phase']) {
      expect(text).not.toContain(word)
    }
    await expect(page.getByRole('button', { name: /下載|註冊|加入/ })).toHaveCount(0)
  })
})
