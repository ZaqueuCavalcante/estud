<script setup lang="ts">
import * as z from 'zod'
import type { FormSubmitEvent } from '@nuxt/ui'
import type { CourseCurriculumDisciplineSelection, CreateCourseCurriculumOut } from '~/types/course-curriculums'

interface CourseItem {
  id: number
  name: string
}

interface CourseDiscipline {
  id: number
  name: string
  code: string
}

const config = useRuntimeConfig()
const toast = useToast()
const loading = ref(false)

const courses = ref<CourseItem[]>([])
const courseDisciplines = ref<CourseDiscipline[]>([])
const loadingDisciplines = ref(false)
const selection = ref<CourseCurriculumDisciplineSelection[]>([])

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
  selection.value = []
  courseDisciplines.value = []
  if (!newId) return
  loadingDisciplines.value = true
  try {
    const result = await $fetch<{ items: CourseDiscipline[] }>(
      `${config.public.backendUrl}/courses/${newId}/disciplines`,
      { credentials: 'include' },
    )
    courseDisciplines.value = result.items
  } finally {
    loadingDisciplines.value = false
  }
})

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
        disciplines: selection.value,
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
            <span class="text-sm font-medium text-highlighted">Disciplinas</span>

            <CourseCurriculumsDisciplinesPicker
              :key="formState.courseId"
              v-model="selection"
              :catalog="courseDisciplines"
              :loading="loadingDisciplines"
            >
              <template #empty>
                <TableEmptyState
                  :loading="false"
                  icon="i-lucide-book-open"
                  message="O curso selecionado não tem disciplinas vinculadas"
                  button-label="Vincular disciplinas"
                  @create="() => { navigateTo(`/courses/${formState.courseId}`) }"
                />
              </template>
            </CourseCurriculumsDisciplinesPicker>
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
