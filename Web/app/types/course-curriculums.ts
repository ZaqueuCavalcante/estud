export interface CreateCourseCurriculumOut {
  id: number
}

export interface CourseCurriculumDisciplineItem {
  id: number
  name: string
  code: string
  period: number
  credits: number
  workload: number
}

export interface CourseCurriculumOfferingItem {
  id: number
  campus: string
  period: string
  session: string
  students: number
}

export interface GetCourseCurriculumDetailsOut {
  id: number
  name: string
  course: string
  periods: number
  students: number
  courseId: number
  courseType: string
  totalCredits: number
  totalWorkload: number
  offerings: CourseCurriculumOfferingItem[]
  disciplines: CourseCurriculumDisciplineItem[]
}
