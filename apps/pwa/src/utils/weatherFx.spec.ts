import { describe, expect, it } from 'vitest'
import { createFx, fxFor } from './weatherFx'
import { WEATHERS } from './roomWeather'

/** A canvas stand-in that records every rectangle painted (the engine only uses fillRect / clearRect / fillStyle). */
function recorder() {
  const rects: string[] = []
  const ctx = { fillStyle: '', fillRect: (x: number, y: number, w: number, h: number) => rects.push(`${ctx.fillStyle}|${x},${y},${w},${h}`), clearRect() { rects.length = 0 } }
  return { ctx: ctx as unknown as CanvasRenderingContext2D, rects }
}
function run(weather: string, w: number, h: number, seconds: number, seed = 1) {
  const fx = createFx(weather, w, h, seed)
  const { ctx, rects } = recorder()
  const frames: string[][] = []
  for (let t = 0; t < seconds * 12; t++) { fx.update(1 / 12); fx.draw(ctx); frames.push([...rects]) }
  return frames
}

describe('fxFor', () => {
  it('every weather the art has does something that moves, and a stranger does nothing', () => {
    for (const w of WEATHERS) expect(fxFor(w).length, w).toBeGreaterThan(0)
    expect(fxFor('sunny')).toEqual([])
  })

  it('rain, thunder and typhoon fall; snow and cold drift; the night twinkles; fog and cloudy drift sideways', () => {
    expect(fxFor('shower')).toContain('rain')
    expect(fxFor('thunder')).toEqual(expect.arrayContaining(['rain', 'lightning']))
    expect(fxFor('starry')).toContain('stars')
    expect(fxFor('snow')).toContain('snow')
    expect(fxFor('fog')).toContain('fog')
    expect(fxFor('sunset')).toContain('birds')
  })
})

describe('createFx', () => {
  it('is deterministic for a seed and differs between seeds', () => {
    expect(run('shower', 64, 72, 2, 5)).toEqual(run('shower', 64, 72, 2, 5))
    expect(run('shower', 64, 72, 2, 5)).not.toEqual(run('shower', 64, 72, 2, 6))
  })

  it('actually moves: consecutive frames differ for every weather over a few seconds', () => {
    for (const w of WEATHERS) {
      const frames = run(w, 64, 72, 6)
      expect(new Set(frames.map((f) => f.join(';'))).size, w).toBeGreaterThan(2)
    }
  })

  it('stays on the canvas (a little spill at the edges is the particles entering)', () => {
    for (const w of ['shower', 'typhoon', 'snow', 'petals', 'fog', 'starry', 'cloudy', 'sunrise']) {
      for (const frame of run(w, 64, 72, 8)) {
        for (const r of frame) {
          const [x, y] = r.split('|')[1].split(',').map(Number)
          expect(x, `${w} x`).toBeGreaterThanOrEqual(-40)
          expect(x, `${w} x`).toBeLessThanOrEqual(64 + 40)
          expect(y, `${w} y`).toBeGreaterThanOrEqual(-12)
          expect(y, `${w} y`).toBeLessThanOrEqual(72 + 12)
        }
      }
    }
  })

  it('a bigger canvas gets more raindrops (density follows the area) and heavier weather more than drizzle', () => {
    const count = (w: string, W: number, H: number) => run(w, W, H, 1)[0].length
    expect(count('shower', 240, 240)).toBeGreaterThan(count('shower', 64, 72) * 3)          // more, but thinner per area
    expect(count('shower', 240, 240) / (240 * 240)).toBeLessThan(count('shower', 64, 72) / (64 * 72))
    expect(count('typhoon', 64, 72)).toBeGreaterThan(count('drizzle', 64, 72))
  })

  it('thunder flashes the whole canvas now and then', () => {
    const frames = run('thunder', 64, 72, 30)
    expect(frames.some((f) => f.some((r) => r.endsWith('|0,0,64,72')))).toBe(true)
  })

  it('the night sky has stars that switch on and off', () => {
    const frames = run('starry', 64, 72, 10)
    const counts = new Set(frames.map((f) => f.length))
    expect(counts.size).toBeGreaterThan(1)
  })
})
