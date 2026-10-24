import { onMounted, onUnmounted, ref, type Ref } from 'vue'

/** Pixels per logical pixel for a 240-wide scene drawn in `el` (updates on resize); feeds the --s CSS variable. */
export function useSceneScale(el: Ref<HTMLElement | null>, logicalWidth = 240) {
  const scale = ref(1)
  let ro: ResizeObserver | undefined
  onMounted(() => {
    const update = () => el.value && (scale.value = el.value.clientWidth / logicalWidth)
    update()
    ro = new ResizeObserver(update)
    if (el.value) ro.observe(el.value)
  })
  onUnmounted(() => ro?.disconnect())
  return scale
}
