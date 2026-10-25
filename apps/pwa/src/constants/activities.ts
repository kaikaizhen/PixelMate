/** What the pet may be doing (the API decides; every device just shows it). */
export const ACTIVITIES: Record<string, { label: string; emoji: string }> = {
  idle: { label: '發呆', emoji: '💭' },
  walk: { label: '散步', emoji: '🚶' },
  read: { label: '看書', emoji: '📖' },
  hum: { label: '哼歌', emoji: '🎵' },
  letter: { label: '寫信', emoji: '✏️' },
}
