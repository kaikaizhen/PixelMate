import { onMounted, onUnmounted, ref } from 'vue'
import { pickRandom } from '@/utils/pickRandom'

export type PetMood = 'idle' | 'blink' | 'happy'

const HAPPY_MS = 1200
const BLINK_MS = 160
const BLINK_MIN_MS = 2600
const BLINK_RANGE_MS = 2600

// 官網示範用短句，不是 AI 對話。
export function usePetReaction(phrases: readonly string[]) {
  const mood = ref<PetMood>('idle')
  const message = ref('')
  let blinkTimer: ReturnType<typeof setTimeout> | undefined
  let happyTimer: ReturnType<typeof setTimeout> | undefined

  function scheduleBlink() {
    blinkTimer = setTimeout(() => {
      if (mood.value === 'idle') {
        mood.value = 'blink'
        setTimeout(() => {
          if (mood.value === 'blink') mood.value = 'idle'
        }, BLINK_MS)
      }
      scheduleBlink()
    }, BLINK_MIN_MS + Math.random() * BLINK_RANGE_MS)
  }

  function touch() {
    message.value = pickRandom(phrases, message.value)
    mood.value = 'happy'
    clearTimeout(happyTimer)
    happyTimer = setTimeout(() => {
      mood.value = 'idle'
      message.value = ''
    }, HAPPY_MS + 800)
  }

  onMounted(scheduleBlink)
  onUnmounted(() => {
    clearTimeout(blinkTimer)
    clearTimeout(happyTimer)
  })

  return { mood, message, touch }
}
