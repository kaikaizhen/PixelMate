export type SkillKey = 'empathy' | 'creativity' | 'exploration' | 'wisdom'

export interface Pet {
  petId: string
  name: string
  bodyVariant: string
  level: number
  xp: number
  revision: number
  skills: Record<SkillKey, number>
  remainingSkillPoints: number
  equipment: Record<string, string | null>
  world: { mood: string; scene: string; weather: string; temperatureC?: number | null; weatherSource?: string; timeZone?: string }
  updatedAt: string
  activity?: PetActivity
}

/** What the pet is doing right now, decided by the API (15-minute slots). */
export interface PetActivity {
  key: string
  startedAt: string
  endsAt: string
}

export interface PetEvent {
  eventId: string
  kind: string
  itemId: string
  name: string
  assetKey: string
  rarity: string
  occurredAt: string
}

export interface CreatePetInput {
  name: string
  bodyVariant: string
}

export interface SkillsResult {
  revision: number
  skills: Record<SkillKey, number>
  remainingSkillPoints: number
}

export const MAX_SKILL_POINTS = 5

export interface InventoryItem {
  inventoryId: string
  itemId: string
  name: string
  category: string
  rarity: 'common' | 'rare' | 'precious'
  slot: string | null
  assetKey: string
  quantity: number
  equipped: boolean
}

export interface EquipResult {
  revision: number
  equipment: Record<string, string | null>
}
