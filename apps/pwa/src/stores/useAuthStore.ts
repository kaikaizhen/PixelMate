import { defineStore } from 'pinia'
import { ref } from 'vue'
import { authApi } from '@/api/authApi'
import { ApiError } from '@/api/http'
import type { LoginInput, RegisterInput, User } from '@/types/auth.types'
import { useSocialStore } from './useSocialStore'
import { useRoomStore } from './useRoomStore'
import { useLocationStore } from './useLocationStore'
import { useDeviceStore } from './useDeviceStore'
import { useInventoryStore } from './useInventoryStore'
import { usePetStore } from './usePetStore'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const ready = ref(false) // false until the first /me answered, so route guards never flash the wrong page

  /** Restore the session from the cookie. Anonymous / offline / error all end as "not signed in". */
  async function init(): Promise<void> {
    if (ready.value) return
    try {
      user.value = await authApi.me()
    } catch (e) {
      user.value = null
      if (!(e instanceof ApiError)) throw e
    } finally {
      ready.value = true
    }
  }

  async function login(input: LoginInput): Promise<User> {
    usePetStore().clear() // never show the previous player's pet or bag
    useInventoryStore().clear()
    useSocialStore().clear()
    useRoomStore().clear()
    useLocationStore().clear()
    useDeviceStore().clear()
    user.value = await authApi.login(input)
    return user.value
  }

  const register = (input: RegisterInput) => authApi.register(input)

  async function logout(): Promise<void> {
    try {
      await authApi.logout()
    } finally {
      user.value = null // even if the request failed the local view must not stay "signed in"
      usePetStore().clear()
      useInventoryStore().clear()
      useSocialStore().clear()
      useRoomStore().clear()
      useLocationStore().clear()
    useDeviceStore().clear()
    }
  }

  return { user, ready, init, login, register, logout }
})
