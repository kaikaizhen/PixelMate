// 建置後處理：有設定 VITE_SITE_URL 才產生 sitemap.xml，並在 robots.txt 加上 Sitemap。
import { appendFileSync, writeFileSync } from 'node:fs'
import { loadEnv } from 'vite'

const env = loadEnv(process.env.NODE_ENV ?? 'production', process.cwd(), 'VITE_')
const siteUrl = (process.env.VITE_SITE_URL ?? env.VITE_SITE_URL ?? '').replace(/\/$/, '')

if (!siteUrl) {
  console.log('[postbuild] VITE_SITE_URL 未設定，略過 sitemap.xml')
} else {
  const today = new Date().toISOString().slice(0, 10)
  writeFileSync(
    'dist/sitemap.xml',
    `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n  <url><loc>${siteUrl}/</loc><lastmod>${today}</lastmod></url>\n</urlset>\n`,
  )
  appendFileSync('dist/robots.txt', `\nSitemap: ${siteUrl}/sitemap.xml\n`)
  console.log(`[postbuild] 已產生 sitemap.xml（${siteUrl}）`)
}
