import { expect, test } from '@playwright/test'

test.describe('台灣味', () => {
  test('日常有珍奶、雞排、海邊消波塊', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('tab', { name: '喝珍奶' }).click()
    await expect(page.getByText('手搖杯一杯')).toBeVisible()
    await page.getByRole('tab', { name: '吃雞排' }).click()
    await expect(page.getByText('比臉還大的雞排')).toBeVisible()
    await page.getByRole('tab', { name: '海邊吹風' }).click()
    await expect(page.getByText('看浪一波一波推過來')).toBeVisible()
    await expect(page.locator('#daily-panel .beach__tetra')).toHaveCount(3)
  })

  test('可以穿上藍白拖', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('button', { name: '藍白拖' }).click()
    await expect(page.getByText('戴著藍白拖')).toBeVisible()
  })

  test('360px 海邊場景沒有橫向捲動', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('tab', { name: '海邊吹風' }).click()
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
    expect(overflow).toBeLessThanOrEqual(0)
  })
})
