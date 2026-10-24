import { ref, watch, type Ref } from 'vue'

/** Colours of the top and bottom edge of a picture, to continue it past the 240x240 square on tall or wide screens. */
export function useEdgeColors(url: Ref<string>) {
  const top = ref('')
  const bottom = ref('')
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
        const px = (y: number) => { const [r, g, b] = ctx.getImageData(1, y, 1, 1).data; return `rgb(${r}, ${g}, ${b})` }
        top.value = px(0)
        bottom.value = px(img.height - 1)
      } catch { /* unreadable: keep the plain fill */ }
    }
    img.src = u
  }, { immediate: true })
  return { top, bottom }
}
