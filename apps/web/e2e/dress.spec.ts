import { expect, test } from '@playwright/test'

const visibleLayers = (page: import('@playwright/test').Page) =>
  page.locator('.dress__layer').evaluateAll((els) => els.filter((el) => getComputedStyle(el).visibility === 'visible').length)

test.describe('長成獨一無二的模樣', () => {
  test('預設不穿；點草帽、圍巾可疊穿', async ({ page }) => {
    await page.goto('/')
    expect(await visibleLayers(page)).toBe(0)
    await page.getByRole('button', { name: '草帽' }).click()
    await page.getByRole('button', { name: '藍圍巾' }).click()
    expect(await visibleLayers(page)).toBe(2)
    await expect(page.getByText('戴著草帽、藍圍巾')).toBeVisible()
  })

  test('換毛線帽會取代草帽，再點一次脫下', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('button', { name: '草帽' }).click()
    await page.getByRole('button', { name: '毛線帽' }).click()
    await expect(page.getByRole('button', { name: '草帽' })).toHaveAttribute('aria-pressed', 'false')
    expect(await visibleLayers(page)).toBe(1)
    await page.getByRole('button', { name: '毛線帽' }).click()
    expect(await visibleLayers(page)).toBe(0)
  })

  test('全部脫下', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('button', { name: '草帽' }).click()
    await page.getByRole('button', { name: '全部脫下' }).click()
    expect(await visibleLayers(page)).toBe(0)
  })

  test('所有圖層動畫同步（相同動畫名稱與時長）', async ({ page }) => {
    await page.goto('/')
    const sig = await page.locator('.dress__pet > .sprite').evaluateAll((els) =>
      els.slice(0, 4).map((el) => getComputedStyle(el).animationName + getComputedStyle(el).animationDuration),
    )
    expect(new Set(sig).size).toBe(1)
  })
})
