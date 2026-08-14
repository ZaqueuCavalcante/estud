export const campusWeekDays = [
  { key: 'Monday', label: 'Segunda', short: 'Seg' },
  { key: 'Tuesday', label: 'Terça', short: 'Ter' },
  { key: 'Wednesday', label: 'Quarta', short: 'Qua' },
  { key: 'Thursday', label: 'Quinta', short: 'Qui' },
  { key: 'Friday', label: 'Sexta', short: 'Sex' },
  { key: 'Saturday', label: 'Sábado', short: 'Sáb' },
]

/**
 * Os limites de cada turno vêm do backend (`ShiftExtensions`), e o editor de
 * horários depende deles: `UpdateCampusOpeningHours` recusa janela que atravesse
 * a fronteira de um turno, e aceita no máximo uma por turno.
 */
export const campusShiftBands = [
  { key: 'Morning', label: 'Manhã', start: 6 * 60, end: 12 * 60, defaultStart: 7 * 60, defaultEnd: 12 * 60 },
  { key: 'Afternoon', label: 'Tarde', start: 12 * 60, end: 18 * 60, defaultStart: 12 * 60, defaultEnd: 18 * 60 },
  { key: 'Evening', label: 'Noite', start: 18 * 60, end: 24 * 60, defaultStart: 18 * 60, defaultEnd: 22 * 60 },
]

/** O passo do enum `Hour` do backend: não existe horário fora do quarto de hora. */
export const OPENING_HOUR_STEP = 15

/** Abaixo de meia hora o card não comporta dois handles sem virar um fio. */
export const OPENING_WINDOW_MIN_MINUTES = 30

/** `H07_30` → `450`, minutos desde a meia-noite (para posicionar no grid). */
export function openingHourToMinutes(hour: string) {
  const match = /^H(\d{2})_(\d{2})$/.exec(hour)
  if (!match) return 0
  return Number(match[1]) * 60 + Number(match[2])
}

/** `450` → `H07_30` */
export function minutesToOpeningHour(minutes: number) {
  const hh = Math.floor(minutes / 60).toString().padStart(2, '0')
  const mm = (minutes % 60).toString().padStart(2, '0')
  return `H${hh}_${mm}`
}

/** `H07_30` → `07:30` */
export function formatOpeningHour(hour: string) {
  return hour.replace(/^H/, '').replace('_', ':')
}

/** `450` → `07:30` */
export function formatOpeningMinutes(minutes: number) {
  const hh = Math.floor(minutes / 60).toString().padStart(2, '0')
  const mm = (minutes % 60).toString().padStart(2, '0')
  return `${hh}:${mm}`
}

/** `330` → `5h 30min` */
export function formatOpeningDuration(minutes: number) {
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  if (h === 0) return `${m}min`
  if (m === 0) return `${h}h`
  return `${h}h ${m}min`
}

export function snapToOpeningStep(minutes: number) {
  return Math.round(minutes / OPENING_HOUR_STEP) * OPENING_HOUR_STEP
}

/** `67.42` → `67%` */
export function formatRate(rate: number): string {
  const rounded = rate > 0 ? Math.max(Math.round(rate), 1) : 0
  return `${rounded}%`
}
