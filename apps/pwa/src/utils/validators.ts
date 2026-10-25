/** Client-side checks that mirror the API rules (apps/api Auth/AuthModels.cs). The API stays the source of truth. */

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function validateEmail(value: string): string {
  const v = value.trim()
  if (!v) return '請輸入 Email。'
  if (v.length > 254 || !EMAIL.test(v)) return 'Email 格式不正確。'
  return ''
}

export interface PasswordCheck {
  length: boolean
  letter: boolean
  digit: boolean
}

export function checkPassword(value: string): PasswordCheck {
  return { length: value.length >= 10, letter: /[A-Za-z]/.test(value), digit: /[0-9]/.test(value) }
}

export function validatePassword(value: string): string {
  if (!value) return '請輸入密碼。'
  const c = checkPassword(value)
  if (!c.length) return '密碼至少 10 個字元。'
  if (!c.letter) return '密碼需要包含英文字母。'
  if (!c.digit) return '密碼需要包含數字。'
  if (value.length > 128) return '密碼太長。'
  return ''
}

export function validateDisplayName(value: string): string {
  const v = value.trim()
  if (!v) return '請輸入暱稱。'
  if ([...v].length > 20) return '暱稱最多 20 個字。'
  if (/[<>]/.test(v) || [...v].some((ch) => ch.charCodeAt(0) < 32)) return '暱稱含有不允許的字元。'
  return ''
}

/** Only allow in-app absolute paths as a post-login redirect (blocks open redirects such as //evil.com). */
export function safeRedirect(target: unknown): string {
  return typeof target === 'string' && /^\/(?!\/)[^\\]*$/.test(target) ? target : '/'
}

/** Mirrors the API pet-name rule: 1-12 characters, letters / digits / spaces / - _ · ・ only (no markup, no emoji). */
export function validatePetName(value: string): string {
  const v = value.trim()
  if (!v) return '請幫小夥伴取個名字。'
  if ([...v].length > 12) return '名字最多 12 個字。'
  if (![...v].every((c) => /[\p{L}\p{N}]/u.test(c) || ' -_·・'.includes(c))) return '名字只能用文字、數字、空格和 - _ ・。'
  return ''
}
