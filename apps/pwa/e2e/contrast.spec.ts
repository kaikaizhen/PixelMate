import { expect, test } from '@playwright/test'
import { BAG, PET, mockApi } from './helpers'

const EQUIP = { hat: null, outfit: null, head: null, front: null, back: null, feet: null, hand: null, costume: null }
const FRIEND = { relationshipId: 'r1', kind: 'friend', since: '', other: { userId: 'u2', displayName: '阿福', petName: '麻糬', bodyVariant: 'star_01_mint' } }

/** WCAG contrast of an element's text colour against its own background colour. */
async function contrast(page: import('@playwright/test').Page, selector: string) {
  return page.locator(selector).first().evaluate((el) => {
    const parse = (c: string) => c.match(/[\d.]+/g)!.slice(0, 3).map(Number)
    const lum = ([r, g, b]: number[]) => { const f = (v: number) => { v /= 255; return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4 }; return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b) }
    const cs = getComputedStyle(el)
    const a = lum(parse(cs.color)), b = lum(parse(cs.backgroundColor))
    return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05)
  })
}

for (const scheme of ['light', 'dark'] as const) {
  test.describe(`文字與底色要看得清楚（${scheme === 'dark' ? '深色' : '淺色'}模式）`, () => {
    test.use({ colorScheme: scheme })

    async function start(page: import('@playwright/test').Page, location: unknown = undefined) {
      const state = await mockApi(page, {}, { pet: { ...structuredClone(PET), equipment: { ...EQUIP } } as typeof PET, bag: BAG })
      state.friends = [FRIEND]
      if (location !== undefined) state.location = location
      await page.goto('login')
      await page.getByLabel('Email').fill('a@example.com')
      await page.getByLabel('密碼').fill('Passw0rd-123')
      await page.getByRole('button', { name: '登入' }).click()
      await expect(page.getByRole('button', { name: '背包' })).toBeVisible()
    }

    test('所在地面板：「使用目前位置」按鈕與選單', async ({ page }) => {
      await start(page, null)
      const dlg = page.getByRole('dialog', { name: '所在地與天氣' })
      await expect(dlg).toBeVisible()
      expect(await contrast(page, '[aria-label="所在地與天氣"] .primary')).toBeGreaterThanOrEqual(4.5)
      expect(await contrast(page, '[aria-label="所在地與天氣"] select')).toBeGreaterThanOrEqual(4.5)
    })

    test('信箱：分頁、寫信按鈕、輸入欄、信紙、配對按鈕', async ({ page }) => {
      await start(page)
      await page.getByRole('button', { name: /^信箱/ }).click()
      const dlg = page.getByRole('dialog', { name: '信箱' })
      expect(await contrast(page, '[aria-label="信箱"] .tabs button.on')).toBeGreaterThanOrEqual(4.5)
      expect(await contrast(page, '[aria-label="信箱"] .primary')).toBeGreaterThanOrEqual(4.5)
      await dlg.getByRole('button', { name: /寫信/ }).click()
      for (const sel of ['textarea', 'select']) expect(await contrast(page, `[aria-label="信箱"] ${sel}`), sel).toBeGreaterThanOrEqual(4.5)
      await dlg.getByRole('button', { name: '取消' }).click()
      await dlg.getByRole('tab', { name: '好友' }).click()
      expect(await contrast(page, '[aria-label="信箱"] .kinds button.on')).toBeGreaterThanOrEqual(4.5)
      expect(await contrast(page, '[aria-label="信箱"] .enter input')).toBeGreaterThanOrEqual(4.5)
    })

    test('布置：完成、收起、選取的分類', async ({ page }) => {
      await start(page)
      await page.getByRole('button', { name: '布置', exact: true }).click()
      const dlg = page.getByRole('dialog', { name: '布置小屋' })
      await expect(dlg).toBeVisible()
      expect(await contrast(page, '[aria-label="布置小屋"] .done')).toBeGreaterThanOrEqual(4.5)
      expect(await contrast(page, '[aria-label="布置小屋"] .tabs button.on')).toBeGreaterThanOrEqual(4.5)
      await page.locator('[data-placed]').first().click()
      expect(await contrast(page, '[aria-label="布置小屋"] .remove')).toBeGreaterThanOrEqual(4.5)
    })

    test('首頁：技能加號、背包分頁', async ({ page }) => {
      await start(page)
      await page.getByRole('button', { name: '背包' }).click()
      expect(await contrast(page, '[aria-label="背包"] .tabs button.on')).toBeGreaterThanOrEqual(4.5)
    })
  })
}
