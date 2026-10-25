import { ref } from 'vue'
import { ApiError } from '@/api/http'

/** Wraps one async form action: a loading flag, the last error and its per-field messages. */
export function useSubmit<A extends unknown[], R>(action: (...args: A) => Promise<R>) {
  const loading = ref(false)
  const error = ref<ApiError | null>(null)
  const fieldErrors = ref<Record<string, string>>({})

  async function submit(...args: A): Promise<R | undefined> {
    if (loading.value) return undefined // double-tap protection
    loading.value = true
    error.value = null
    fieldErrors.value = {}
    try {
      return await action(...args)
    } catch (e) {
      if (!(e instanceof ApiError)) throw e
      error.value = e
      fieldErrors.value = Object.fromEntries(Object.entries(e.fieldErrors).map(([k, v]) => [k, v[0] ?? '']))
      return undefined
    } finally {
      loading.value = false
    }
  }

  return { loading, error, fieldErrors, submit }
}
