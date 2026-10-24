import { expect, test } from '@playwright/test'

test.describe('遠方的心意', () => {
  test('點信箱 → 郵差送信 → 開信 → 關信', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('button', { name: /點信箱/ }).click()
    await expect(page.getByText('小郵差正從路的那頭跑來')).toBeVisible()
    await page.getByRole('button', { name: '打開信' }).click({ timeout: 8000 })
    const dialog = page.getByRole('dialog')
    await expect(dialog).toBeVisible()
    await expect(dialog.getByText('示範信件')).toBeVisible()
    await dialog.getByRole('button', { name: '收進口袋' }).click()
    await expect(dialog).toBeHidden()
    await expect(page.getByRole('button', { name: '再收一封' })).toBeVisible()
  })

  test('送信途中信箱不可重複點擊', async ({ page }) => {
    await page.goto('/')
    const box = page.getByRole('button', { name: /點信箱/ })
    await box.click()
    await expect(box).toBeDisabled()
  })

  test('可以再收一封', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('button', { name: /點信箱/ }).click()
    await page.getByRole('button', { name: '打開信' }).click({ timeout: 8000 })
    await page.getByRole('button', { name: '收進口袋' }).click()
    await page.getByRole('button', { name: '再收一封' }).click()
    await expect(page.getByRole('button', { name: /點信箱/ })).toBeEnabled()
  })

  test('Esc 可關閉信件', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('button', { name: /點信箱/ }).click()
    await page.getByRole('button', { name: '打開信' }).click({ timeout: 8000 })
    await page.keyboard.press('Escape')
    await expect(page.getByRole('dialog')).toBeHidden()
  })
})
