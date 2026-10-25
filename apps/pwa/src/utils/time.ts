/** The API sends UTC times; values read back from the database have no "Z", so add it before parsing. */
export const utc = (t: string): number => Date.parse(/Z|[+-]\d\d:\d\d$/.test(t) ? t : `${t}Z`)

/** "10/25 14:30" in the player's own time zone. */
export function shortTime(t: string): string {
  const d = new Date(utc(t))
  const p = (n: number) => String(n).padStart(2, '0')
  return `${d.getMonth() + 1}/${d.getDate()} ${p(d.getHours())}:${p(d.getMinutes())}`
}

/** "剛剛", "5 分鐘前", "3 小時前", else the short date; `now` is injectable for tests. */
export function ago(t: string | null, now = Date.now()): string {
  if (!t) return '還沒連線過'
  const s = Math.max(0, Math.round((now - utc(t)) / 1000))
  if (s < 60) return '剛剛'
  if (s < 3600) return `${Math.floor(s / 60)} 分鐘前`
  if (s < 86_400) return `${Math.floor(s / 3600)} 小時前`
  return shortTime(t)
}
