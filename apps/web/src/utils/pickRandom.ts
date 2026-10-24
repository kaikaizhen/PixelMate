export function pickRandom<T>(items: readonly T[], exclude?: T): T {
  const pool = items.length > 1 && exclude !== undefined ? items.filter((item) => item !== exclude) : items
  return pool[Math.floor(Math.random() * pool.length)] as T
}
