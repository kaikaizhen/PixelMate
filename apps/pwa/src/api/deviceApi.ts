import type { Device } from '@/types/device.types'
import { request } from './http'

export const deviceApi = {
  list: async () => (await request<{ devices: Device[] }>('GET', '/devices')).devices,
  /** The owner types the code the device shows on its screen. */
  pair: (code: string) => request<Device>('POST', '/devices/pair', { code }),
  revoke: (id: string) => request('DELETE', `/devices/${id}`),
}
