import { expect, test } from '@playwright/test'

test.describe('每一天，窗外的世界都不同', () => {
  test('切換晴／雨／夜會更換文案與窗外動畫', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByText('牠想出去散散步')).toBeVisible()
    await page.getByRole('radio', { name: '雨天' }).click()
    await expect(page.getByText('牠窩在窗邊')).toBeVisible()
    await expect(page.locator('#weather .win__drop').first()).toBeAttached()
    await page.getByRole('radio', { name: '夜晚' }).click()
    await expect(page.getByText('牠睡得很沉')).toBeVisible()
    await expect(page.locator('#weather .win__moon')).toBeAttached()
    await page.getByRole('radio', { name: '晴天' }).click()
    await expect(page.locator('#weather .win__drop')).toHaveCount(0)
  })

  test('晴天時角色在散步（位移動畫）', async ({ page }) => {
    await page.goto('/')
    const name = await page.locator('#weather .wr__walker').evaluate((el) => getComputedStyle(el).animationName)
    expect(name).not.toBe('none')
  })

  test('標明為模擬天氣', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByText('不代表任何地區的即時天氣')).toBeVisible()
  })

  test('360px 無橫向捲動', async ({ page }) => {
    await page.goto('/')
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
    expect(overflow).toBeLessThanOrEqual(0)
  })
})
