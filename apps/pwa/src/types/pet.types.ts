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
  world: { mood: string; scene: string; weather: string }
  updatedAt: string
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
