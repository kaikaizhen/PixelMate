/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

// The PWA is served under /web/ and talks to the API on the same origin (/api/), so the session cookie is first-party.
export default defineConfig({
  base: '/web/',
  plugins: [
    vue(),
    VitePWA({
      registerType: 'prompt',
      includeAssets: ['icons/*.png'],
      manifest: {
        id: '/web/',
        name: '小伴星 PixelMate',
        short_name: '小伴星',
        description: '和你的像素小夥伴一起生活的小世界。',
        lang: 'zh-Hant',
        start_url: '/web/',
        scope: '/web/',
        display: 'standalone',
        orientation: 'portrait',
        background_color: '#fff8ec',
        theme_color: '#fff8ec',
        icons: [
          { src: 'icons/icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: 'icons/icon-512.png', sizes: '512x512', type: 'image/png' },
          { src: 'icons/maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        globPatterns: ['**/*.{js,css,html,png,woff2,svg,webmanifest}'],
        globIgnores: ['game/**'],                   // 600+ game files: fetched when needed, not precached
        navigateFallback: '/web/index.html',
        navigateFallbackDenylist: [/^\/api\//],     // never answer API calls from the cache
        cleanupOutdatedCaches: true,
        // static game art is cached after first use; API responses are never stored
        runtimeCaching: [{ urlPattern: ({ url }) => url.pathname.startsWith('/web/game/'), handler: 'CacheFirst', options: { cacheName: 'game-assets', expiration: { maxEntries: 400, maxAgeSeconds: 60 * 60 * 24 * 30 } } }],
      },
      devOptions: { enabled: false },
    }),
  ],
  resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
  server: {
    port: 5173,
    proxy: { '/api': { target: 'http://localhost:5200', changeOrigin: false, ws: true } },   // ws: the SignalR link
  },
  preview: { port: 4174, proxy: { '/api': { target: 'http://localhost:5200', ws: true } } },
  build: { assetsInlineLimit: 0 },
  test: { environment: 'jsdom', include: ['src/**/*.spec.ts'] },
})
