import { expect, test } from '@playwright/test'
import { BAG, PET, mockApi } from './helpers'

const EQUIP = { hat: null, outfit: null, head: null, front: null, back: null, feet: null, hand: null, costume: null }
const LETTER = (over = {}) => ({ letterId: 'l1', otherUserId: 'u-fu', otherDisplayName: '阿福', preview: '今天路過花店，想到你。', body: '今天路過花店，想到你。\n下次一起去！', sentAt: '2026-10-25T01:00:00', deliverAt: '2026-10-25T01:10:00', delivered: true, readAt: null, hasGift: false, ...over })
const FRIEND = { relationshipId: 'r1', kind: 'friend', since: '2026-10-20T00:00:00Z', other: { userId: 'u-fu', displayName: '阿福', petName: '麻糬', bodyVariant: 'star_01_mint' } }

async function start(page: import('@playwright/test').Page, setup?: (s: Awaited<ReturnType<typeof mockApi>>) => void) {
  const state = await mockApi(page, {}, { pet: { ...structuredClone(PET), equipment: { ...EQUIP } } as typeof PET, bag: BAG })
  setup?.(state)
  await page.goto('login')
  await page.getByLabel('Email').fill('a@example.com')
  await page.getByLabel('密碼').fill('Passw0rd-123')
  await page.getByRole('button', { name: '登入' }).click()
  await expect(page.getByRole('button', { name: /^信箱/ })).toBeEnabled()
  return state
}

test.describe('信箱與好友', () => {
  test('沒有好友時：收件匣是空的，寫信按鈕不能按，提示先交朋友', async ({ page }) => {
    await start(page)
    await page.getByRole('button', { name: /^信箱/ }).click()
    const dlg = page.getByRole('dialog', { name: '信箱' })
    await expect(dlg.getByText('還沒有收到信。')).toBeVisible()
    await expect(dlg.getByRole('button', { name: /寫信/ })).toBeDisabled()
    await expect(dlg.getByText('先到「好友」交個朋友')).toBeVisible()
  })

  test('用邀請碼配對：預覽邀請的人再確認；產生邀請碼會顯示一次', async ({ page }) => {
    await start(page)
    await page.getByRole('button', { name: /^信箱/ }).click()
    const dlg = page.getByRole('dialog', { name: '信箱' })
    await dlg.getByRole('tab', { name: '好友' }).click()
    await dlg.getByRole('button', { name: '產生邀請碼' }).click()
    await expect(dlg.getByLabel('邀請碼').first()).toHaveText('ABCD-2345')

    await dlg.getByRole('textbox', { name: '邀請碼' }).fill('zzzz-zzzz')
    await dlg.getByRole('button', { name: '查看' }).click()
    await expect(dlg.getByRole('alert')).toContainText('找不到這組邀請碼')
    await dlg.getByRole('textbox', { name: '邀請碼' }).fill('abcd-2345')
    await dlg.getByRole('button', { name: '查看' }).click()
    await expect(dlg.getByText('阿福（麻糬） 想和你成為好友')).toBeVisible()
    await dlg.getByRole('button', { name: '確認配對' }).click()
    await expect(dlg.getByText('你和阿福成為好友了！')).toBeVisible()
    await expect(dlg.getByRole('list', { name: '好友' })).toContainText('麻糬')

    await dlg.getByRole('button', { name: '解除' }).click()                              // two taps: the first only asks
    await expect(dlg.getByRole('list', { name: '好友' })).toBeVisible()
    await dlg.getByRole('button', { name: '確定？' }).click()
    await expect(dlg.getByText('還沒有好友。')).toBeVisible()
  })

  test('寫信：短句、字數、信紙與禮物；同一封信重送用同一把 Idempotency-Key', async ({ page }) => {
    const state = await start(page, (s) => { s.friends = [FRIEND] })
    await page.getByRole('button', { name: /^信箱/ }).click()
    const dlg = page.getByRole('dialog', { name: '信箱' })
    await dlg.getByRole('button', { name: /寫信/ }).click()
    await expect(dlg.getByRole('button', { name: '請郵差送信' })).toBeDisabled()                // nothing written yet
    await dlg.getByRole('button', { name: '謝謝你！' }).click()
    await dlg.getByLabel('信的內容').fill('謝謝你！\n今天很開心。')
    await expect(dlg.getByText('還可以寫 489 字')).toBeVisible()
    await dlg.getByLabel('禮物').selectOption({ label: '郵局草帽' })
    await dlg.getByRole('button', { name: '請郵差送信' }).click()
    await expect(dlg.getByText('郵差出發了！')).toBeVisible()
    const sends = state.calls.filter((c) => c.path === '/mail' && c.body)
    expect(sends).toHaveLength(1)
    expect(sends[0].key).toMatch(/^[0-9a-f-]{36}$/)
    expect(sends[0].body).toMatchObject({ recipientUserId: 'u-fu', giftItemId: 'equip.hat.straw_post' })
    await expect(dlg.getByText('🚶 郵差送信中…')).toBeVisible()                              // the sent box shows it is on its way
  })

  test('收信：信箱有未讀數；打開信才標已讀並顯示全文與禮物', async ({ page }) => {
    await start(page, (s) => { s.friends = [FRIEND]; s.inbox = [LETTER({ hasGift: true, gift: { itemId: 'equip.hat.straw_post', name: '郵局草帽', assetKey: 'equip.hat.straw_post', rarity: 'common' } })] })
    const tool = page.getByRole('button', { name: '信箱，1 封未讀' })
    await expect(tool).toBeVisible()
    await tool.click()
    const dlg = page.getByRole('dialog', { name: '信箱' })
    await expect(dlg.getByRole('button', { name: /阿福/ })).toContainText('今天路過花店')
    await dlg.getByRole('button', { name: /阿福/ }).click()
    await expect(dlg.getByLabel('信件內容')).toContainText('下次一起去！')
    await expect(dlg.getByText('收到禮物：郵局草帽，已放進背包！')).toBeVisible()
    await expect(page.getByRole('button', { name: '信箱' })).toBeVisible()                    // no unread left
    await dlg.getByRole('button', { name: '刪除' }).click()
    await dlg.getByRole('button', { name: '確定刪除？再按一次' }).click()
    await expect(dlg.getByText('還沒有收到信。')).toBeVisible()
  })

  test('信件內容只當文字顯示（不執行 HTML）', async ({ page }) => {
    await start(page, (s) => { s.friends = [FRIEND]; s.inbox = [LETTER({ body: '<img src=x onerror="window.__pwn=1"><b>hi</b>', preview: 'x' })] })
    await page.getByRole('button', { name: /^信箱/ }).click()
    const dlg = page.getByRole('dialog', { name: '信箱' })
    await dlg.getByRole('button', { name: /阿福/ }).click()
    await expect(dlg.getByLabel('信件內容')).toContainText('<b>hi</b>')
    expect(await page.evaluate(() => (window as unknown as { __pwn?: number }).__pwn)).toBeUndefined()
  })

  test('寫信畫面在手機上放得下，頁面不會捲動', async ({ page }) => {
    await start(page, (s) => { s.friends = [FRIEND] })
    await page.getByRole('button', { name: /^信箱/ }).click()
    await page.getByRole('dialog', { name: '信箱' }).getByRole('button', { name: /寫信/ }).click()
    expect(await page.evaluate(() => document.documentElement.scrollHeight > window.innerHeight + 1)).toBe(false)
  })

  test('新信寄到：信箱出現紅色數字並跳出提示', async ({ page }) => {
    const state = await start(page, (s) => { s.friends = [FRIEND] })
    await expect(page.getByRole('button', { name: '信箱' })).toBeVisible()
    state.inbox = [LETTER()]
    await page.evaluate(() => window.dispatchEvent(new Event('focus')))
    await expect(page.getByText('郵差送來一封信！')).toBeVisible()
    await expect(page.getByRole('button', { name: '信箱，1 封未讀' })).toBeVisible()
  })
})
