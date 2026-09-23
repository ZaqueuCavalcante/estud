import { afterEach, describe, expect, it, vi } from 'vitest'
import type { StudentClassActivityItem } from '~/types/classes'
import {
  classActivityDueLabel,
  classActivityWorkStatusLabel,
  formatClassActivityDueDate,
  formatClassActivityNote,
  formatClassHour,
  formatClassLesson,
  formatClassSchedule,
  formatClassWorkload,
  groupActivitiesByNote,
  groupStudentActivitiesByNote,
  isFutureClassLesson
} from '~/utils/classes'

describe('formatClassHour', () => {
  it('formats the Hour enum as hh:mm', () => {
    expect(formatClassHour('H07_30')).toBe('07:30')
  })
})

describe('formatClassSchedule', () => {
  it('builds day and time range', () => {
    expect(formatClassSchedule({ day: 'Monday', startAt: 'H07_30', endAt: 'H09_10' }))
      .toBe('Segunda · 07:30 – 09:10')
  })

  it('keeps the original day when there is no translation', () => {
    expect(formatClassSchedule({ day: 'Holiday', startAt: 'H07_30', endAt: 'H09_10' }))
      .toBe('Holiday · 07:30 – 09:10')
  })
})

describe('formatClassWorkload', () => {
  it('converts minutes to hours rounding down', () => {
    expect(formatClassWorkload(0)).toBe('0h')
    expect(formatClassWorkload(119)).toBe('1h')
    expect(formatClassWorkload(3600)).toBe('60h')
  })
})

describe('formatClassActivityDueDate', () => {
  it('formats due date and hour', () => {
    expect(formatClassActivityDueDate('2026-03-15', 'H23_59')).toBe('15/03/2026 · 23:59')
  })
})

describe('formatClassLesson', () => {
  it('formats lesson date and time range', () => {
    expect(formatClassLesson({ date: '2026-03-15', startAt: 'H19_00', endAt: 'H22_30' }))
      .toBe('15/03/2026 · 19:00 – 22:30')
  })
})

describe('formatClassActivityNote', () => {
  it('uses a comma and between 1 and 2 decimals', () => {
    expect(formatClassActivityNote(7)).toBe('7,0')
    expect(formatClassActivityNote(7.5)).toBe('7,5')
    expect(formatClassActivityNote(6.666)).toBe('6,67')
  })
})

describe('classActivityWorkStatusLabel', () => {
  it('labels a pending exam as Agendada', () => {
    expect(classActivityWorkStatusLabel({ type: 'Exam' }, 'Pending')).toBe('Agendada')
  })

  it('uses the default label in other cases', () => {
    expect(classActivityWorkStatusLabel({ type: 'Work' }, 'Pending')).toBe('Pendente')
    expect(classActivityWorkStatusLabel({ type: 'Exam' }, 'Review')).toBe('Correção')
  })

  it('returns the status itself when there is no label', () => {
    expect(classActivityWorkStatusLabel({ type: 'Work' }, 'Unknown')).toBe('Unknown')
  })
})

describe('classActivityDueLabel', () => {
  it('distinguishes exams from other activities', () => {
    expect(classActivityDueLabel({ type: 'Exam' })).toBe('Prova em')
    expect(classActivityDueLabel({ type: 'Project' })).toBe('Entrega até')
  })
})

describe('isFutureClassLesson', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('compares with the current date in UTC', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-03-15T23:30:00-03:00'))

    expect(isFutureClassLesson({ date: '2026-03-15' })).toBe(false)
    expect(isFutureClassLesson({ date: '2026-03-16' })).toBe(false)
    expect(isFutureClassLesson({ date: '2026-03-17' })).toBe(true)
  })
})

describe('groupActivitiesByNote', () => {
  it('groups by note in order, preserving activity order', () => {
    const activities = [
      { id: 1, note: 'N2' },
      { id: 2, note: 'N1' },
      { id: 3, note: 'N2' }
    ]

    expect(groupActivitiesByNote(activities)).toEqual([
      { note: 'N1', activities: [{ id: 2, note: 'N1' }] },
      { note: 'N2', activities: [{ id: 1, note: 'N2' }, { id: 3, note: 'N2' }] }
    ])
  })

  it('returns an empty list without activities', () => {
    expect(groupActivitiesByNote([])).toEqual([])
  })
})

describe('groupStudentActivitiesByNote', () => {
  const activity = (id: number, note: string, createdAt: string) =>
    ({ id, note, createdAt }) as StudentClassActivityItem

  it('distributes activities into notes, sorted by creation', () => {
    const notes = [
      { note: 'N1', performance: 80 },
      { note: 'N2', performance: null }
    ]
    const activities = [
      activity(1, 'N1', '2026-03-10T10:00:00Z'),
      activity(2, 'N1', '2026-03-01T10:00:00Z')
    ]

    const result = groupStudentActivitiesByNote(notes, activities)

    expect(result).toHaveLength(2)
    expect(result[0]!.performance).toBe(80)
    expect(result[0]!.activities.map(a => a.id)).toEqual([2, 1])
    expect(result[1]!.activities).toEqual([])
  })
})
