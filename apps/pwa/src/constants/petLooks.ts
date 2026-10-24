import type { SkillKey } from '@/types/pet.types'

/** Looks that have art today (assets/manifest.json: pet.star_01.body.*). The API accepts the same ids. */
export const PET_LOOKS = [
  { id: 'star_01_cream', color: 'cream', label: '奶油色' },
  { id: 'star_01_mint', color: 'mint', label: '薄荷色' },
] as const

export type PetColor = (typeof PET_LOOKS)[number]['color']

export function colorOf(bodyVariant: string): PetColor {
  return PET_LOOKS.find((l) => l.id === bodyVariant)?.color ?? 'cream'
}

export const SKILLS: { key: SkillKey; label: string; hint: string }[] = [
  { key: 'empathy', label: '共感', hint: '寫信與交流' },
  { key: 'creativity', label: '創作', hint: '音樂與表演' },
  { key: 'exploration', label: '探索', hint: '散步與收集' },
  { key: 'wisdom', label: '智慧', hint: '學習與協助' },
]

export const NAME_IDEAS = ['小栗', '星星', '麻糬', '糰子', '布丁', '小豆', '泡芙', '栗子', '米粒', '芝麻']
