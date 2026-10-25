#!/usr/bin/env node
// PixelMate device simulator: behaves like an ESP32 on the wire (no cookie, no CSRF header, one Bearer credential) so the device API can be
// exercised end to end before the hardware arrives. It does NOT pretend to test the screen, touch or Wi-Fi.
//
//   node sim.mjs pair [--api http://localhost:5200] [--model Waveshare-AMOLED-2.16]
//   node sim.mjs snapshot | events [--after N] | mail | open <letterId> | watch | rotate | unpair
import { readFileSync, writeFileSync, existsSync, unlinkSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const STATE = join(dirname(fileURLToPath(import.meta.url)), '.device.json')   // where a real device keeps its credential in NVS
const args = process.argv.slice(2)
const cmd = args[0]
const opt = (name, fallback) => { const i = args.indexOf(`--${name}`); return i >= 0 ? args[i + 1] : fallback }
const load = () => { if (!existsSync(STATE)) die('Not paired yet: run `node sim.mjs pair` first.'); return JSON.parse(readFileSync(STATE, 'utf8')) }
const die = (m) => { console.error(m); process.exit(1) }
const sleep = (ms) => new Promise((r) => setTimeout(r, ms))

async function call(method, path, { api, credential, body } = {}) {
  const res = await fetch(api + '/api/v1' + path, {
    method,
    headers: { Accept: 'application/json', ...(body ? { 'Content-Type': 'application/json' } : {}), ...(credential ? { Authorization: `Bearer ${credential}` } : {}) },
    body: body ? JSON.stringify(body) : undefined,
  })
  const text = await res.text()
  const json = text ? JSON.parse(text) : null
  if (!res.ok) { const e = new Error(`${res.status} ${json?.code ?? ''} ${json?.message ?? ''}`.trim()); e.status = res.status; throw e }
  return json
}

async function pair() {
  const api = opt('api', 'http://localhost:5200')
  const started = await call('POST', '/device/pairing/start', { api, body: { model: opt('model', 'Waveshare-AMOLED-2.16') } })
  console.log(`\n  配對碼：${started.code}   （${new Date(started.expiresAt).toLocaleTimeString()} 前有效）`)
  console.log('  請在 PWA 的裝置設定輸入這組碼，或呼叫 POST /api/v1/devices/pair { "code": "..." }。\n')
  for (;;) {
    await sleep(started.pollIntervalSeconds * 1000)
    const r = await call('POST', '/device/pairing/poll', { api, body: { pairingId: started.pairingId, pollToken: started.pollToken } })
    if (r.status === 'pending') { process.stdout.write('.'); continue }
    if (r.status === 'approved') {
      writeFileSync(STATE, JSON.stringify({ api, deviceId: r.deviceId, petId: r.petId, credential: r.credential }, null, 2), { mode: 0o600 })
      return console.log(`\n配對完成！deviceId=${r.deviceId}，憑證已存到 ${STATE}`)
    }
    die(`\n配對結束：${r.status}`)
  }
}

async function main() {
  if (cmd === 'pair') return pair()
  const s = cmd ? load() : die('commands: pair | snapshot | events | mail | open <id> | watch | rotate | unpair')
  const c = { api: s.api, credential: s.credential }
  if (cmd === 'snapshot') return console.log(JSON.stringify(await call('GET', '/device/snapshot', c), null, 2))
  if (cmd === 'events') return console.log(JSON.stringify(await call('GET', `/device/events?after=${opt('after', 0)}`, c), null, 2))
  if (cmd === 'mail') return console.log(JSON.stringify(await call('GET', '/device/mail', c), null, 2))
  if (cmd === 'open') { const id = args[1] ?? die('open <letterId>'); const l = await call('GET', `/device/mail/${id}`, c); console.log(l.body); return console.log(JSON.stringify(await call('POST', `/device/mail/${id}/read`, c))) }
  if (cmd === 'rotate') { const r = await call('POST', '/device/credential/rotate', c); writeFileSync(STATE, JSON.stringify({ ...s, credential: r.credential }, null, 2), { mode: 0o600 }); return console.log('credential rotated') }
  if (cmd === 'unpair') { await call('DELETE', '/device/me', c); unlinkSync(STATE); return console.log('unpaired') }
  if (cmd === 'watch') {                                   // what the firmware does: poll the snapshot and the events, react to new eventIds
    let after = 0
    for (;;) {
      try {
        const snap = await call('GET', '/device/snapshot', c)
        const { events } = await call('GET', `/device/events?after=${after}`, c)
        for (const e of events) { after = Math.max(after, e.seq); console.log(`[event ${e.seq}] ${e.type}`, JSON.stringify(e.payload)) }
        const p = snap.pet
        console.log(`${new Date().toLocaleTimeString()}  ${p.name} Lv.${p.level} 在${p.world.scene}・${p.activity.key}・${p.world.weather}${p.world.temperatureC != null ? ` ${Math.round(p.world.temperatureC)}°C` : ''}・未讀信 ${snap.unreadMail}・rev ${p.revision}`)
      } catch (e) { console.log('同步失敗：', e.message) }
      await sleep(5000)
    }
  }
  die('unknown command')
}
main().catch((e) => die(e.message))
