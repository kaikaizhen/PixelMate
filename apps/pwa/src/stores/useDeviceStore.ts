import { defineStore } from 'pinia'
import { ref } from 'vue'
import { deviceApi } from '@/api/deviceApi'
import type { Device } from '@/types/device.types'

export const useDeviceStore = defineStore('devices', () => {
  const devices = ref<Device[]>([])
  const loaded = ref(false)

  async function load(): Promise<void> {
    devices.value = await deviceApi.list()
    loaded.value = true
  }

  async function pair(code: string, name?: string): Promise<Device> {
    const d = await deviceApi.pair(code, name)
    await load()
    return d
  }

  async function rename(id: string, name: string): Promise<void> {
    await deviceApi.rename(id, name)
    await load()
  }

  async function revoke(id: string): Promise<void> {
    await deviceApi.revoke(id)
    await load()
  }

  function clear() {
    devices.value = []
    loaded.value = false
  }

  return { devices, loaded, load, pair, rename, revoke, clear }
})
