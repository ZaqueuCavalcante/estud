export const campusWeekDays = [
  { key: 'Monday', label: 'Segunda', short: 'Seg' },
  { key: 'Tuesday', label: 'Terça', short: 'Ter' },
  { key: 'Wednesday', label: 'Quarta', short: 'Qua' },
  { key: 'Thursday', label: 'Quinta', short: 'Qui' },
  { key: 'Friday', label: 'Sexta', short: 'Sex' },
  { key: 'Saturday', label: 'Sábado', short: 'Sáb' },
]

/**
 * Horários oferecidos no editor de funcionamento: de 07:00 a 22:00, de 15 em 15
 * minutos. 22:00 é o limite superior — a madrugada do enum `Hour` do backend não
 * tem valores entre 00:15 e 06:45, e nenhum campus abre depois das 22h.
 */
export function buildOpeningHourOptions() {
  const options = []
  for (let h = 7; h <= 22; h++) {
    for (let m = 0; m < 60; m += 15) {
      const hh = h.toString().padStart(2, '0')
      const mm = m.toString().padStart(2, '0')
      options.push({ label: `${hh}:${mm}`, value: `H${hh}_${mm}` })
      if (h === 22) break
    }
  }
  return options
}

/** `H07_30` → `730`, para comparar horários sem parsear data. */
export function openingHourValue(hour: string) {
  return Number(hour.replace(/^H/, '').replace('_', ''))
}

/** `H07_30` → `450`, minutos desde a meia-noite (para posicionar no grid). */
export function openingHourToMinutes(hour: string) {
  const match = /^H(\d{2})_(\d{2})$/.exec(hour)
  if (!match) return 0
  return Number(match[1]) * 60 + Number(match[2])
}

/** `H07_30` → `07:30` */
export function formatOpeningHour(hour: string) {
  return hour.replace(/^H/, '').replace('_', ':')
}
