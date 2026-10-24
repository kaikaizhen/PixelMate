import { expect, test } from '@playwright/test'
import { PET_L4, mockApi } from './helpers'

async function openPetPanel(page: import('@playwright/test').Page) {
  await page.goto('login')
  await page.getByLabel('Email').fill('a@example.com')
  await page.getByLabel('密碼').fill('Passw0rd-123')
  await page.getByRole('button', { name: '登入' }).click()
  await page.getByRole('button', { name: '小夥伴' }).click()
  return page.getByRole('dialog', { name: '小栗' })
}

test.describe('技能分配', () => {
  test('等級 4 有 3 點：加點會更新點數與格子，用完後加號停用', async ({ page }) => {
    const api = await mockApi(page, {}, { pet: structuredClone(PET_L4) })
    const dlg = await openPetPanel(page)
    await expect(dlg.getByText('可分配')).toContainText('3')
    await expect(page.locator('.chip .badge')).toHaveText('+3')

    await dlg.getByRole('button', { name: '共感 加 1 點' }).click()
    await expect(dlg.getByRole('img', { name: '共感 1 / 5 點' })).toBeVisible()
    await expect(dlg.getByText('可分配')).toContainText('2')
    await dlg.getByRole('button', { name: '智慧 加 1 點' }).click()
    await dlg.getByRole('button', { name: '智慧 加 1 點' }).click()
    await expect(dlg.getByText('可分配')).toContainText('0')
    for (const s of ['共感', '創作', '探索', '智慧']) await expect(dlg.getByRole('button', { name: `${s} 加 1 點` })).toBeDisabled()
    await expect(page.locator('.chip .badge')).toHaveCount(0)
    expect(api.calls.filter((c) => c.path === '/pets/me/skills/allocate').map((c) => c.body.expectedRevision)).toEqual([5, 6, 7])   // each call sends the latest revision
  })

  test('等級 1 沒有點數，所有加號都停用', async ({ page }) => {
    await mockApi(page)
    const dlg = await openPetPanel(page)
    await expect(dlg.getByText('可分配')).toContainText('0')
    await expect(dlg.getByRole('button', { name: '共感 加 1 點' })).toBeDisabled()
    await expect(dlg.getByRole('button', { name: '重置技能' })).toBeDisabled()
  })

  test('重置要按兩次確認，所有點數回來', async ({ page }) => {
    await mockApi(page, {}, { pet: structuredClone(PET_L4) })
    const dlg = await openPetPanel(page)
    await dlg.getByRole('button', { name: '探索 加 1 點' }).click()
    await dlg.getByRole('button', { name: '重置技能' }).click()
    await expect(dlg.getByRole('button', { name: '確定重置？再按一次' })).toBeVisible()      // first tap only asks
    await expect(dlg.getByRole('img', { name: '探索 1 / 5 點' })).toBeVisible()
    await dlg.getByRole('button', { name: '確定重置？再按一次' }).click()
    await expect(dlg.getByRole('img', { name: '探索 0 / 5 點' })).toBeVisible()
    await expect(dlg.getByText('可分配')).toContainText('3')
  })

  test('別的裝置先改過（409）：顯示提示並重新載入最新狀態', async ({ page }) => {
    const pet = structuredClone(PET_L4)
    const api = await mockApi(page, {}, { pet })
    const dlg = await openPetPanel(page)
    api.pet!.revision = 9                                                              // another device changed the pet meanwhile
    api.pet!.skills = { ...api.pet!.skills, creativity: 2 }
    api.pet!.remainingSkillPoints = 1
    await dlg.getByRole('button', { name: '共感 加 1 點' }).click()
    await expect(dlg.getByRole('alert')).toContainText('重新載入')
    await expect(dlg.getByRole('img', { name: '創作 2 / 5 點' })).toBeVisible()          // the screen now shows the server's truth
    await expect(dlg.getByText('可分配')).toContainText('1')
    await dlg.getByRole('button', { name: '共感 加 1 點' }).click()                      // and a retry works
    await expect(dlg.getByRole('img', { name: '共感 1 / 5 點' })).toBeVisible()
  })

  test('單一技能最多 5 點：滿了加號停用', async ({ page }) => {
    const pet = { ...structuredClone(PET_L4), level: 10, remainingSkillPoints: 9 }
    await mockApi(page, {}, { pet })
    const dlg = await openPetPanel(page)
    for (let i = 0; i < 5; i++) await dlg.getByRole('button', { name: '創作 加 1 點' }).click()
    await expect(dlg.getByRole('img', { name: '創作 5 / 5 點' })).toBeVisible()
    await expect(dlg.getByRole('button', { name: '創作 加 1 點' })).toBeDisabled()
    await expect(dlg.getByRole('button', { name: '共感 加 1 點' })).toBeEnabled()
  })
})
