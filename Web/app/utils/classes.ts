import type { ClassLessonItem, ClassSchedule, StudentClassActivityItem, StudentClassNoteItem } from '~/types/classes'

type BadgeColor = 'neutral' | 'primary' | 'success' | 'warning' | 'error' | 'info'

export const classStatusLabels: Record<string, string> = {
  OnPreEnrollment: 'Pré-matrícula',
  OnEnrollment: 'Matrícula',
  OnReview: 'Revisão',
  Started: 'Iniciada',
  Finalized: 'Finalizada',
}

export const classStatusColors: Record<string, BadgeColor> = {
  OnPreEnrollment: 'neutral',
  OnEnrollment: 'info',
  OnReview: 'warning',
  Started: 'primary',
  Finalized: 'success',
}

export const studentClassStatusLabels: Record<string, string> = {
  Pendente: 'Pendente',
  Matriculado: 'Matriculado',
  Aprovado: 'Aprovado',
  Dispensado: 'Dispensado',
  ReprovadoPorNota: 'Reprovado por nota',
  ReprovadoPorFalta: 'Reprovado por falta',
}

export const studentClassStatusColors: Record<string, BadgeColor> = {
  Pendente: 'neutral',
  Matriculado: 'info',
  Aprovado: 'success',
  Dispensado: 'warning',
  ReprovadoPorNota: 'error',
  ReprovadoPorFalta: 'error',
}

export const classActivityTypeLabels: Record<string, string> = {
  Exam: 'Prova',
  Project: 'Projeto',
  Work: 'Trabalho',
  Presentation: 'Apresentação',
}

export const classActivityTypeIcons: Record<string, string> = {
  Exam: 'i-lucide-file-pen',
  Project: 'i-lucide-folder-kanban',
  Work: 'i-lucide-file-text',
  Presentation: 'i-lucide-presentation',
}

export const classActivityStatusLabels: Record<string, string> = {
  Pending: 'Pendente',
  Published: 'Publicada',
  Finalized: 'Finalizada',
}

export const classActivityStatusColors: Record<string, BadgeColor> = {
  Pending: 'neutral',
  Published: 'info',
  Finalized: 'success',
}

export const classActivityWorkStatusLabels: Record<string, string> = {
  Pending: 'Pendente',
  Review: 'Correção',
  Finalized: 'Finalizada',
}

export const classActivityWorkStatusColors: Record<string, BadgeColor> = {
  Pending: 'neutral',
  Review: 'info',
  Finalized: 'success',
}

export const classActivityWorkStatusOptions = Object.entries(classActivityWorkStatusLabels)
  .map(([value, label]) => ({ value, label }))

export const classActivityWorkEntryTypeLabels: Record<string, string> = {
  Comment: 'Comentário',
  NoteChange: 'Alteração de Nota',
  StatusChange: 'Alteração de Status',
}

export function formatClassActivityNote(note: number) {
  return note.toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 2 })
}

export function isExamActivity(activity: { type: string }) {
  return activity.type === 'Exam'
}

export function classActivityWorkStatusLabel(activity: { type: string }, workStatus: string) {
  if (isExamActivity(activity) && workStatus === 'Pending') return 'Agendada'
  return classActivityWorkStatusLabels[workStatus] ?? workStatus
}

export function classActivityDueLabel(activity: { type: string }) {
  return isExamActivity(activity) ? 'Prova em' : 'Entrega até'
}

const dayLabels: Record<string, string> = {
  Sunday: 'Domingo',
  Monday: 'Segunda',
  Tuesday: 'Terça',
  Wednesday: 'Quarta',
  Thursday: 'Quinta',
  Friday: 'Sexta',
  Saturday: 'Sábado',
}

export function formatClassHour(value: string) {
  return value.replace(/^H/, '').replace('_', ':')
}

export function formatClassSchedule(s: Pick<ClassSchedule, 'day' | 'startAt' | 'endAt'>) {
  return `${dayLabels[s.day] ?? s.day} · ${formatClassHour(s.startAt)} – ${formatClassHour(s.endAt)}`
}

// A carga horária vem da API em minutos; exibimos sempre em horas, arredondando para baixo.
export function formatClassWorkload(minutes: number) {
  return `${Math.floor(minutes / 60)}h`
}

export function formatClassActivityDueDate(dueDate: string, dueHour: string) {
  const [year, month, day] = dueDate.split('-')
  return `${day}/${month}/${year} · ${formatClassHour(dueHour)}`
}

export const classLessonStatusLabels: Record<string, string> = {
  Pending: 'Pendente',
  Finalized: 'Concluída',
}

export const classLessonStatusColors: Record<string, BadgeColor> = {
  Pending: 'neutral',
  Finalized: 'success',
}

export function formatClassLessonDate(date: string) {
  const [year, month, day] = date.split('-')
  return `${day}/${month}/${year}`
}

export function formatClassLesson(lesson: Pick<ClassLessonItem, 'date' | 'startAt' | 'endAt'>) {
  return `${formatClassLessonDate(lesson.date)} · ${formatClassHour(lesson.startAt)} – ${formatClassHour(lesson.endAt)}`
}

// O backend compara a data da aula com hoje em UTC, então a UI usa a mesma
// referência para nunca liberar uma chamada que a API vai recusar.
export function isFutureClassLesson(lesson: Pick<ClassLessonItem, 'date'>) {
  return lesson.date > new Date().toISOString().slice(0, 10)
}

export function groupActivitiesByNote<T extends { note: string }>(activities: T[]) {
  return [...new Set(activities.map(a => a.note))]
    .sort()
    .map(note => ({ note, activities: activities.filter(a => a.note === note) }))
}

export function groupStudentActivitiesByNote(notes: StudentClassNoteItem[], activities: StudentClassActivityItem[]) {
  return notes.map(note => ({
    ...note,
    activities: activities
      .filter(a => a.note === note.note)
      .sort((a, b) => a.createdAt.localeCompare(b.createdAt)),
  }))
}

export interface ScheduleSpan {
  key: number
  start: number
  end: number
}

/**
 * Distribui os horários de um mesmo dia em colunas lado a lado, como uma agenda
 * faz com eventos sobrepostos. `lanes` é quantas colunas o grupo sobreposto usa.
 */
export function layoutScheduleLanes(spans: ScheduleSpan[]) {
  const layout = new Map<number, { lane: number, lanes: number }>()
  const sorted = [...spans].sort((a, b) => a.start - b.start || b.end - a.end)

  let group: { key: number, lane: number }[] = []
  let laneEnds: number[] = []
  let groupEnd = -Infinity

  const flush = () => {
    for (const g of group) layout.set(g.key, { lane: g.lane, lanes: laneEnds.length })
    group = []
    laneEnds = []
  }

  for (const span of sorted) {
    if (span.start >= groupEnd) flush()
    let lane = laneEnds.findIndex(end => end <= span.start)
    if (lane < 0) lane = laneEnds.length
    laneEnds[lane] = span.end
    group.push({ key: span.key, lane })
    groupEnd = Math.max(groupEnd, span.end)
  }
  flush()

  return layout
}

/** As chaves dos horários que se sobrepõem a algum outro do mesmo conjunto. */
export function overlappingScheduleKeys(spans: ScheduleSpan[]) {
  const keys = new Set<number>()
  for (let i = 0; i < spans.length; i++) {
    for (let j = i + 1; j < spans.length; j++) {
      const a = spans[i]!
      const b = spans[j]!
      if (a.start < b.end && b.start < a.end) {
        keys.add(a.key)
        keys.add(b.key)
      }
    }
  }
  return keys
}
