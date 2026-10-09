<script setup lang="ts">
import type { DropdownMenuItem } from '@nuxt/ui'
import type { ClassScheduleSlot, ClassTeacherItem, GetClassSchedulesOut, WeekEditorItem, WeekEditorSlot } from '~/types/classes'

interface ClassroomOption {
  id: number
  name: string
  campusId: number
  campus: string
  capacity: number
}

const open = defineModel<boolean>('open', { default: false })
const props = defineProps<{
  classId: number
  campusId: number | null
  vacancies: number
  teachers: ClassTeacherItem[]
}>()
const emit = defineEmits<{ saved: [] }>()

const isMobile = useIsMobile()
const config = useRuntimeConfig()
const toast = useToast()
const saving = ref(false)

const classrooms = ref<ClassroomOption[]>([])
const loadingClassrooms = ref(false)
const loadingSchedules = ref(false)
const busySlots = ref<ClassScheduleSlot[]>([])

const campusClassrooms = computed(() =>
  props.campusId == null ? [] : classrooms.value.filter(c => c.campusId === props.campusId),
)

const pickClassroom = computed(() => props.campusId != null && campusClassrooms.value.length > 0)

async function fetchClassrooms() {
  if (props.campusId == null) return
  loadingClassrooms.value = true
  try {
    classrooms.value = await $fetch<ClassroomOption[]>(
      `${config.public.backendUrl}/classrooms`,
      { credentials: 'include' },
    )
  } catch {
    classrooms.value = []
    toast.add({ title: 'Erro', description: 'Erro ao carregar as salas.', color: 'error' })
  } finally {
    loadingClassrooms.value = false
  }
}

async function fetchSchedules() {
  loadingSchedules.value = true
  try {
    const { schedules } = await $fetch<GetClassSchedulesOut>(
      `${config.public.backendUrl}/classes/${props.classId}/schedules`,
      { credentials: 'include' },
    )
    rows.value = schedules.filter(s => !s.fromOtherClass).map(s => ({
      key: nextKey++,
      day: s.day,
      start: s.startAt,
      end: s.endAt,
      teacherId: s.teacherId,
      classroomId: s.classroomId,
      removed: false,
    }))
    for (const r of rows.value) originals.set(r.key, { ...r })
    busySlots.value = schedules.filter(s => s.fromOtherClass)
  } catch {
    toast.add({ title: 'Erro', description: 'Erro ao carregar os horários da turma.', color: 'error' })
    open.value = false
  } finally {
    loadingSchedules.value = false
  }
}

interface Row {
  key: number
  day: string
  start: string
  end: string
  teacherId: number | null
  classroomId: number | null
  removed: boolean
}

let nextKey = 0
const rows = ref<Row[]>([])
const originals = new Map<number, Row>()

function rowOf(key: number) {
  return rows.value.find(r => r.key === key)
}

function addRow(slot: WeekEditorSlot) {
  rows.value = [...rows.value, {
    key: nextKey++,
    ...slot,
    teacherId: props.teachers.length === 1 ? props.teachers[0]!.id : null,
    classroomId: null,
    removed: false,
  }]
}

function removeRow(key: number) {
  const row = rowOf(key)
  if (row && originals.has(key)) row.removed = true
  else rows.value = rows.value.filter(r => r.key !== key)
}

function restoreRow(key: number) {
  const row = rowOf(key)
  if (row) row.removed = false
}

const activeRows = computed(() => rows.value.filter(r => !r.removed))

function updateRowSlot(key: number, slot: WeekEditorSlot) {
  const row = rowOf(key)
  if (!row) return
  row.day = slot.day
  row.start = slot.start
  row.end = slot.end
}

function rowChanged(r: Row) {
  const original = originals.get(r.key)
  return !!original && !r.removed && (original.day !== r.day
    || openingHourToMinutes(original.start) !== openingHourToMinutes(r.start)
    || openingHourToMinutes(original.end) !== openingHourToMinutes(r.end)
    || original.teacherId !== r.teacherId
    || original.classroomId !== r.classroomId)
}

function rowClassroomTooSmall(r: Row | undefined) {
  const classroom = campusClassrooms.value.find(c => c.id === r?.classroomId)
  return !!classroom && classroom.capacity < props.vacancies
}

function teacherBusyAt(teacherId: number | null, day: string, start: string, end: string) {
  if (teacherId == null) return false
  const from = openingHourToMinutes(start)
  const to = openingHourToMinutes(end)
  return busySlots.value.some(b => b.teacherId === teacherId && b.day === day
    && openingHourToMinutes(b.startAt) < to && from < openingHourToMinutes(b.endAt))
}

function rowTeacherBusy(r: Row) {
  return teacherBusyAt(r.teacherId, r.day, r.start, r.end)
}

const overlappingKeys = computed(() => {
  const byDay = new Map<string, ScheduleSpan[]>()
  for (const r of activeRows.value) {
    const spans = byDay.get(r.day) ?? []
    spans.push({ key: r.key, start: openingHourToMinutes(r.start), end: openingHourToMinutes(r.end) })
    byDay.set(r.day, spans)
  }
  return new Set([...byDay.values()].flatMap(spans => [...overlappingScheduleKeys(spans)]))
})

function plural(count: number, singular: string, plural: string) {
  return `${count} ${count === 1 ? singular : plural}`
}

const errors = computed(() => {
  const list: string[] = []
  const overlaps = overlappingKeys.value.size
  const tooSmall = activeRows.value.filter(rowClassroomTooSmall).length
  const teacherBusy = activeRows.value.filter(rowTeacherBusy).length
  if (overlaps) list.push(`${overlaps} horários se sobrepõem.`)
  if (teacherBusy) list.push(`${plural(teacherBusy, 'horário choca', 'horários chocam')} com outra turma do professor.`)
  if (tooSmall) list.push(`${plural(tooSmall, 'sala menor', 'salas menores')} que as vagas da turma.`)
  return list
})

const changes = computed(() => {
  const added = rows.value.filter(r => !originals.has(r.key)).length
  const changed = rows.value.filter(rowChanged).length
  const removed = rows.value.filter(r => r.removed).length
  const list: { label: string, icon: string, color: string }[] = []
  if (added) list.push({ label: `${plural(added, 'horário adicionado', 'horários adicionados')}.`, icon: 'i-lucide-plus', color: 'text-success' })
  if (changed) list.push({ label: `${plural(changed, 'horário alterado', 'horários alterados')}.`, icon: 'i-lucide-pencil', color: 'text-warning' })
  if (removed) list.push({ label: `${plural(removed, 'horário removido', 'horários removidos')}.`, icon: 'i-lucide-trash-2', color: 'text-error' })
  return list
})

// Sem verde, amarelo ou vermelho: essas cores marcam horário adicionado, alterado e removido.
const TEACHER_COLORS = [
  'bg-blue-100 dark:bg-blue-950/50 border-blue-400 dark:border-blue-700 text-blue-900 dark:text-blue-100',
  'bg-fuchsia-100 dark:bg-fuchsia-950/50 border-fuchsia-400 dark:border-fuchsia-700 text-fuchsia-900 dark:text-fuchsia-100',
  'bg-cyan-100 dark:bg-cyan-950/50 border-cyan-400 dark:border-cyan-700 text-cyan-900 dark:text-cyan-100',
  'bg-violet-100 dark:bg-violet-950/50 border-violet-400 dark:border-violet-700 text-violet-900 dark:text-violet-100',
  'bg-indigo-100 dark:bg-indigo-950/50 border-indigo-400 dark:border-indigo-700 text-indigo-900 dark:text-indigo-100',
]
const NO_TEACHER_COLOR = 'bg-elevated border-accented text-muted'
const REMOVED_COLOR = 'border-dashed border-error/40 bg-error/5 text-error/60'
const BUSY_COLOR = 'border-dashed opacity-50'

function teacherColor(teacherId: number | null) {
  const idx = props.teachers.findIndex(t => t.id === teacherId)
  return idx < 0 ? NO_TEACHER_COLOR : TEACHER_COLORS[idx % TEACHER_COLORS.length]!
}

function rowColor(r: Row) {
  if (r.removed) return REMOVED_COLOR
  const color = teacherColor(r.teacherId)
  if (overlappingKeys.value.has(r.key) || rowClassroomTooSmall(r) || rowTeacherBusy(r)) return `${color} border-2 border-dashed border-error!`
  if (!originals.has(r.key)) return `${color} border-2 border-dashed border-success!`
  if (rowChanged(r)) return `${color} border-2 border-dashed border-warning!`
  return color
}

const editorItems = computed<WeekEditorItem[]>(() => [
  ...busySlots.value.map((b, i) => ({
    key: busyKey(i),
    day: b.day,
    start: b.startAt,
    end: b.endAt,
    colorClass: `${teacherColor(b.teacherId)} ${BUSY_COLOR}`,
    locked: true,
  })),
  ...rows.value.map(r => ({
    key: r.key,
    day: r.day,
    start: r.start,
    end: r.end,
    colorClass: rowColor(r),
    removed: r.removed,
  })),
])

function busyKey(index: number) {
  return -(index + 1)
}

function busySlotOf(key: number) {
  return busySlots.value[-key - 1]
}

function teacherLabel(r: Row | undefined) {
  return props.teachers.find(t => t.id === r?.teacherId)?.name ?? 'Sem professor'
}

function dayLabel(day: string) {
  return campusWeekDays.find(d => d.key === day)?.label ?? day
}

function changedFields(key: number) {
  const row = rowOf(key)
  const original = originals.get(key)
  if (!row || !original || !rowChanged(row)) return []
  const fields: { icon: string, label: string }[] = []
  if (original.day !== row.day) {
    fields.push({ icon: 'i-lucide-calendar', label: dayLabel(original.day) })
  }
  if (openingHourToMinutes(original.start) !== openingHourToMinutes(row.start)
    || openingHourToMinutes(original.end) !== openingHourToMinutes(row.end)) {
    fields.push({ icon: 'i-lucide-clock', label: `${formatOpeningHour(original.start)} – ${formatOpeningHour(original.end)}` })
  }
  if (original.teacherId !== row.teacherId) {
    fields.push({ icon: 'i-lucide-user', label: teacherLabel(original) })
  }
  if (original.classroomId !== row.classroomId) {
    fields.push({ icon: 'i-lucide-door-open', label: classroomLabel(original) })
  }
  return fields
}

function classroomLabel(r: Row | undefined) {
  return campusClassrooms.value.find(c => c.id === r?.classroomId)?.name ?? 'Sem sala'
}

function teacherItems(key: number): DropdownMenuItem[] {
  const row = rowOf(key)
  const options = [
    { label: 'Sem professor', value: null as number | null },
    ...props.teachers.map(t => ({ label: t.name, value: t.id as number | null })),
  ]
  return options.map(option => ({
    label: option.label,
    description: row && option.value !== row.teacherId && teacherBusyAt(option.value, row.day, row.start, row.end)
      ? 'Ocupado em outra turma neste horário'
      : undefined,
    type: 'checkbox' as const,
    checked: row?.teacherId === option.value,
    onSelect: () => { if (row) row.teacherId = option.value },
  }))
}

function classroomItems(key: number): DropdownMenuItem[] {
  const row = rowOf(key)
  const options = [
    { label: 'Sem sala', value: null as number | null, capacity: null as number | null },
    ...campusClassrooms.value.map(c => ({ label: c.name, value: c.id as number | null, capacity: c.capacity as number | null })),
  ]
  return options.map(option => ({
    label: option.label,
    description: option.capacity == null
      ? undefined
      : `${option.capacity} lugares${option.capacity < props.vacancies ? ' · menor que as vagas' : ''}`,
    type: 'checkbox' as const,
    checked: row?.classroomId === option.value,
    onSelect: () => { if (row) row.classroomId = option.value },
  }))
}

async function save() {
  if (errors.value.length) return
  saving.value = true
  try {
    await $fetch(`${config.public.backendUrl}/classes/${props.classId}/schedules`, {
      method: 'PUT',
      body: {
        schedules: activeRows.value.map(r => ({
          day: r.day,
          start: r.start,
          end: r.end,
          teacherId: r.teacherId,
          classroomId: r.classroomId,
        })),
      },
      credentials: 'include',
    })
    toast.add({ title: 'Horários atualizados com sucesso', color: 'success' })
    open.value = false
    emit('saved')
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao atualizar os horários.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    saving.value = false
  }
}

watch(open, (val) => {
  rows.value = []
  originals.clear()
  busySlots.value = []
  classrooms.value = []
  if (val) {
    fetchSchedules()
    fetchClassrooms()
  }
})
</script>

<template>
  <UModal
    v-model:open="open"
    title="Horários da turma"
    :fullscreen="isMobile"
    description="Defina os horários semanais da turma, o professor e a sala de cada um."
    :ui="{ content: 'sm:max-w-7xl' }"
  >
    <template #body>
      <div class="flex flex-col gap-2">
        <ClassesSchedulesWeekEditor
          :items="editorItems"
          @change="(key, slot) => { updateRowSlot(key, slot) }"
          @create="(slot) => { addRow(slot) }"
          @remove="(key) => { removeRow(key) }"
          @restore="(key) => { restoreRow(key) }"
        >
          <template #card="{ item, compact }">
            <div
              v-if="item.locked"
              class="flex items-start pr-5"
              :class="compact ? 'flex-row flex-wrap gap-x-2' : 'flex-col gap-0.5'"
            >
              <span class="text-xs font-semibold tabular-nums">
                {{ formatOpeningHour(item.start) }} – {{ formatOpeningHour(item.end) }}
              </span>
              <span class="flex max-w-full items-center gap-1 text-xs">
                <UIcon name="i-lucide-user" class="size-3.5 shrink-0" />
                <span class="truncate">{{ busySlotOf(item.key)?.teacher }}</span>
              </span>
              <span class="flex max-w-full items-center gap-1 text-xs">
                <UIcon name="i-lucide-book-open" class="size-3.5 shrink-0" />
                <span class="truncate">{{ busySlotOf(item.key)?.discipline }}</span>
              </span>
            </div>
            <div
              v-else
              class="flex items-start pr-5"
              :class="compact ? 'flex-row flex-wrap gap-x-2' : 'flex-col gap-0.5'"
            >
              <span class="text-xs font-semibold tabular-nums">
                {{ formatOpeningHour(item.start) }} – {{ formatOpeningHour(item.end) }}
              </span>
              <UDropdownMenu v-if="teachers.length" :items="teacherItems(item.key)" :disabled="item.removed">
                <button
                  type="button"
                  class="flex max-w-full items-center gap-1 rounded text-left text-xs enabled:hover:underline"
                  @pointerdown="(e: PointerEvent) => { e.stopPropagation() }"
                  @click="(e: MouseEvent) => { e.stopPropagation() }"
                >
                  <UIcon name="i-lucide-user" class="size-3.5 shrink-0" />
                  <span class="truncate">{{ teacherLabel(rowOf(item.key)) }}</span>
                  <UIcon name="i-lucide-chevron-down" class="size-3.5 shrink-0 opacity-60" />
                </button>
              </UDropdownMenu>
              <UDropdownMenu v-if="pickClassroom" :items="classroomItems(item.key)" :disabled="item.removed">
                <button
                  type="button"
                  class="flex max-w-full items-center gap-1 rounded text-left text-xs enabled:hover:underline"
                  :class="!item.removed && rowClassroomTooSmall(rowOf(item.key)) ? 'font-semibold text-error' : ''"
                  @pointerdown="(e: PointerEvent) => { e.stopPropagation() }"
                  @click="(e: MouseEvent) => { e.stopPropagation() }"
                >
                  <UIcon name="i-lucide-door-open" class="size-3.5 shrink-0" />
                  <span class="truncate">{{ classroomLabel(rowOf(item.key)) }}</span>
                  <UIcon name="i-lucide-chevron-down" class="size-3.5 shrink-0 opacity-60" />
                </button>
              </UDropdownMenu>
            </div>
            <UIcon
              v-if="item.locked"
              name="i-lucide-lock"
              class="pointer-events-none absolute bottom-0.5 right-1 size-3.5"
            />
            <UIcon
              v-else-if="item.removed"
              name="i-lucide-trash-2"
              class="pointer-events-none absolute bottom-0.5 right-1 size-3.5 text-error"
            />
            <UIcon
              v-else-if="!originals.has(item.key)"
              name="i-lucide-plus"
              class="pointer-events-none absolute bottom-0.5 right-1 size-3.5 text-success"
            />
            <UPopover v-else-if="changedFields(item.key).length" mode="hover" :content="{ side: 'top' }">
              <span
                class="absolute bottom-0.5 right-1 z-10 flex cursor-help"
                @pointerdown="(e: PointerEvent) => { e.stopPropagation() }"
                @click="(e: MouseEvent) => { e.stopPropagation() }"
              >
                <UIcon name="i-lucide-history" class="size-3.5 text-warning" />
              </span>
              <template #content>
                <div class="flex flex-col gap-1 p-2.5 text-xs">
                  <span class="font-semibold text-highlighted">Antes da alteração</span>
                  <span
                    v-for="field in changedFields(item.key)"
                    :key="field.icon"
                    class="flex items-center gap-1.5 tabular-nums"
                  >
                    <UIcon :name="field.icon" class="size-3.5 shrink-0 text-muted" />
                    {{ field.label }}
                  </span>
                </div>
              </template>
            </UPopover>
          </template>
        </ClassesSchedulesWeekEditor>
      </div>
    </template>

    <template #footer>
      <div class="flex w-full flex-wrap items-center justify-end gap-3">
        <ul v-if="errors.length || changes.length" class="mr-auto flex flex-wrap gap-x-3 gap-y-0.5 text-xs">
          <li v-for="error in errors" :key="error" class="flex items-center gap-1.5 text-error">
            <UIcon name="i-lucide-circle-alert" class="size-3.5 shrink-0" />
            {{ error }}
          </li>
          <li v-for="change in changes" :key="change.label" class="flex items-center gap-1.5" :class="change.color">
            <UIcon :name="change.icon" class="size-3.5 shrink-0" />
            {{ change.label }}
          </li>
        </ul>
        <UButton label="Cancelar" color="neutral" variant="subtle" :disabled="saving" @click="() => { open = false }" />
        <UButton label="Salvar" :loading="saving" :disabled="saving || loadingClassrooms || loadingSchedules || errors.length > 0" @click="() => { save() }" />
      </div>
    </template>
  </UModal>
</template>
