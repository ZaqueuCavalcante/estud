export interface CampusOccupancyClassroom {
  id: number
  name: string
  availableMinutes: number
  usedMinutes: number
  usedMinutesRate: number
  usedCapacityRate: number
}

export interface CampusOccupancyCell {
  day: string // 'Monday' | 'Tuesday' | ...
  shift: string // 'Morning' | 'Afternoon' | 'Evening'
  /** Se o campus abre neste dia e turno. Célula fechada não é célula vazia. */
  open: boolean
  /** Minutos que o campus abre no turno, sem multiplicar pelas salas. */
  openMinutes: number
  availableMinutes: number
  usedMinutes: number
  usedMinutesRate: number
  /** Assento-minuto ocupado: alunos x minutos em aula. É o movimento do turno. */
  usedCapacity: number
  /** Assentos ocupados no turno, pesados pela capacidade de cada sala. */
  usedCapacityRate: number
  classrooms: CampusOccupancyClassroom[]
}

/** Ocupação de uma sala na semana inteira (aba Salas). */
export interface CampusClassroomOccupancy {
  id: number
  name: string
  capacity: number
  availableMinutes: number
  usedMinutes: number
  usedMinutesRate: number
  usedCapacity: number
  usedCapacityRate: number
  averageStudents: number
}

export interface GetCampusOccupancyOut {
  campusId: number
  campus: string
  totalClassrooms: number
  /** Ocupação de minutos do campus: sala-minuto usado sobre sala-minuto aberto. */
  overallUsedMinutesRate: number
  /** Ocupação de assentos do campus: assento-minuto usado sobre assento-minuto ofertado. */
  overallUsedCapacityRate: number
  /** Quantas células o campus abre — é o denominador de "turnos livres". */
  openCells: number
  cells: CampusOccupancyCell[]
  classrooms: CampusClassroomOccupancy[]
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
