import { describe, expect, it } from 'vitest'
import {
  formatOpeningDuration,
  formatOpeningHour,
  formatOpeningMinutes,
  formatRate,
  minutesToOpeningHour,
  openingHourToMinutes,
  snapToOpeningStep
} from '~/utils/campi'

describe('openingHourToMinutes', () => {
  it('converts the Hour enum to minutes since midnight', () => {
    expect(openingHourToMinutes('H00_00')).toBe(0)
    expect(openingHourToMinutes('H07_30')).toBe(450)
    expect(openingHourToMinutes('H22_45')).toBe(1365)
  })

  it('returns 0 for a value out of format', () => {
    expect(openingHourToMinutes('07:30')).toBe(0)
    expect(openingHourToMinutes('')).toBe(0)
  })
})

describe('minutesToOpeningHour', () => {
  it('converts minutes to the Hour enum', () => {
    expect(minutesToOpeningHour(0)).toBe('H00_00')
    expect(minutesToOpeningHour(450)).toBe('H07_30')
    expect(minutesToOpeningHour(1440)).toBe('H24_00')
  })

  it('is the inverse of openingHourToMinutes', () => {
    for (const hour of ['H06_00', 'H12_15', 'H18_45', 'H23_30']) {
      expect(minutesToOpeningHour(openingHourToMinutes(hour))).toBe(hour)
    }
  })
})

describe('formatOpeningHour', () => {
  it('formats the Hour enum as hh:mm', () => {
    expect(formatOpeningHour('H07_30')).toBe('07:30')
  })
})

describe('formatOpeningMinutes', () => {
  it('formats minutes as hh:mm', () => {
    expect(formatOpeningMinutes(0)).toBe('00:00')
    expect(formatOpeningMinutes(450)).toBe('07:30')
  })
})

describe('formatOpeningDuration', () => {
  it('omits the part that is zero', () => {
    expect(formatOpeningDuration(0)).toBe('0min')
    expect(formatOpeningDuration(45)).toBe('45min')
    expect(formatOpeningDuration(120)).toBe('2h')
    expect(formatOpeningDuration(330)).toBe('5h 30min')
  })
})

describe('snapToOpeningStep', () => {
  it('rounds to the nearest quarter hour', () => {
    expect(snapToOpeningStep(7)).toBe(0)
    expect(snapToOpeningStep(8)).toBe(15)
    expect(snapToOpeningStep(22)).toBe(15)
    expect(snapToOpeningStep(23)).toBe(30)
    expect(snapToOpeningStep(-8)).toBe(-15)
  })
})

describe('formatRate', () => {
  it('rounds to an integer', () => {
    expect(formatRate(67.42)).toBe('67%')
    expect(formatRate(99.6)).toBe('100%')
  })

  it('never shows 0% when there is any occupancy', () => {
    expect(formatRate(0.3)).toBe('1%')
  })

  it('shows 0% without occupancy', () => {
    expect(formatRate(0)).toBe('0%')
    expect(formatRate(-5)).toBe('0%')
  })
})
