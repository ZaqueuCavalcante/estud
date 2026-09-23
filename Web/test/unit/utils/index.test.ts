import { describe, expect, it } from 'vitest'
import { formatPhoneNumber, onlyDigits } from '~/utils/index'

describe('onlyDigits', () => {
  it('removes everything that is not a digit', () => {
    expect(onlyDigits('(11) 98765-4321')).toBe('11987654321')
    expect(onlyDigits('abc')).toBe('')
  })
})

describe('formatPhoneNumber', () => {
  it('applies the mask as the user types', () => {
    expect(formatPhoneNumber('')).toBe('')
    expect(formatPhoneNumber('1')).toBe('(1')
    expect(formatPhoneNumber('11')).toBe('(11')
    expect(formatPhoneNumber('119')).toBe('(11) 9')
    expect(formatPhoneNumber('119876')).toBe('(11) 9876')
    expect(formatPhoneNumber('1198765')).toBe('(11) 9876-5')
  })

  it('formats a landline number', () => {
    expect(formatPhoneNumber('1134567890')).toBe('(11) 3456-7890')
  })

  it('formats a mobile number', () => {
    expect(formatPhoneNumber('11987654321')).toBe('(11) 98765-4321')
  })

  it('ignores non-numeric characters and digits beyond the 11th', () => {
    expect(formatPhoneNumber('(11) 98765-4321')).toBe('(11) 98765-4321')
    expect(formatPhoneNumber('119876543210')).toBe('(11) 98765-4321')
  })
})
