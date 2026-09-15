import type { ClassSchedule } from '~/types/classes'

export interface TeacherCampusItem {
  id: number
  name: string
  state: string
  city: string
}

export interface TeacherDisciplineItem {
  id: number
  name: string
  code: string
}

export interface TeacherClassItem {
  id: number
  discipline: string
  period: string
  vacancies: number
  students: number
  workload: number
  status: string
  // horários da turma cobertos por este professor
  schedules: ClassSchedule[]
}

export interface GetTeacherDetailsOut {
  id: number
  name: string
  email: string
  campi: TeacherCampusItem[]
  disciplines: TeacherDisciplineItem[]
  classes: TeacherClassItem[]
}

export interface TeacherHomeClassItem {
  id: number
  discipline: string
  period: string
  campus: string | null
  status: string
  students: number
  lessons: number
  finishedLessons: number
}

export interface GetTeacherHomeOut {
  activeClasses: number
  students: number
  classes: TeacherHomeClassItem[]
}
