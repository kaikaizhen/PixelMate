// Moving weather drawn on a small canvas at "logical pixel" resolution (the canvas is scaled up with nearest-neighbour, so every
// particle is one chunky game pixel like the art). The window and the outdoor scenes show the same engine at different sizes.
// Everything is driven by a seeded generator, so the same weather looks the same in tests.

export type Rng = () => number
export function seeded(seed: number): Rng {
  let a = seed >>> 0
  return () => {
    a = (a + 0x6d2b79f5) >>> 0
    let t = a
    t = Math.imul(t ^ (t >>> 15), t | 1)
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61)
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}

export interface Layer {
  update(dt: number): void
  draw(ctx: CanvasRenderingContext2D): void
}

const px = (ctx: CanvasRenderingContext2D, x: number, y: number, w = 1, h = 1) => ctx.fillRect(Math.round(x), Math.round(y), w, h)

// ---- falling things --------------------------------------------------------------------------------------------
interface Drop { x: number; y: number; v: number; ph: number }

function rain(w: number, h: number, rng: Rng, o: { per: number; speed: number; slant: number; len: number; color: string }): Layer {
  const n = Math.max(3, Math.round((w * h) / o.per))
  const drops: Drop[] = Array.from({ length: n }, () => ({ x: rng() * w, y: rng() * h, v: o.speed * (0.8 + rng() * 0.4), ph: 0 }))
  return {
    update(dt) {
      for (const d of drops) {
        d.y += d.v * dt; d.x += o.slant * d.v * dt
        if (d.y > h + o.len) { d.y = -o.len * rng(); d.x = rng() * (w + h * Math.abs(o.slant)) - h * Math.max(o.slant, 0) }
        if (d.x > w + 2) d.x -= w + 4
        if (d.x < -2) d.x += w + 4
      }
    },
    draw(ctx) {
      for (let k = 0; k < o.len; k++) {
        ctx.fillStyle = o.color.replace('A', String(0.85 - k * (0.6 / o.len)))
        for (const d of drops) px(ctx, d.x - o.slant * k, d.y - k)
      }
    },
  }
}

function flakes(w: number, h: number, rng: Rng, o: { per: number; speed: number; sway: number; colors: string[]; big?: number }): Layer {
  const n = Math.max(3, Math.round((w * h) / o.per))
  const f = Array.from({ length: n }, (_, i) => ({ x: rng() * w, y: rng() * h, v: o.speed * (0.6 + rng() * 0.8), ph: rng() * 6.28, c: o.colors[i % o.colors.length], big: rng() < (o.big ?? 0.2) }))
  let t = 0
  return {
    update(dt) {
      t += dt
      for (const p of f) {
        p.y += p.v * dt
        if (p.y > h + 2) { p.y = -2; p.x = rng() * w }
      }
    },
    draw(ctx) {
      for (const p of f) {
        ctx.fillStyle = p.c
        const x = p.x + Math.sin(t * 1.3 + p.ph) * o.sway
        px(ctx, x, p.y, p.big ? 2 : 1, p.big ? 2 : 1)
      }
    },
  }
}

// ---- sky -------------------------------------------------------------------------------------------------------
function stars(w: number, h: number, rng: Rng, o: { sky: number; n: number; color: string }): Layer {
  const s = Array.from({ length: Math.max(4, Math.round(w * h * o.n)) }, () => ({ x: Math.floor(rng() * w), y: Math.floor(rng() * h * o.sky), per: 1.2 + rng() * 3, ph: rng() * 6.28, cross: rng() < 0.25 }))
  let t = 0
  let shoot: { x: number; y: number; life: number } | null = null
  let next = 4 + rng() * 6
  return {
    update(dt) {
      t += dt
      next -= dt
      if (!shoot && next <= 0) { shoot = { x: rng() * w * 0.6 + w * 0.3, y: rng() * h * 0.2, life: 0 }; next = 9 + rng() * 9 }
      if (shoot) { shoot.life += dt; if (shoot.life > 0.7) shoot = null }
    },
    draw(ctx) {
      for (const p of s) {
        const b = Math.sin(t * (6.28 / p.per) + p.ph)
        if (b < -0.2) continue                                   // off for a moment: the twinkle
        ctx.fillStyle = b > 0.7 ? '#ffffff' : o.color
        px(ctx, p.x, p.y)
        if (p.cross && b > 0.7) { px(ctx, p.x - 1, p.y); px(ctx, p.x + 1, p.y); px(ctx, p.x, p.y - 1); px(ctx, p.x, p.y + 1) }
      }
      if (shoot) {
        for (let k = 0; k < 9; k++) {
          ctx.fillStyle = `rgba(255,255,230,${1 - k / 9})`
          px(ctx, shoot.x - shoot.life * 70 + k * 1.2, shoot.y + shoot.life * 35 - k * 0.6)
        }
      }
    },
  }
}

const CLOUD = ['..XXXX....', '.XXXXXXX..', 'XXXXXXXXXX', 'XXXXXXXXXX', '.XXXXXXXX.']
function clouds(w: number, h: number, rng: Rng, o: { n: number; top: number; alpha: number; color: string; speed: number }): Layer {
  const c = Array.from({ length: o.n }, () => ({ x: rng() * (w + 24) - 12, y: Math.floor(rng() * h * o.top), s: 0.6 + rng() * 0.8, k: 1 + Math.floor(rng() * 2) }))
  return {
    update(dt) { for (const p of c) { p.x += o.speed * p.s * dt; if (p.x > w + 4) p.x = -CLOUD[0].length * p.k - 2 } },
    draw(ctx) {
      ctx.fillStyle = o.color.replace('A', String(o.alpha))
      for (const p of c) CLOUD.forEach((row, ry) => [...row].forEach((ch, rx) => { if (ch === 'X') px(ctx, p.x + rx * p.k, p.y + ry * p.k, p.k, p.k) }))
    },
  }
}

function fog(w: number, h: number, rng: Rng, o: { n: number; alpha: number }): Layer {
  const b = Array.from({ length: o.n }, (_, i) => ({ y: (i + 0.5) * (h / o.n) + (rng() - 0.5) * 6, h: 5 + Math.floor(rng() * 5), x: rng() * w, v: 1.2 + rng() * 2.2, len: w * (0.7 + rng() * 0.6) }))
  return {
    update(dt) { for (const p of b) { p.x += p.v * dt; if (p.x > w) p.x -= w + p.len } },
    draw(ctx) {
      ctx.fillStyle = `rgba(245,248,252,${o.alpha})`
      for (const p of b) for (let yy = 0; yy < p.h; yy++) for (let xx = Math.max(0, Math.ceil(-p.x)); xx < Math.min(p.len, w - p.x); xx += 2) px(ctx, p.x + xx + (yy % 2), p.y + yy)   // dithered like the art, only where it shows
    },
  }
}

function birds(w: number, h: number, rng: Rng, o: { color: string }): Layer {
  type B = { x: number; y: number; v: number; ph: number }
  let flock: B[] = []
  let next = 2 + rng() * 4
  let t = 0
  return {
    update(dt) {
      t += dt; next -= dt
      if (next <= 0 && flock.length === 0) {
        const n = 1 + Math.floor(rng() * 3)
        const y0 = h * (0.15 + rng() * 0.25)
        flock = Array.from({ length: n }, (_, i) => ({ x: -4 - i * 5, y: y0 + i * 2 + rng() * 2, v: 16 + rng() * 6, ph: rng() * 3 }))
        next = 8 + rng() * 10
      }
      for (const b of flock) b.x += b.v * dt
      flock = flock.filter((b) => b.x < w + 6)
    },
    draw(ctx) {
      ctx.fillStyle = o.color
      for (const b of flock) {
        const up = Math.floor(t * 6 + b.ph) % 2 === 0
        px(ctx, b.x, b.y + (up ? 0 : 1)); px(ctx, b.x + 1, b.y + 1); px(ctx, b.x + 2, b.y + (up ? 0 : 1))
        px(ctx, b.x - 1, b.y + (up ? 1 : 0)); px(ctx, b.x + 3, b.y + (up ? 1 : 0))
      }
    },
  }
}

function lightning(w: number, h: number, rng: Rng): Layer {
  let next = 4 + rng() * 5
  let flash = 0
  let bolt: [number, number][] = []
  return {
    update(dt) {
      next -= dt
      if (flash > 0) flash -= dt
      if (next <= 0) {
        flash = 0.16
        next = 6 + rng() * 8
        let x = Math.round(rng() * w); let y = 0
        bolt = [[x, y]]
        while (y < h * 0.7) { y += 2 + Math.floor(rng() * 3); x += Math.round((rng() - 0.5) * 4); bolt.push([x, y]) }
      }
    },
    draw(ctx) {
      if (flash <= 0) return
      ctx.fillStyle = `rgba(255,255,255,${Math.min(0.45, flash * 3)})`
      ctx.fillRect(0, 0, w, h)
      ctx.fillStyle = '#fff8c8'
      for (const [x, y] of bolt) px(ctx, x, y, 1, 2)
    },
  }
}

// ---- which weather moves how -----------------------------------------------------------------------------------
export type FxName = 'rain' | 'snow' | 'fog' | 'petals' | 'stars' | 'clouds' | 'birds' | 'lightning' | 'frost'

const LAYERS: Record<string, FxName[]> = {
  sunrise: ['clouds', 'birds'],
  sunset: ['clouds', 'birds'],
  starry: ['stars'],
  cloudy: ['clouds', 'birds'],
  fog: ['fog'],
  drizzle: ['rain'],
  shower: ['rain'],
  thunder: ['rain', 'lightning'],
  typhoon: ['rain'],
  petals: ['petals'],
  cold: ['frost'],
  snow: ['snow'],
}

export const fxFor = (weather: string): FxName[] => LAYERS[weather] ?? []

export interface Fx {
  layers: Layer[]
  update(dt: number): void
  draw(ctx: CanvasRenderingContext2D): void
}

/** Builds the moving weather for a canvas of w x h logical pixels. Density follows the area, so a small window is not crowded. */
export function createFx(weather: string, w: number, h: number, seed = 1): Fx {
  const rng = seeded(seed)
  // a whole screen is far bigger than the window: thin the falling things out so the scene stays readable
  const sparse = w * h > 12000 ? 3.2 : 1
  const make = (name: FxName): Layer => {
    switch (name) {
      case 'rain':
        if (weather === 'drizzle') return rain(w, h, rng, { per: 90 * sparse, speed: 34, slant: 0.05, len: 3, color: 'rgba(205,225,245,A)' })
        if (weather === 'typhoon') return rain(w, h, rng, { per: 18 * sparse, speed: 95, slant: 0.7, len: 5, color: 'rgba(215,230,250,A)' })
        return rain(w, h, rng, { per: (weather === 'thunder' ? 26 : 34) * sparse, speed: 70, slant: 0.18, len: 4, color: 'rgba(205,225,248,A)' })
      case 'snow': return flakes(w, h, rng, { per: 55 * sparse, speed: 11, sway: 2.2, colors: ['#ffffff', '#e9f2ff', '#d6e6fb'], big: 0.25 })
      case 'frost': return flakes(w, h, rng, { per: 260 * sparse, speed: 4, sway: 1.2, colors: ['#ffffff', '#cfe8ff'], big: 0.1 })
      case 'petals': return flakes(w, h, rng, { per: 160 * sparse, speed: 9, sway: 3.5, colors: ['#ffc2d4', '#ff9fbd', '#ffe0ea'], big: 0.5 })
      case 'stars': return stars(w, h, rng, { sky: 0.62, n: 0.012, color: '#cfd8ff' })
      case 'clouds': return clouds(w, h, rng, { n: Math.max(2, Math.round(w / 55)), top: 0.42, alpha: 0.8, color: 'rgba(255,255,255,A)', speed: 2.2 })
      case 'fog': return fog(w, h, rng, { n: Math.max(3, Math.round(h / 22)), alpha: 0.32 })
      case 'birds': return birds(w, h, rng, { color: '#4b3b52' })
      case 'lightning': return lightning(w, h, rng)
    }
  }
  const layers = fxFor(weather).map(make)
  return {
    layers,
    update: (dt) => layers.forEach((l) => l.update(dt)),
    draw(ctx) { ctx.clearRect(0, 0, w, h); layers.forEach((l) => l.draw(ctx)) },
  }
}
