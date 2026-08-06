<script setup lang="ts">
import type { ClassSchedule, ClassTeacherItem } from '~/types/classes'

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
  schedules: ClassSchedule[]
  teachers: ClassTeacherItem[]
}>()
const emit = defineEmits<{ saved: [] }>()

const isMobile = useIsMobile()
const config = useRuntimeConfig()
const toast = useToast()
const saving = ref(false)

// Turma sem professores não tem o que escolher; com qualquer professor definido,
// o horário sempre mostra a quem ele pertence.
const pickTeacher = computed(() => props.teachers.length >= 1)

// O professor só é obrigatório quando a turma tem mais de um — com um só, o backend
// resolve sozinho. O campo fica vazio até o vínculo vir da API.
const requireTeacher = computed(() => props.teachers.length >= 2)

// 0 = "Sem sala" — sentinela para o horário sem sala alocada.
const NO_CLASSROOM = 0

const classrooms = ref<ClassroomOption[]>([])
const loadingClassrooms = ref(false)

// Só as salas do campus da turma podem sediar seus horários.
const campusClassrooms = computed(() =>
  props.campusId == null ? [] : classrooms.value.filter(c => c.campusId === props.campusId),
)

// Turma sem campus (online) não usa sala.
const pickClassroom = computed(() => props.campusId != null && campusClassrooms.value.length > 0)

const classroomOptions = computed(() => [
  { label: 'Sem sala', value: NO_CLASSROOM },
  ...campusClassrooms.value.map(c => ({
    label: `${c.name} · ${c.capacity} lugares`,
    value: c.id,
  })),
])

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

const dayOptions = [
  { label: 'Segunda', value: 'Monday' },
  { label: 'Terça', value: 'Tuesday' },
  { label: 'Quarta', value: 'Wednesday' },
  { label: 'Quinta', value: 'Thursday' },
  { label: 'Sexta', value: 'Friday' },
  { label: 'Sábado', value: 'Saturday' },
]

const teacherOptions = computed(() =>
  props.teachers.map(t => ({ label: t.name, value: t.id })),
)

function buildHourOptions() {
  const opts = []
  for (let h = 7; h <= 23; h++) {
    for (let m = 0; m < 60; m += 15) {
      const hh = h.toString().padStart(2, '0')
      const mm = m.toString().padStart(2, '0')
      opts.push({ label: `${hh}:${mm}`, value: `H${hh}_${mm}` })
    }
  }
  return opts
}
const hourOptions = buildHourOptions()

interface Row {
  key: number
  day: string | undefined
  start: string | undefined
  end: string | undefined
  teacherId: number | undefined
  classroomId: number
}

let nextKey = 0
const rows = ref<Row[]>([])

function addRow() {
  rows.value = [...rows.value, {
    key: nextKey++,
    day: undefined,
    start: undefined,
    end: undefined,
    teacherId: undefined,
    classroomId: NO_CLASSROOM,
  }]
}

function removeRow(key: number) {
  rows.value = rows.value.filter(r => r.key !== key)
}

function hourValue(h: string) {
  return Number(h.replace(/^H/, '').replace('_', ''))
}

function rowIncomplete(r: Row) {
  return !r.day || !r.start || !r.end || (requireTeacher.value && !r.teacherId)
}

function rowBadRange(r: Row) {
  return !!r.start && !!r.end && hourValue(r.start) >= hourValue(r.end)
}

function rowClassroomTooSmall(r: Row) {
  const classroom = campusClassrooms.value.find(c => c.id === r.classroomId)
  return !!classroom && classroom.capacity < props.vacancies
}

const hasErrors = computed(() =>
  rows.value.some(r => rowIncomplete(r) || rowBadRange(r) || rowClassroomTooSmall(r)),
)

async function save() {
  if (hasErrors.value) return
  saving.value = true
  try {
    await $fetch(`${config.public.backendUrl}/classes/${props.classId}/schedules`, {
      method: 'PUT',
      body: {
        schedules: rows.value.map(r => ({
          day: r.day,
          start: r.start,
          end: r.end,
          teacherId: r.teacherId ?? null,
          classroomId: r.classroomId === NO_CLASSROOM ? null : r.classroomId,
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
  if (val) {
    rows.value = props.schedules.map(s => ({
      key: nextKey++,
      day: s.day,
      start: s.startAt,
      end: s.endAt,
      teacherId: s.teacherId ?? undefined,
      classroomId: s.classroomId ?? NO_CLASSROOM,
    }))
    fetchClassrooms()
  } else {
    rows.value = []
    classrooms.value = []
  }
})
</script>

<template>
  <UModal
    v-model:open="open"
    title="Horários da turma"
    :fullscreen="isMobile"
    description="Defina os horários semanais da turma e a sala de cada um."
  >
    <template #body>
      <div class="space-y-4">
        <div v-if="!rows.length" class="flex flex-col items-center gap-3 py-8 text-muted">
          <UIcon name="i-lucide-clock" class="size-10" />
          <p class="text-sm text-center">
            Nenhum horário definido
          </p>
        </div>

        <div v-else class="flex flex-col gap-3">
          <div v-for="row in rows" :key="row.key" class="flex flex-col gap-1">
            <div class="flex items-center gap-2">
              <div
                class="flex flex-1 flex-col gap-2"
                :class="pickTeacher || pickClassroom ? 'rounded-lg border border-default p-3' : ''"
              >
                <USelect
                  v-if="pickTeacher"
                  v-model="row.teacherId"
                  :items="teacherOptions"
                  value-key="value"
                  class="w-full"
                  placeholder="Sem professor"
                  icon="i-lucide-user"
                />
                <div class="flex gap-2">
                  <USelect
                    v-model="row.day"
                    :items="dayOptions"
                    value-key="value"
                    class="flex-1"
                    placeholder="Dia"
                  />
                  <USelect
                    v-model="row.start"
                    :items="hourOptions"
                    value-key="value"
                    class="flex-1"
                    placeholder="Início"
                  />
                  <USelect
                    v-model="row.end"
                    :items="hourOptions"
                    value-key="value"
                    class="flex-1"
                    placeholder="Fim"
                  />
                </div>
                <USelect
                  v-if="pickClassroom"
                  v-model="row.classroomId"
                  :items="classroomOptions"
                  value-key="value"
                  class="w-full"
                  placeholder="Sala"
                  icon="i-lucide-door-open"
                />
              </div>
              <UButton
                icon="i-lucide-trash-2"
                color="error"
                variant="ghost"
                @click="() => { removeRow(row.key) }"
              />
            </div>
            <p v-if="rowBadRange(row)" class="text-xs text-error">
              O horário de início deve ser menor que o de fim.
            </p>
            <p v-if="rowClassroomTooSmall(row)" class="text-xs text-error">
              A capacidade da sala é menor que o número de vagas da turma.
            </p>
          </div>
        </div>

        <UButton
          icon="i-lucide-plus"
          label="Adicionar horário"
          color="neutral"
          variant="subtle"
          size="sm"
          @click="() => { addRow() }"
        />

        <div class="flex justify-end gap-2 pt-2">
          <UButton label="Cancelar" color="neutral" variant="subtle" :disabled="saving" @click="() => { open = false }" />
          <UButton label="Salvar" :loading="saving" :disabled="saving || loadingClassrooms || hasErrors" @click="() => { save() }" />
        </div>
      </div>
    </template>
  </UModal>
</template>
