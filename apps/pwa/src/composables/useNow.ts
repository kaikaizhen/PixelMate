import { onBeforeUnmount, ref } from 'vue'

/** The player's clock as a reactive value, so the sky follows their local time (sunrise, day, sunset, night) without a reload. */
export function useNow(everyMs = 30_000) {
  const now = ref(new Date())
  const id = window.setInterval(() => (now.value = new Date()), everyMs)
  onBeforeUnmount(() => window.clearInterval(id))
  return now
}
