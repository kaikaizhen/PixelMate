import { expect, test } from '@playwright/test'
import { PET, mockApi } from './helpers'

const EQUIP = { hat: null, outfit: null, head: null, front: null, back: null, feet: null, hand: null, costume: null }
const item = (id: string, name: string, category: string, quantity = 1) => ({ inventoryId: 'i-' + id, itemId: id, name, category, rarity: 'common', slot: null, assetKey: id, quantity, equipped: false })
const BAG = [item('world.furniture.table_garden', '花園矮木桌', 'furniture'), item('world.furniture.bed_garden', '花園單人床', 'furniture'), item('world.decor.bouquet_garden', '花園花束', 'decor', 2), item('world.decor.floor_tile_garden', '花園地板拼貼', 'decor')]

async function start(page: import('@playwright/test').Page) {
  const state = await mockApi(page, {}, { pet: { ...structuredClone(PET), equipment: { ...EQUIP } } as typeof PET, bag: BAG as never })
  await page.goto('login')
  await page.getByLabel('Email').fill('a@example.com')
  await page.getByLabel('密碼').fill('Passw0rd-123')
  await page.getByRole('button', { name: '登入' }).click()
  await expect(page.locator('[data-placed="world.furniture.table_garden"]')).toBeVisible()      // the starter table from the server's room
  return state
}

const placed = (page: import('@playwright/test').Page) => page.locator('[data-placed]')

test.describe('布置小屋', () => {
  test('背包點房間道具 → 進入布置：放進去、拖曳移動、儲存後重新整理仍在', async ({ page }) => {
    const state = await start(page)
    await page.getByRole('button', { name: '背包' }).click()
    await page.getByRole('dialog', { name: '背包' }).getByRole('button', { name: /花園單人床/ }).click()
    const dlg = page.getByRole('dialog', { name: '布置小屋' })
    await expect(dlg).toBeVisible()
    await expect(placed(page)).toHaveCount(2)

    const bed = page.locator('[data-placed="world.furniture.bed_garden"]')
    const before = await bed.boundingBox()
    await page.mouse.move(before!.x + before!.width / 2, before!.y + before!.height / 2)
    await page.mouse.down()
    await page.mouse.move(before!.x + before!.width / 2 - 60, before!.y + before!.height / 2 - 30, { steps: 6 })
    await page.mouse.up()
    const after = await bed.boundingBox()
    expect(after!.x).toBeLessThan(before!.x - 30)
    expect(after!.y).toBeLessThan(before!.y - 10)

    await dlg.getByRole('button', { name: '完成' }).click()
    await expect(dlg).toBeHidden()
    const saved = state.room.placements.find((p: any) => p.itemId === 'world.furniture.bed_garden')
    expect(saved).toBeTruthy()
    expect(saved.x).toBeLessThan(100)
    expect(state.room.revision).toBe(2)

    await page.reload()
    await expect(page.locator('[data-placed="world.furniture.bed_garden"]')).toBeVisible()
    await expect(placed(page)).toHaveCount(2)
  })

  test('取消不會存；收起選取的東西；每個道具不能擺超過擁有的數量', async ({ page }) => {
    const state = await start(page)
    await page.getByRole('button', { name: '背包' }).click()
    await page.getByRole('dialog', { name: '背包' }).getByRole('button', { name: '🏠 布置小屋' }).click()
    const dlg = page.getByRole('dialog', { name: '布置小屋' })
    await dlg.getByRole('tab', { name: '布置' }).click()
    await dlg.getByRole('button', { name: /花園花束，還有 2 個可以擺/ }).click()
    await dlg.getByRole('tab', { name: '布置' }).click()
    await dlg.getByRole('button', { name: /花園花束，還有 1 個可以擺/ }).click()
    await expect(placed(page)).toHaveCount(3)
    await dlg.getByRole('button', { name: /花園花束，還有 0 個可以擺/ }).click()
    await expect(dlg.getByRole('alert')).toContainText('都已經擺出來了')
    await expect(placed(page)).toHaveCount(3)

    await dlg.getByRole('button', { name: '收起選取的' }).click()
    await expect(placed(page)).toHaveCount(2)
    await dlg.getByRole('button', { name: '取消' }).click()
    await expect(placed(page)).toHaveCount(1)                                              // back to what was saved
    expect(state.room.revision).toBe(1)
  })

  test('預設的桌子也可以收起來；換地板後整個地板換成那款', async ({ page }) => {
    const state = await start(page)
    await page.getByRole('button', { name: '背包' }).click()
    await page.getByRole('dialog', { name: '背包' }).getByRole('button', { name: '🏠 布置小屋' }).click()
    const dlg = page.getByRole('dialog', { name: '布置小屋' })
    await page.locator('[data-placed="world.furniture.table_garden"]').click()
    await dlg.getByRole('button', { name: '收起選取的' }).click()
    await expect(placed(page)).toHaveCount(0)

    await dlg.getByRole('tab', { name: '地板' }).click()
    await dlg.getByRole('button', { name: /花園地板拼貼.*換上/ }).click()
    await expect(page.locator('[data-floor]')).toHaveCount(1)
    await dlg.getByRole('button', { name: '完成' }).click()
    expect(state.room.floorItemId).toBe('world.decor.floor_tile_garden')
    expect(state.room.placements).toHaveLength(0)
    await expect(page.locator('[data-floor]')).toHaveCount(1)
  })

  test('另一台裝置先存了：這台儲存會收到提示並重新載入', async ({ page }) => {
    const state = await start(page)
    await page.getByRole('button', { name: '背包' }).click()
    await page.getByRole('dialog', { name: '背包' }).getByRole('button', { name: /花園單人床/ }).click()
    state.room = { ...state.room, revision: 5, placements: [] }                               // someone else saved meanwhile
    const dlg = page.getByRole('dialog', { name: '布置小屋' })
    await dlg.getByRole('button', { name: '完成' }).click()
    await expect(page.getByRole('dialog', { name: '布置小屋' })).toBeHidden()
    await expect(placed(page)).toHaveCount(0)                                                // shows the other device's room
  })

  test('手機上布置面板不會讓頁面捲動，小夥伴不能在布置時被點', async ({ page }) => {
    await start(page)
    await page.getByRole('button', { name: '背包' }).click()
    await page.getByRole('dialog', { name: '背包' }).getByRole('button', { name: '🏠 布置小屋' }).click()
    expect(await page.evaluate(() => document.documentElement.scrollHeight > window.innerHeight + 1)).toBe(false)
    await expect(page.locator('button.pet')).toBeDisabled()
  })
})
