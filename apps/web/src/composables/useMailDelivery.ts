import { onUnmounted, ref } from 'vue'

export type MailPhase = 'idle' | 'delivering' | 'arrived' | 'reading' | 'done'

export const DELIVER_MS = 2800
export const LEAVE_MS = 2600

// 官網示範用：只有前端狀態，不建立真實信件。
export function useMailDelivery() {
  const phase = ref<MailPhase>('idle')
  const isPostmanLeaving = ref(false)
  let deliverTimer: ReturnType<typeof setTimeout> | undefined
  let leaveTimer: ReturnType<typeof setTimeout> | undefined

  function clearTimers() {
    clearTimeout(deliverTimer)
    clearTimeout(leaveTimer)
  }

  function startDelivery() {
    if (phase.value !== 'idle' && phase.value !== 'done') return
    clearTimers()
    isPostmanLeaving.value = false
    phase.value = 'delivering'
    deliverTimer = setTimeout(() => {
      phase.value = 'arrived'
      leaveTimer = setTimeout(() => {
        isPostmanLeaving.value = true
      }, 1400)
    }, DELIVER_MS)
  }

  function openLetter() {
    if (phase.value !== 'arrived') return
    phase.value = 'reading'
  }

  function closeLetter() {
    if (phase.value !== 'reading') return
    phase.value = 'done'
  }

  function reset() {
    clearTimers()
    isPostmanLeaving.value = false
    phase.value = 'idle'
  }

  onUnmounted(clearTimers)

  return { phase, isPostmanLeaving, startDelivery, openLetter, closeLetter, reset }
}
