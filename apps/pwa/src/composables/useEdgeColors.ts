import { ref, watch, type Ref } from 'vue'

/**
 * Continues a 240x240 picture past its square on tall and wide screens: the colour of its top and bottom edge, and a 1x240 strip that
 * holds the most common colour of every row (sky bands, grass, deck...), which stretched sideways looks like the same scenery going on.
 */
export function useEdgeColors(url: Ref<string>) {
  const top = ref('')
  const bottom = ref('')
  const strip = ref('')
  watch(url, (u) => {
    if (!u) return
    const img = new Image()
    img.onload = () => {
      try {
        const c = document.createElement('canvas')
        c.width = img.width; c.height = img.height
        const ctx = c.getContext('2d')
        if (!ctx) return
        ctx.drawImage(img, 0, 0)
        const data = ctx.getImageData(0, 0, img.width, img.height).data
        const out = document.createElement('canvas')
        out.width = 1; out.height = img.height
        const octx = out.getContext('2d')!
        const colour = (r: number, g: number, b: number) => `rgb(${r}, ${g}, ${b})`
        for (let y = 0; y < img.height; y++) {
          const counts = new Map<number, number>()
          for (let x = 0; x < img.width; x++) {
            const i = (y * img.width + x) * 4
            const key = (data[i] << 16) | (data[i + 1] << 8) | data[i + 2]
            counts.set(key, (counts.get(key) ?? 0) + 1)
          }
          const [best] = [...counts.entries()].sort((a, b) => b[1] - a[1])[0]
          octx.fillStyle = colour(best >> 16, (best >> 8) & 255, best & 255)
          octx.fillRect(0, y, 1, 1)
          if (y === 0) top.value = octx.fillStyle
          if (y === img.height - 1) bottom.value = octx.fillStyle
        }
        strip.value = out.toDataURL()
      } catch { /* unreadable: keep the plain fill */ }
    }
    img.src = u
  }, { immediate: true })
  return { top, bottom, strip }
}
