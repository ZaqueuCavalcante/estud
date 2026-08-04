export type DayType = 'Default' | 'Vacation' | 'Recess' | 'Holiday' | 'Weekend'

export interface CalendarItem {
  id: number | null // nulo quando o dia ainda não foi customizado pela instituição
  date: string // ex: "2026-01-01T00:00:00"
  dayType: DayType
  description: string | null
}

export interface GetInstitutionCalendarOut {
  year: number
  total: number
  items: CalendarItem[]
}

// Períodos letivos e de matrícula: o backend guarda só o par início/fim, e os
// dias cobertos são derivados do range aqui no front.
export type PeriodType = 'Academic' | 'Enrollment'

export interface PeriodItem {
  id: number
  name: string
  startAt: string // ex: "2026-02-02"
  endAt: string // ex: "2026-06-30"
}

export interface GetPeriodsOut {
  total: number
  items: PeriodItem[]
}
