export interface ClassSchedule {
  day: string // 'Monday' | 'Tuesday' | ...
  startAt: string // ex: "H07_00"
  endAt: string // ex: "H10_00"
  teacherId: number | null
  teacher: string | null
  classroomId: number | null
  classroom: string | null
}

export interface ClassStudentItem {
  id: number
  name: string
  photo: string | null
  status: string
  averageGrade: number // nota média do aluno na turma (de 0 a 10)
  averageAttendance: number // frequência média do aluno na turma (de 0% a 100%)
}

export interface ClassTeacherItem {
  id: number
  name: string
  photo: string | null
}

export interface GetClassOut {
  id: number
  disciplineId: number
  discipline: string
  teachers: ClassTeacherItem[]
  period: string
  campusId: number | null
  campus: string | null
  vacancies: number
  workload: number
  status: string
  averageAttendance: number // frequência média da turma (de 0% a 100%)
  averageGrade: number // nota média da turma (de 0 a 10)
  schedules: ClassSchedule[]
  students: ClassStudentItem[]
}

export interface ClassStatusImplication {
  icon: string
  class: string
  text: string
}

export interface ClassStatusTransition {
  // caminho do endpoint PUT (ex: 'start', 'release-for-enrollment')
  path: string
  title: string
  actionLabel: string
  actionIcon: string
  successTitle: string
  errorTitle: string
  fromStatus: string
  toStatus: string
  implications: ClassStatusImplication[]
}

export interface GetTeacherClassOut {
  id: number
  discipline: string
  period: string
  vacancies: number
  workload: number
  status: string
  schedules: ClassSchedule[]
}

export interface GetTeacherClassStudentsOut {
  students: ClassStudentItem[]
}

export interface ClassLessonItem {
  id: number
  number: number
  date: string // ex: "2026-07-20"
  startAt: string // ex: "H19_00"
  endAt: string // ex: "H22_00"
  status: string // 'Pending' | 'Finalized'
  plannedContent: string | null
  presentStudents: number[]
}

export interface GetTeacherClassLessonsOut {
  lessons: ClassLessonItem[]
}

export interface TeacherLessonStudentItem {
  id: number
  name: string
  present: boolean
}

export interface GetTeacherClassLessonOut {
  id: number
  classId: number
  discipline: string
  number: number
  date: string // ex: "2026-07-20"
  startAt: string // ex: "H19_00"
  endAt: string // ex: "H22_00"
  status: string // 'Pending' | 'Finalized'
  plannedContent: string | null
  students: TeacherLessonStudentItem[]
}

export interface CreateLessonPlanFileOut {
  uploadUrl: string
  publicUrl: string
}

export interface CreateClassActivityFileOut {
  uploadUrl: string
  publicUrl: string
}

export interface CreateClassActivityWorkFileOut {
  uploadUrl: string
  publicUrl: string
}

export interface StudentClassLessonItem {
  id: number
  number: number
  date: string // ex: "2026-07-20"
  startAt: string // ex: "H19_00"
  endAt: string // ex: "H22_00"
  status: string // 'Pending' | 'Finalized'
}

export interface GetStudentClassLessonsOut {
  lessons: StudentClassLessonItem[]
}

export interface GetStudentClassLessonOut {
  id: number
  classId: number
  discipline: string
  number: number
  date: string // ex: "2026-07-20"
  startAt: string // ex: "H19_00"
  endAt: string // ex: "H22_00"
  status: string // 'Pending' | 'Finalized'
  plannedContent: string | null
}

export interface ClassActivityItem {
  id: number
  classId: number
  note: string // 'N1' | 'N2' | 'N3'
  title: string
  description: string
  type: string // 'Exam' | 'Project' | 'Work' | 'Presentation'
  status: string // 'Pending' | 'Published' | 'Finalized'
  weight: number
  createdAt: string
  dueDate: string // ex: "2026-07-20"
  dueHour: string // ex: "H19_00"
  deliveredWorks: number
  totalWorks: number
}

export interface GetTeacherClassActivitiesOut {
  activities: ClassActivityItem[]
}

export interface ClassActivityWorkNoteChange {
  fromNote: number
  toNote: number
}

export interface ClassActivityWorkStatusChange {
  fromStatus: string // 'Pending' | 'Review' | 'Finalized'
  toStatus: string
}

export interface ClassActivityWorkEntry {
  id: number
  userId: number
  user: string | null
  userPhoto: string | null
  type: string // 'Comment' | 'NoteChange' | 'StatusChange'
  content: string | null
  metadata: ClassActivityWorkNoteChange | ClassActivityWorkStatusChange | null
  createdAt: string
}

export interface StudentClassActivityItem {
  id: number
  classId: number
  note: string // 'N1' | 'N2' | 'N3'
  title: string
  description: string
  type: string // 'Exam' | 'Project' | 'Work' | 'Presentation'
  status: string // 'Pending' | 'Published' | 'Finalized'
  weight: number
  createdAt: string
  dueDate: string // ex: "2026-07-20"
  dueHour: string // ex: "H19_00"
  workStatus: string // 'Pending' | 'Review' | 'Finalized'
  workEntries: ClassActivityWorkEntry[]
  value: number
  ponderedValue: number
}

export interface StudentClassNoteItem {
  note: string // 'N1' | 'N2' | 'N3'
  performance: number | null // aproveitamento na N (de 0% a 100%); null enquanto a N não tem atividade
}

export interface GetStudentClassActivitiesOut {
  notes: StudentClassNoteItem[]
  activities: StudentClassActivityItem[]
}

export interface TeacherActivityWorkItem {
  id: number
  studentId: number
  student: string
  studentPhoto: string | null
  status: string // 'Pending' | 'Review' | 'Finalized'
  value: number
  entries: ClassActivityWorkEntry[]
}

export interface GetTeacherClassActivityOut {
  id: number
  classId: number
  note: string // 'N1' | 'N2' | 'N3'
  title: string
  description: string
  type: string // 'Exam' | 'Project' | 'Work' | 'Presentation'
  status: string // 'Pending' | 'Published' | 'Finalized'
  weight: number
  createdAt: string
  dueDate: string // ex: "2026-07-20"
  dueHour: string // ex: "H19_00"
  deliveredWorks: number
  totalWorks: number
  works: TeacherActivityWorkItem[]
}

export interface GetStudentClassActivityOut {
  id: number
  classId: number
  note: string // 'N1' | 'N2' | 'N3'
  title: string
  description: string
  type: string // 'Exam' | 'Project' | 'Work' | 'Presentation'
  status: string // 'Pending' | 'Published' | 'Finalized'
  weight: number
  createdAt: string
  dueDate: string // ex: "2026-07-20"
  dueHour: string // ex: "H19_00"
  workStatus: string // 'Pending' | 'Review' | 'Finalized'
  workEntries: ClassActivityWorkEntry[]
  value: number
  ponderedValue: number
}

export interface GetStudentClassOut {
  id: number
  discipline: string
  teachers: string[]
  period: string
  workload: number
  status: string
  myStatus: string
  schedules: ClassSchedule[]
}
