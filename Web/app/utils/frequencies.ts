import type { StudentDayAttendanceStatus } from '~/types/frequencies'

export const attendanceMonths = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez']
export const attendanceWeekDays = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb']
export const attendanceWeekDayNames = ['Domingo', 'Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado']

export const attendanceStatusLabels: Record<StudentDayAttendanceStatus, string> = {
  NoClass: 'Sem aula',
  Undefined: 'Aguardando registro',
  Present: 'Presença',
  Absent: 'Falta',
}

export function attendanceCellClass(status: StudentDayAttendanceStatus): string {
  switch (status) {
    case 'NoClass': return 'bg-elevated'
    case 'Undefined': return 'bg-accented'
    case 'Present': return 'bg-success'
    case 'Absent': return 'bg-error'
  }
}
