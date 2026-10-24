import { expect, test } from '@playwright/test'

test.describe('牠有自己的日常', () => {
  test('「往下認識牠」會捲到日常區塊', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('link', { name: /往下認識牠/ }).click()
    await expect(page.getByRole('heading', { name: '牠有自己的日常' })).toBeInViewport()
  })

  test('點分頁會更換活動說明與動畫', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('tab', { name: '看書' }).click()
    await expect(page.getByRole('tab', { name: '看書' })).toHaveAttribute('aria-selected', 'true')
    await expect(page.getByText('翻開一本小小的書')).toBeVisible()
    const sprite = page.locator('#daily-panel .sprite').last()
    expect(await sprite.evaluate((el) => getComputedStyle(el).animationName)).not.toBe('none')
  })

  test('看雨時窗外下雨', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('tab', { name: '看雨' }).click()
    await expect(page.locator('#daily-panel .win__drop').first()).toBeAttached()
  })

  test('方向鍵可切換分頁', async ({ page }) => {
    await page.goto('/')
    const tab = page.getByRole('tab', { name: '午睡' })
    await tab.click()
    await tab.press('ArrowRight')
    await expect(page.getByRole('tab', { name: '看書' })).toHaveAttribute('aria-selected', 'true')
  })

  test('360px 沒有橫向捲動', async ({ page }) => {
    await page.goto('/')
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
    expect(overflow).toBeLessThanOrEqual(0)
  })
})
