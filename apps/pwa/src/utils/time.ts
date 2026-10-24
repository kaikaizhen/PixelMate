/** The API sends UTC times; values read back from the database have no "Z", so add it before parsing. */
export const utc = (t: string): number => Date.parse(/Z|[+-]\d\d:\d\d$/.test(t) ? t : `${t}Z`)

/** "10/25 14:30" in the player's own time zone. */
export function shortTime(t: string): string {
  const d = new Date(utc(t))
  const p = (n: number) => String(n).padStart(2, '0')
  return `${d.getMonth() + 1}/${d.getDate()} ${p(d.getHours())}:${p(d.getMinutes())}`
}
