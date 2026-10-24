import { defineConfig } from '@playwright/test'

// The API is mocked per test (page.route), so e2e never touches a real database or mailbox.
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  reporter: [['list']],
  use: { baseURL: 'http://localhost:4174/web/', serviceWorkers: 'allow' },
  projects: [
    { name: 'desktop', use: { browserName: 'chromium', viewport: { width: 1440, height: 900 } } },
    { name: 'mobile', use: { browserName: 'chromium', viewport: { width: 360, height: 740 }, isMobile: true, hasTouch: true } },
  ],
  webServer: { command: 'npm run build && npm run preview', url: 'http://localhost:4174/web/', reuseExistingServer: true, timeout: 120_000 },
})
