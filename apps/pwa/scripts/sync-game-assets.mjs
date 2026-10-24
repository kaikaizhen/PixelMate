// Copies the game assets (repo-root /assets: manifest.json, sprites, backgrounds) into public/game so the PWA
// reads them by assetKey through the manifest instead of hard-coded paths. public/game is git-ignored.
import { cpSync, existsSync, mkdirSync, rmSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const here = dirname(fileURLToPath(import.meta.url))
const from = join(here, '..', '..', '..', 'assets')
const to = join(here, '..', 'public', 'game')

if (!existsSync(join(from, 'manifest.json'))) {
  console.warn('[sync-game-assets] ../../assets/manifest.json not found; skipping (the room falls back to plain art)')
  process.exit(0)
}
rmSync(to, { recursive: true, force: true })
mkdirSync(to, { recursive: true })
for (const part of ['manifest.json', 'sprites', 'backgrounds']) cpSync(join(from, part), join(to, part), { recursive: true })
console.log('[sync-game-assets] copied assets ->', to)
