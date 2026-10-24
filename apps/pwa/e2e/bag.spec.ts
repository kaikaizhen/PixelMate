import { expect, test } from '@playwright/test'
import { BAG, PET, mockApi } from './helpers'

const EQUIP = { hat: null, outfit: null, head: null, front: null, back: null, feet: null, hand: null, costume: null }

async function openBag(page: import('@playwright/test').Page, bag = BAG) {
  await mockApi(page, {}, { pet: { ...structuredClone(PET), equipment: { ...EQUIP } } as typeof PET, bag })
  await page.goto('login')
  await page.getByLabel('Email').fill('a@example.com')
  await page.getByLabel('密碼').fill('Passw0rd-123')
  await page.getByRole('button', { name: '登入' }).click()
  await page.getByRole('button', { name: '背包' }).click()
  return page.getByRole('dialog', { name: '背包' })
}

const worn = (page: import('@playwright/test').Page) => page.locator('button.pet img[data-item]')

test.describe('背包與穿戴', () => {
  test('空背包顯示提示', async ({ page }) => {
    const dlg = await openBag(page, [])
    await expect(dlg.getByText('還沒有道具，小夥伴會自己去找喔。')).toBeVisible()
  })

  test('列出道具、稀有度與數量；穿上帽子後小夥伴頭上真的出現那張圖，再按一次脫下', async ({ page }) => {
    const dlg = await openBag(page)
    await expect(dlg.getByText('4 種道具')).toBeVisible()
    await expect(dlg.getByRole('button', { name: /野餐帆布鞋，稀有/ })).toContainText('×2')
    await expect(worn(page)).toHaveCount(0)

    await dlg.getByRole('button', { name: '郵局草帽，普通，點一下穿上' }).click()
    await expect(worn(page)).toHaveCount(1)
    await expect(worn(page).first()).toHaveAttribute('src', /sprites\/equipment\/hats\/straw_post\.png/)
    await expect(dlg.getByRole('button', { name: '郵局草帽，普通，穿戴中，點一下脫下' })).toHaveAttribute('aria-pressed', 'true')
    expect(await worn(page).first().evaluate((e) => (e as HTMLImageElement).naturalWidth)).toBe(48)          // the art file really loaded

    await dlg.getByRole('button', { name: '郵局草帽，普通，穿戴中，點一下脫下' }).click()
    await expect(worn(page)).toHaveCount(0)
  })

  test('布偶裝與帽子互斥，布偶裝的尾巴畫在身體後面；鞋子可以同時穿', async ({ page }) => {
    const dlg = await openBag(page)
    await dlg.getByRole('button', { name: /郵局草帽/ }).click()
    await dlg.getByRole('button', { name: /野餐帆布鞋/ }).click()
    await expect(worn(page)).toHaveCount(2)

    await dlg.getByRole('button', { name: /小狗布偶裝/ }).click()
    await expect(worn(page)).toHaveCount(2)                                                        // costume + shoes, the hat is gone
    await expect(page.locator('button.pet img[data-item="equip.hat.straw_post"]')).toHaveCount(0)
    await expect(page.locator('button.pet img[src*="dog_tail"]')).toHaveCount(1)                   // tail layer behind the body
    const order = await page.locator('button.pet .gp > *').evaluateAll((els) => els.map((e) => (e as HTMLElement).getAttribute('src') ?? 'body'))
    expect(order.findIndex((s) => s.includes('dog_tail'))).toBeLessThan(order.indexOf('body'))     // behind
    expect(order.findIndex((s) => s.includes('costumes/dog.png'))).toBeGreaterThan(order.indexOf('body'))   // in front

    await dlg.getByRole('button', { name: /郵局草帽/ }).click()
    await expect(page.locator('button.pet img[src*="dog_tail"]')).toHaveCount(0)                   // the hat pushed the costume off
    await expect(page.locator('button.pet img[data-item="equip.hat.straw_post"]')).toHaveCount(1)
  })

  test('分頁會篩選；房間用品點了只提示，不會穿上', async ({ page }) => {
    const dlg = await openBag(page)
    await dlg.getByRole('tab', { name: '房間' }).click()
    await expect(dlg.getByRole('button', { name: /花園單人床/ })).toBeVisible()
    await expect(dlg.getByRole('button', { name: /郵局草帽/ })).toHaveCount(0)
    await dlg.getByRole('button', { name: /花園單人床/ }).click()
    await expect(dlg.getByRole('alert')).toContainText('很快就會開放')
    await expect(worn(page)).toHaveCount(0)
    await dlg.getByRole('tab', { name: '收藏' }).click()
    await expect(dlg.getByText('這一類還沒有道具。')).toBeVisible()
  })

  test('穿戴的狀態重新整理後還在（以後端為準）', async ({ page }) => {
    const dlg = await openBag(page)
    await dlg.getByRole('button', { name: /小狗布偶裝/ }).click()
    await expect(page.locator('button.pet img[data-item="equip.costume.dog"]')).toHaveCount(1)
    await page.reload()
    await expect(page.locator('button.pet img[data-item="equip.costume.dog"]')).toHaveCount(1)
  })

  test('別的裝置換了裝備：回到這個畫面時小夥伴也跟著換', async ({ page }) => {
    await openBag(page)
    await expect(worn(page)).toHaveCount(0)
    await page.route('**/api/v1/pets/me', (r) =>
      r.request().method() === 'GET'
        ? r.fulfill({ json: { ...structuredClone(PET), revision: 9, equipment: { ...EQUIP, head: 'equip.hat.straw_post' } } })
        : r.fallback())
    await page.evaluate(() => window.dispatchEvent(new Event('focus')))
    await expect(worn(page)).toHaveCount(1)
  })
})
