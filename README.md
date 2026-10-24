# 小伴星 PixelMate

一隻會陪你生活、陪你成長，也會幫你把心意送到遠方的像素小夥伴。目前為預告官網階段（M1）。

## 技術棧

- Vue 3 + TypeScript + Vite，`vite-ssg` 預先產生靜態 HTML（SEO / OG）
- Vue Router、Pinia；遵守前端架構規範（View / Component / Composable / Store）
- 測試：Vitest（單元）、Playwright（E2E，桌機 1440px / 手機 360px）
- 像素素材由 `tools/pixel-art/*.py` 以 Python + Pillow 原創繪製（暫用素材）

## 專案結構

```text
apps/web/          官網（/）
tools/pixel-art/   像素素材產生腳本
```

## 啟動

```bash
cd apps/web
cp .env.example .env     # 可選：填入 VITE_SITE_URL
npm install
npm run dev              # 開發
npm run build            # 型別檢查 + 靜態產出至 dist/
npm test                 # 單元測試
npx playwright test      # E2E（會自動 build + preview）
```

## 重新產生素材

```bash
python3 -m pip install pillow
cd tools/pixel-art
python3 pet.py ../../apps/web/src/assets/pixel
python3 room.py ../../apps/web/src/assets/pixel
python3 brand.py ../../apps/web/src/assets/pixel ../../apps/web/public
```

## Git Flow

`main`（主）← `develop`（開發）← `feature/frontend/*`；修復使用 `bug/*`，完成後合併回 `develop`。

## 注意

- `.env` 與 `docs/` 不納入版本控制。
- 官網互動皆為示範預覽，不代表功能已上線。
