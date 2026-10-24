# assets/

遊戲用像素素材（PWA 與日後 ESP32 共用）。**由 `tools/pixel-art/game/` 的腳本產生，請勿手改 PNG。**

```bash
cd tools/pixel-art/game
python3 build_all.py          # 產生所有已完成階段的素材與 manifest.json
python3 validate_assets.py    # 驗證命名、尺寸、半透明像素、manifest 一致性
python3 preview.py 3          # 輸出 assets/preview/ 的動畫對齊預覽（整數 3 倍）
```

- 邏輯畫布 240×240；角色影格 48×48；錨點 `[24, 46]`（腳底中心）。
- 圖層順序、assetKey 與動畫描述見 `manifest.json`；動畫逐格指定 `body` / `face` / `attach` / `root`。
- 素材皆為原創暫用，授權紀錄將於整合階段補在 `LICENSES.md`。
