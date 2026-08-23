import type { CourseCurriculumDisciplineItem, CourseCurriculumDisciplineSelection } from '~/types/course-curriculums'

type DisciplineField = 'period' | 'credits' | 'workload'

export function useCurriculumDisciplinesDiff(
  saved: MaybeRefOrGetter<CourseCurriculumDisciplineItem[]>,
  selection: MaybeRefOrGetter<CourseCurriculumDisciplineSelection[]>,
) {
  const savedById = computed(() => new Map(toValue(saved).map(d => [d.id, d])))
  const selectionById = computed(() => new Map(toValue(selection).map(d => [d.id, d])))

  const toAdd = computed(() => toValue(selection).filter(s => !savedById.value.has(s.id)).map(s => s.id))
  const toRemove = computed(() => toValue(saved).filter(d => !selectionById.value.has(d.id)).map(d => d.id))

  function fieldChanged(id: number, field: DisciplineField) {
    const before = savedById.value.get(id)
    const now = selectionById.value.get(id)
    if (!before || !now) return false
    return before[field] !== now[field]
  }

  const edited = computed(() => toValue(saved)
    .filter(d => fieldChanged(d.id, 'period') || fieldChanged(d.id, 'credits') || fieldChanged(d.id, 'workload'))
    .map(d => d.id))

  const dirty = computed(() => toAdd.value.length > 0 || toRemove.value.length > 0 || edited.value.length > 0)

  return { toAdd, toRemove, edited, dirty, fieldChanged }
}
