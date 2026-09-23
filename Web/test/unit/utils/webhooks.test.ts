import { describe, expect, it } from 'vitest'
import { formatWebhookDuration, formatWebhookJson } from '~/utils/webhooks'

describe('formatWebhookJson', () => {
  it('indents valid JSON', () => {
    expect(formatWebhookJson('{"id":1,"name":"Ana"}')).toBe('{\n  "id": 1,\n  "name": "Ana"\n}')
  })

  it('returns the original text when it is not JSON', () => {
    expect(formatWebhookJson('Internal Server Error')).toBe('Internal Server Error')
  })

  it('returns empty for an empty value', () => {
    expect(formatWebhookJson('')).toBe('')
  })
})

describe('formatWebhookDuration', () => {
  it('uses ms below 1 second', () => {
    expect(formatWebhookDuration(0)).toBe('0 ms')
    expect(formatWebhookDuration(999)).toBe('999 ms')
  })

  it('uses seconds with 2 decimals from 1 second on', () => {
    expect(formatWebhookDuration(1000)).toBe('1.00 s')
    expect(formatWebhookDuration(1234)).toBe('1.23 s')
  })
})
