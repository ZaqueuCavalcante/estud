export type DayType = 'Default' | 'Vacation' | 'Recess' | 'Holiday' | 'Weekend'

// Nível de onde o tipo efetivo do dia veio. A precedência no backend é
// Campus → Institution → Global (feriado nacional) → Weekend → Default.
export type CalendarDaySource = 'Default' | 'Weekend' | 'Global' | 'Institution' | 'Campus'

export interface CalendarItem {
  id: number | null // nulo quando o dia é herdado de um nível acima (não há override no nível consultado)
  date: string // ex: "2026-01-01T00:00:00"
  dayType: DayType
  description: string | null
  source: CalendarDaySource
}

export interface GetCalendarOut {
  year: number
  campusId: number | null // nulo quando o calendário é o da instituição
  campus: string | null
  total: number
  items: CalendarItem[]
}

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

// Item das listas laterais que destacam dias no calendário (períodos, feriados).
export interface HighlightItem {
  key: string
  label: string
  hint?: string
}
