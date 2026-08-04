export interface CampusOccupancyClassroom {
  id: number
  name: string
  usedMinutes: number
  rate: number
}

export interface CampusOccupancyCell {
  day: string // 'Monday' | 'Tuesday' | ...
  shift: string // 'Morning' | 'Afternoon' | 'Evening'
  /** Se o campus abre neste dia e turno. Célula fechada não é célula vazia. */
  open: boolean
  availableMinutes: number
  usedMinutes: number
  rate: number
  classrooms: CampusOccupancyClassroom[]
}

export interface GetCampusOccupancyOut {
  campusId: number
  campus: string
  totalClassrooms: number
  overallRate: number
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
