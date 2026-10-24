import { onMounted, onUnmounted, ref } from 'vue'

// 自動輪播；使用者主動選擇後即停止，並尊重「減少動態」偏好。
export function useAutoRotate(count: number, intervalMs: number) {
  const index = ref(0)
  let timer: ReturnType<typeof setInterval> | undefined

  function stop() {
    clearInterval(timer)
    timer = undefined
  }

  function select(next: number) {
    stop()
    index.value = next
  }

  onMounted(() => {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return
    timer = setInterval(() => {
      index.value = (index.value + 1) % count
    }, intervalMs)
  })
  onUnmounted(stop)

  return { index, select }
}
