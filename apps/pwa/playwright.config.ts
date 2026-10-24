import { defineConfig } from '@playwright/test'

// The API is mocked per test (page.route), so e2e never touches a real database or mailbox.
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  reporter: [['list']],
  use: { baseURL: 'http://localhost:4174/web/', serviceWorkers: 'block' },   // only pwa.spec.ts turns the service worker on
  projects: [
    { name: 'desktop', use: { browserName: 'chromium', viewport: { width: 1440, height: 900 } } },
    { name: 'mobile', use: { browserName: 'chromium', viewport: { width: 393, height: 852 }, deviceScaleFactor: 3, isMobile: true, hasTouch: true } },
  ],
  webServer: { command: 'npm run build && npm run preview', url: 'http://localhost:4174/web/', reuseExistingServer: true, timeout: 120_000 },
})
