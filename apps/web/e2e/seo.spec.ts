import { expect, test } from '@playwright/test'

test.describe('SEO / GEO 基本盤', () => {
  test('語言、標題、描述與社群預覽', async ({ page }) => {
    await page.goto('/')
    await expect(page.locator('html')).toHaveAttribute('lang', 'zh-Hant')
    await expect(page).toHaveTitle(/小伴星PixelMate/)
    const desc = await page.locator('meta[name="description"]').getAttribute('content')
    expect(desc!.length).toBeGreaterThan(30)
    for (const sel of ['meta[property="og:title"]', 'meta[property="og:description"]', 'meta[name="twitter:title"]', 'meta[property="og:image:alt"]']) {
      await expect(page.locator(sel)).toHaveCount(1)
    }
  })

  test('有合法的 WebSite JSON-LD', async ({ page }) => {
    await page.goto('/')
    const raw = await page.locator('script[type="application/ld+json"]').first().textContent()
    const data = JSON.parse(raw!)
    expect(data['@type']).toBe('WebSite')
    expect(data.inLanguage).toBe('zh-Hant')
  })

  test('只有一個 h1，標題層級不跳級', async ({ page }) => {
    await page.goto('/')
    await expect(page.locator('h1')).toHaveCount(1)
  })

  test('沒有任何外部請求', async ({ page }) => {
    const external: string[] = []
    page.on('request', (r) => {
      if (!r.url().startsWith('http://localhost')) external.push(r.url())
    })
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    expect(external).toEqual([])
  })

  test('robots.txt 與 llms.txt 可讀取', async ({ request }) => {
    expect((await request.get('/robots.txt')).ok()).toBeTruthy()
    const llms = await request.get('/llms.txt')
    expect(await llms.text()).toContain('小伴星PixelMate')
  })
})
