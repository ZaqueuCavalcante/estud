export interface CourseDisciplineItem {
  id: number
  name: string
  code: string
}

export interface CourseCurriculumItem {
  id: number
  name: string
  disciplines: number
}

export interface CourseOfferingItem {
  id: number
  campus: string
  curriculum: string
  period: string
  session: string
  students: number
}

export interface GetCourseDetailsOut {
  id: number
  name: string
  type: string
  typeValue: string
  students: number
  disciplines: CourseDisciplineItem[]
  curriculums: CourseCurriculumItem[]
  offerings: CourseOfferingItem[]
}
