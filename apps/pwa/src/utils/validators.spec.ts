import { describe, expect, it } from 'vitest'
import { checkPassword, safeRedirect, validateDisplayName, validateEmail, validatePassword, validatePetName } from './validators'

describe('validators', () => {
  it('validates e-mail', () => {
    expect(validateEmail('')).toBeTruthy()
    expect(validateEmail('nope')).toBeTruthy()
    expect(validateEmail('a@b')).toBeTruthy()
    expect(validateEmail(' a@example.com ')).toBe('')
  })

  it('mirrors the API password rules', () => {
    expect(checkPassword('abc12345678')).toEqual({ length: true, letter: true, digit: true })
    expect(validatePassword('short1')).toContain('10')
    expect(validatePassword('onlyletterspassword')).toContain('數字')
    expect(validatePassword('1234567890123')).toContain('英文')
    expect(validatePassword('Passw0rd-123')).toBe('')
  })

  it('limits display names', () => {
    expect(validateDisplayName('小栗')).toBe('')
    expect(validateDisplayName('')).toBeTruthy()
    expect(validateDisplayName('x'.repeat(21))).toBeTruthy()
    expect(validateDisplayName('<b>')).toBeTruthy()
  })

  it('refuses open redirects', () => {
    expect(safeRedirect('/')).toBe('/')
    expect(safeRedirect('/pets?x=1')).toBe('/pets?x=1')
    expect(safeRedirect('//evil.com')).toBe('/')
    expect(safeRedirect('https://evil.com')).toBe('/')
    expect(safeRedirect('/\\evil.com')).toBe('/')
    expect(safeRedirect(undefined)).toBe('/')
    expect(safeRedirect(['/a'])).toBe('/')
  })

  it('mirrors the API pet-name rule', () => {
    expect(validatePetName('小栗')).toBe('')
    expect(validatePetName('Mochi 2')).toBe('')
    expect(validatePetName('星・星')).toBe('')
    expect(validatePetName('一二三四五六七八九十一二')).toBe('')
    expect(validatePetName('')).toBeTruthy()
    expect(validatePetName('   ')).toBeTruthy()
    expect(validatePetName('一二三四五六七八九十一二三')).toContain('12')
    expect(validatePetName('<b>小栗</b>')).toContain('只能')
    expect(validatePetName('小栗😀')).toContain('只能')
  })
})
