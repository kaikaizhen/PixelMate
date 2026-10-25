export type DeviceState = 'waiting' | 'active' | 'revoked'

export interface Device {
  deviceId: string
  petId: string
  model: string
  name: string
  state: DeviceState
  createdAt: string
  activatedAt: string | null
  lastSeenAt: string | null
}
