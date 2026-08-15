<script setup lang="ts">
import * as z from 'zod'
import type { FormSubmitEvent } from '@nuxt/ui'
import type { CreateCourseCurriculumOut } from '~/types/course-curriculums'

interface CourseItem {
  id: number
  name: string
}

interface DisciplineItem {
  id: number
  name: string
}

interface DisciplineRow {
  disciplineId: number | undefined
  period: number
  credits: number
  workload: number
}

const config = useRuntimeConfig()
const toast = useToast()
const loading = ref(false)

const courses = ref<CourseItem[]>([])
const availableDisciplines = ref<DisciplineItem[]>([])
const loadingDisciplines = ref(false)
const disciplineRows = ref<DisciplineRow[]>([])

const schema = z.object({
  name: z.string({ error: 'Nome obrigatório' }).min(1, 'Nome obrigatório').max(50, 'Máximo 50 caracteres'),
  courseId: z.number({ error: 'Curso obrigatório' }),
})

type Schema = z.output<typeof schema>

const formState = reactive<Partial<Schema>>({
  name: '',
  courseId: undefined,
})

watch(() => formState.courseId, async (newId) => {
  disciplineRows.value = []
  availableDisciplines.value = []
  if (!newId) return
  loadingDisciplines.value = true
  try {
    const result = await $fetch<{ items: DisciplineItem[] }>(
      `${config.public.backendUrl}/courses/${newId}/disciplines`,
      { credentials: 'include' },
    )
    availableDisciplines.value = result.items
  } finally {
    loadingDisciplines.value = false
  }
})

function disciplinesForRow(rowIndex: number): DisciplineItem[] {
  const selectedIds = new Set(
    disciplineRows.value
      .filter((_, i) => i !== rowIndex)
      .map(r => r.disciplineId)
      .filter((id): id is number => id !== undefined),
  )
  return availableDisciplines.value.filter(d => !selectedIds.has(d.id))
}

const canAddDiscipline = computed(() => {
  if (!formState.courseId || availableDisciplines.value.length === 0) return false
  const selectedCount = disciplineRows.value.filter(r => r.disciplineId !== undefined).length
  return selectedCount < availableDisciplines.value.length
})

function addRow() {
  disciplineRows.value.push({ disciplineId: undefined, period: 1, credits: 0, workload: 0 })
}

function removeRow(index: number) {
  disciplineRows.value.splice(index, 1)
}

async function fetchCourses() {
  const result = await $fetch<{ items: CourseItem[] }>(`${config.public.backendUrl}/courses`, {
    credentials: 'include',
    query: { pageSize: 100 },
  })
  courses.value = result.items
}

onMounted(() => { fetchCourses() })

async function onSubmit(event: FormSubmitEvent<Schema>) {
  loading.value = true
  try {
    const created = await $fetch<CreateCourseCurriculumOut>(`${config.public.backendUrl}/course-curriculums`, {
      method: 'POST',
      body: {
        name: event.data.name,
        courseId: event.data.courseId,
        disciplines: disciplineRows.value
          .filter(r => r.disciplineId !== undefined)
          .map(r => ({
            id: r.disciplineId,
            period: r.period,
            credits: r.credits,
            workload: r.workload,
          })),
      },
      credentials: 'include',
    })
    toast.add({ title: 'Grade curricular criada com sucesso', color: 'success' })
    await navigateTo(`/course-curriculums/${created.id}`)
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao criar grade curricular.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    loading.value = false
  }
}

const breadcrumb = [
  { label: 'Grades', to: '/course-curriculums', icon: 'i-lucide-layout-list' },
  { label: 'Nova grade' },
]
</script>

<template>
  <UDashboardPanel id="course-curriculum-new">
    <template #header>
      <UDashboardNavbar>
        <template #title>
          <UBreadcrumb :items="breadcrumb" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="w-full lg:max-w-2xl mx-auto min-w-0 flex flex-col gap-6 py-2">
        <div class="flex flex-col gap-1">
          <h1 class="text-2xl font-semibold tracking-tight text-highlighted">
            Nova grade curricular
          </h1>
          <p class="text-sm text-muted">
            Preencha os dados para cadastrar uma nova grade curricular.
          </p>
        </div>

        <UForm
          :schema="schema"
          :state="formState"
          class="flex flex-col gap-4"
          @submit="onSubmit"
        >
          <UFormField label="Nome" name="name">
            <UInput v-model="formState.name" class="w-full" placeholder="Ex: Grade ADS 2024" />
          </UFormField>

          <UFormField label="Curso" name="courseId">
            <USelectMenu
              v-model="formState.courseId"
              :items="courses"
              label-key="name"
              value-key="id"
              class="w-full"
              placeholder="Selecione o curso"
            />
          </UFormField>

          <div v-if="formState.courseId" class="flex flex-col gap-3">
            <div class="flex items-center justify-between">
              <span class="text-sm font-medium text-highlighted">Disciplinas</span>
              <UButton
                v-if="canAddDiscipline"
                icon="i-lucide-plus"
                label="Adicionar"
                color="neutral"
                variant="subtle"
                size="sm"
                @click="() => { addRow() }"
              />
            </div>

            <div v-if="loadingDisciplines" class="flex justify-center py-6">
              <AppSpinner class="size-5 text-muted" />
            </div>

            <template v-else>
              <div v-if="!disciplineRows.length" class="flex flex-col items-center gap-2 py-6 text-muted">
                <UIcon name="i-lucide-book-open" class="size-8" />
                <p class="text-sm">
                  {{ availableDisciplines.length ? 'Nenhuma disciplina adicionada' : 'O curso selecionado não tem disciplinas vinculadas' }}
                </p>
                <UButton
                  v-if="availableDisciplines.length"
                  icon="i-lucide-plus"
                  label="Adicionar disciplina"
                  color="neutral"
                  variant="subtle"
                  size="sm"
                  @click="() => { addRow() }"
                />
              </div>

              <div v-else class="flex flex-col gap-2">
                <div class="grid grid-cols-[1fr_4rem_4rem_4rem_2rem] gap-2 px-1">
                  <span class="text-xs text-muted">Disciplina</span>
                  <span class="text-xs text-muted text-center">Período</span>
                  <span class="text-xs text-muted text-center">Créditos</span>
                  <span class="text-xs text-muted text-center">C.H.</span>
                  <span />
                </div>

                <div
                  v-for="(row, index) in disciplineRows"
                  :key="index"
                  class="grid grid-cols-[1fr_4rem_4rem_4rem_2rem] items-center gap-2"
                >
                  <USelectMenu
                    v-model="row.disciplineId"
                    :items="disciplinesForRow(index)"
                    label-key="name"
                    value-key="id"
                    class="min-w-0"
                    placeholder="Selecionar..."
                  />
                  <UInputNumber
                    v-model="row.period"
                    :min="1"
                    :max="10"
                    :increment="false"
                    :decrement="false"
                    class="text-center"
                  />
                  <UInputNumber
                    v-model="row.credits"
                    :min="0"
                    :max="100"
                    :increment="false"
                    :decrement="false"
                    class="text-center"
                  />
                  <UInputNumber
                    v-model="row.workload"
                    :min="0"
                    :max="500"
                    :increment="false"
                    :decrement="false"
                    class="text-center"
                  />
                  <UTooltip text="Remover">
                    <UButton
                      icon="i-lucide-x"
                      color="neutral"
                      variant="ghost"
                      size="xs"
                      @click="() => { removeRow(index) }"
                    />
                  </UTooltip>
                </div>
              </div>
            </template>
          </div>

          <div class="flex justify-end gap-2 pt-2">
            <UButton
              label="Cancelar"
              color="neutral"
              variant="subtle"
              to="/course-curriculums"
              :disabled="loading"
            />
            <UButton
              label="Criar grade"
              type="submit"
              :loading="loading"
            />
          </div>
        </UForm>
      </div>
    </template>
  </UDashboardPanel>
</template>
