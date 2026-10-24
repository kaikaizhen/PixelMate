/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  build: {
    // 像素圖一律輸出為檔案（可長期快取），不內嵌進 HTML
    assetsInlineLimit: 0,
  },
  ssgOptions: {
    script: 'async',
    formatting: 'minify',
    // CSS 很小：整份內嵌進 HTML，避免擋住首次渲染（不使用 onload 載入法，CSP 禁止行內事件）
    beastiesOptions: { preload: 'body', inlineThreshold: 60000, minimumExternalSize: 0 },
  },
  test: {
    environment: 'jsdom',
    include: ['src/**/*.spec.ts'],
  },
})
