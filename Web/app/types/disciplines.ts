export interface DisciplineCourseItem {
  id: number
  name: string
}

export interface DisciplineTeacherItem {
  id: number
  name: string
}

export interface DisciplineClassItem {
  id: number
  period: string
  campus: string | null
  vacancies: number
  students: number
  workload: number
  status: string
}

export interface GetDisciplineDetailsOut {
  id: number
  name: string
  code: string
  courses: DisciplineCourseItem[]
  teachers: DisciplineTeacherItem[]
  classes: DisciplineClassItem[]
}
