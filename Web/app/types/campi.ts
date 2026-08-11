export interface CampusOccupancyClassroom {
  id: number
  name: string
  availableMinutes: number
  usedMinutes: number
  /** Quanto do horário aberto do turno a sala passa reservada. */
  usedMinutesRate: number
  /** Quanto dos assentos-minuto da sala as turmas ocupam. */
  usedCapacityRate: number
}

export interface CampusOccupancyCell {
  day: string // 'Monday' | 'Tuesday' | ...
  shift: string // 'Morning' | 'Afternoon' | 'Evening'
  /** Se o campus abre neste dia e turno. Célula fechada não é célula vazia. */
  open: boolean
  availableMinutes: number
  usedMinutes: number
  usedMinutesRate: number
  classrooms: CampusOccupancyClassroom[]
}

export interface GetCampusOccupancyOut {
  campusId: number
  campus: string
  totalClassrooms: number
  /** Ocupação de minutos do campus: sala-minuto usado sobre sala-minuto aberto. */
  overallUsedMinutesRate: number
  /** Quantas células o campus abre — é o denominador de "turnos livres". */
  openCells: number
  cells: CampusOccupancyCell[]
}

export interface OpeningHoursWindow {
  start: string // 'H07_00'
  end: string // 'H22_00'
}

export interface OpeningHoursDay {
  day: string // 'Monday' | 'Tuesday' | ...
  /** Vazia = o campus não abre neste dia. */
  windows: OpeningHoursWindow[]
}

export interface GetCampusOpeningHoursOut {
  campusId: number
  campus: string
  days: OpeningHoursDay[]
}
