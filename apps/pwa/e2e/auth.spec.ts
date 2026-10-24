import { expect, test } from '@playwright/test'
import { json, mockApi } from './helpers'

test.describe('登入', () => {
  test('未登入開私有頁會導向登入，登入後回首頁並顯示暱稱', async ({ page }) => {
    const api = await mockApi(page)
    await page.goto('./')
    await expect(page).toHaveURL(/\/web\/login/)
    await page.getByLabel('Email').fill('a@example.com')
    await page.getByLabel('密碼').fill('Passw0rd-123')
    await page.getByRole('button', { name: '登入' }).click()
    await expect(page.getByRole('heading', { name: /嗨，小栗/ })).toBeVisible()
    expect(api.calls.find((c) => c.path === '/auth/login')?.csrf).toBe('pixelmate')

    await page.reload()                                              // the session cookie keeps the player signed in
    await expect(page.getByRole('heading', { name: /嗨，小栗/ })).toBeVisible()
    await page.getByRole('button', { name: '登出' }).click()
    await expect(page).toHaveURL(/\/web\/login/)
  })

  test('帳密錯誤顯示提示，密碼可以切換顯示', async ({ page }) => {
    await mockApi(page, { '/auth/login': (r) => json(r, 401, { code: 'AUTH_REQUIRED', message: 'Email 或密碼不正確。', requestId: 'r' }) })
    await page.goto('login')
    await page.getByLabel('Email').fill('a@example.com')
    const pw = page.getByLabel('密碼')
    await pw.fill('Wrong-pass-1')
    await expect(pw).toHaveAttribute('type', 'password')
    await page.getByRole('button', { name: '顯示' }).click()
    await expect(pw).toHaveAttribute('type', 'text')
    await page.getByRole('button', { name: '登入' }).click()
    await expect(page.getByRole('alert')).toContainText('Email 或密碼不正確')
  })

  test('Email 還沒確認時可以重新寄確認信', async ({ page }) => {
    const api = await mockApi(page, {
      '/auth/login': (r) => json(r, 403, { code: 'EMAIL_NOT_CONFIRMED', message: '請先到信箱點擊確認連結。', requestId: 'r' }),
      '/auth/resend-confirmation': (r) => json(r, 202),
    })
    await page.goto('login')
    await page.getByLabel('Email').fill('a@example.com')
    await page.getByLabel('密碼').fill('Passw0rd-123')
    await page.getByRole('button', { name: '登入' }).click()
    await page.getByRole('button', { name: '重新寄送確認信' }).click()
    await expect(page.getByText('已重新寄出確認信')).toBeVisible()
    expect(api.calls.some((c) => c.path === '/auth/resend-confirmation')).toBe(true)
  })

  test('登入成功後不會跳到外部網址', async ({ page }) => {
    await mockApi(page)
    await page.goto('login?redirect=//evil.example')
    await page.getByLabel('Email').fill('a@example.com')
    await page.getByLabel('密碼').fill('Passw0rd-123')
    await page.getByRole('button', { name: '登入' }).click()
    await expect(page).toHaveURL(/localhost:4174\/web\/$/)
  })
})

test.describe('註冊', () => {
  test('填寫表單後顯示成功畫面，即時密碼規則會打勾', async ({ page }) => {
    const api = await mockApi(page, { '/auth/register': (r) => json(r, 201, { userId: 'u', emailConfirmationRequired: true }) })
    await page.goto('register')
    await page.getByLabel('暱稱').fill('小栗')
    await page.getByLabel('Email').fill('new@example.com')
    await page.getByLabel('密碼', { exact: true }).fill('abc')
    await expect(page.locator('.rules li.ok')).toHaveCount(1)
    await page.getByLabel('密碼', { exact: true }).fill('Passw0rd-123')
    await expect(page.locator('.rules li.ok')).toHaveCount(3)
    await page.getByRole('button', { name: '註冊' }).click()
    await expect(page.getByRole('heading', { name: '註冊完成' })).toBeVisible()
    await expect(page.getByText('確認信')).toBeVisible()
    expect(api.calls.find((c) => c.path === '/auth/register')?.body).toEqual({ email: 'new@example.com', password: 'Passw0rd-123', displayName: '小栗' })
  })

  test('Email 已被註冊時顯示伺服器訊息', async ({ page }) => {
    await mockApi(page, { '/auth/register': (r) => json(r, 409, { code: 'EMAIL_ALREADY_REGISTERED', message: '這個 Email 已經註冊過了。', requestId: 'r' }) })
    await page.goto('register')
    await page.getByLabel('暱稱').fill('小栗')
    await page.getByLabel('Email').fill('a@example.com')
    await page.getByLabel('密碼', { exact: true }).fill('Passw0rd-123')
    await page.getByRole('button', { name: '註冊' }).click()
    await expect(page.getByRole('alert')).toContainText('已經註冊過')
  })

  test('欄位不合法時不送出並標出錯誤', async ({ page }) => {
    const api = await mockApi(page)
    await page.goto('register')
    await page.getByRole('button', { name: '註冊' }).click()
    await expect(page.getByText('請輸入暱稱。')).toBeVisible()
    await expect(page.getByLabel('Email')).toHaveAttribute('aria-invalid', 'true')
    expect(api.calls.some((c) => c.path === '/auth/register')).toBe(false)
  })
})

test.describe('忘記與重設密碼', () => {
  test('忘記密碼一律顯示相同的成功訊息', async ({ page }) => {
    await mockApi(page, { '/auth/forgot-password': (r) => json(r, 202) })
    await page.goto('forgot-password')
    await page.getByLabel('Email').fill('ghost@example.com')
    await page.getByRole('button', { name: '寄出重設連結' }).click()
    await expect(page.getByText('如果這個 Email 已經註冊過')).toBeVisible()
  })

  test('從信件連結進入、兩次密碼要一致，成功後回登入頁', async ({ page }) => {
    const api = await mockApi(page, { '/auth/reset-password': (r) => json(r, 204) })
    await page.goto('reset-password?email=a%40example.com&token=abc123')
    await page.getByLabel('新密碼', { exact: true }).fill('Brand-new-pass9')
    await page.getByLabel('再輸入一次新密碼').fill('different-pass-9')
    await page.getByRole('button', { name: '更新密碼' }).click()
    await expect(page.getByText('兩次輸入的密碼不一樣')).toBeVisible()
    await page.getByLabel('再輸入一次新密碼').fill('Brand-new-pass9')
    await page.getByRole('button', { name: '更新密碼' }).click()
    await expect(page).toHaveURL(/\/web\/login/)
    await expect(page.getByText('密碼已經更新')).toBeVisible()
    expect(api.calls.find((c) => c.path === '/auth/reset-password')?.body).toEqual({ email: 'a@example.com', token: 'abc123', newPassword: 'Brand-new-pass9' })
  })

  test('連結過期時提供重新申請', async ({ page }) => {
    await mockApi(page, { '/auth/reset-password': (r) => json(r, 422, { code: 'INVALID_TOKEN', message: '重設連結無效或已過期。', requestId: 'r' }) })
    await page.goto('reset-password?email=a%40example.com&token=old')
    await page.getByLabel('新密碼', { exact: true }).fill('Brand-new-pass9')
    await page.getByLabel('再輸入一次新密碼').fill('Brand-new-pass9')
    await page.getByRole('button', { name: '更新密碼' }).click()
    await expect(page.getByRole('alert')).toContainText('無效或已過期')
    await expect(page.getByRole('link', { name: '重新申請' })).toBeVisible()
  })

  test('缺少參數的重設連結會提示', async ({ page }) => {
    await mockApi(page)
    await page.goto('reset-password')
    await expect(page.getByText('這個連結不完整')).toBeVisible()
  })
})

test.describe('確認 Email', () => {
  test('成功與失敗', async ({ page }) => {
    await mockApi(page, { '/auth/confirm-email': (r, b) => (b.token === 'good' ? json(r, 204) : json(r, 422, { code: 'INVALID_TOKEN', message: '確認連結無效或已過期。', requestId: 'r' })) })
    await page.goto('confirm-email?userId=11111111-1111-1111-1111-111111111111&token=good')
    await expect(page.getByText('Email 已確認')).toBeVisible()
    await page.goto('confirm-email?userId=11111111-1111-1111-1111-111111111111&token=bad')
    await expect(page.getByRole('alert')).toContainText('無效或已過期')
  })
})
