export interface CourseOfferingStudentItem {
  id: number
  name: string
  photo: string | null
  enrollmentCode: string
  status: string
  enrolledAt: string
}

export interface GetCourseOfferingDetailsOut {
  id: number
  campusId: number
  campus: string
  courseId: number
  course: string
  courseType: string
  courseCurriculumId: number
  curriculum: string
  period: string
  periodStartAt: string
  periodEndAt: string
  session: string
  disciplines: number
  students: CourseOfferingStudentItem[]
}
