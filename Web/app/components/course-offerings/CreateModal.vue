<script setup lang="ts">
import * as z from 'zod'
import type { FormSubmitEvent } from '@nuxt/ui'

const open = defineModel<boolean>('open', { default: false })
const emit = defineEmits<{ created: [] }>()

const isMobile = useIsMobile()
const config = useRuntimeConfig()
const toast = useToast()
const loading = ref(false)

interface CampusItem { id: number; name: string }
interface CourseItem { id: number; name: string }
interface CurriculumItem { id: number; name: string }
interface PeriodItem { id: number; name: string }

const campi = ref<CampusItem[]>([])
const courses = ref<CourseItem[]>([])
const curriculums = ref<CurriculumItem[]>([])
const periods = ref<PeriodItem[]>([])
const curriculumsLoading = ref(false)

const sessionOptions = [
  { label: 'Matutino', value: 'Morning' },
  { label: 'Vespertino', value: 'Afternoon' },
  { label: 'Noturno', value: 'Evening' },
]

const schema = z.object({
  campusId: z.number({ error: 'Campus obrigatório' }),
  courseId: z.number({ error: 'Curso obrigatório' }),
  courseCurriculumId: z.number({ error: 'Grade curricular obrigatória' }),
  academicPeriodId: z.number({ error: 'Período acadêmico obrigatório' }),
  courseSession: z.string({ error: 'Turno obrigatório' }).min(1, 'Turno obrigatório'),
})

type Schema = z.output<typeof schema>

const formState = reactive<Partial<Schema>>({
  campusId: undefined,
  courseId: undefined,
  courseCurriculumId: undefined,
  academicPeriodId: undefined,
  courseSession: undefined,
})

async function fetchAll() {
  const [campiRes, coursesRes, periodsRes] = await Promise.all([
    $fetch<{ items: CampusItem[] }>(`${config.public.backendUrl}/campi`, { credentials: 'include' }),
    $fetch<{ items: CourseItem[] }>(`${config.public.backendUrl}/courses`, { credentials: 'include', query: { pageSize: 100, hasCurriculums: true } }),
    $fetch<{ items: PeriodItem[] }>(`${config.public.backendUrl}/periods/academic`, { credentials: 'include' }),
  ])
  campi.value = campiRes.items
  courses.value = coursesRes.items
  periods.value = periodsRes.items
}

async function fetchCurriculums(courseId: number) {
  curriculumsLoading.value = true
  try {
    const res = await $fetch<{ items: CurriculumItem[] }>(`${config.public.backendUrl}/course-curriculums`, {
      credentials: 'include',
      query: { pageSize: 100, courseId },
    })
    curriculums.value = res.items
  } finally {
    curriculumsLoading.value = false
  }
}

function resetForm() {
  formState.campusId = undefined
  formState.courseId = undefined
  formState.courseCurriculumId = undefined
  formState.academicPeriodId = undefined
  formState.courseSession = undefined
  curriculums.value = []
}

watch(open, (val) => {
  if (val) fetchAll()
  else resetForm()
})

watch(() => formState.courseId, (courseId) => {
  formState.courseCurriculumId = undefined
  curriculums.value = []
  if (courseId) fetchCurriculums(courseId)
})

async function onSubmit(event: FormSubmitEvent<Schema>) {
  loading.value = true
  try {
    await $fetch(`${config.public.backendUrl}/course-offerings`, {
      method: 'POST',
      body: {
        campusId: event.data.campusId,
        courseId: event.data.courseId,
        courseCurriculumId: event.data.courseCurriculumId,
        academicPeriodId: event.data.academicPeriodId,
        courseSession: event.data.courseSession,
      },
      credentials: 'include',
    })
    toast.add({ title: 'Oferta de curso criada com sucesso', color: 'success' })
    open.value = false
    emit('created')
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao criar oferta de curso.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <UModal
    v-model:open="open"
    title="Nova oferta de curso"
    :fullscreen="isMobile"
    description="Preencha os dados para cadastrar uma nova oferta de curso."
  >
    <template #body>
      <UForm
        :schema="schema"
        :state="formState"
        class="space-y-4"
        @submit="onSubmit"
      >
        <UFormField label="Campus" name="campusId">
          <USelectMenu
            v-model="formState.campusId"
            :items="campi"
            label-key="name"
            value-key="id"
            class="w-full"
            placeholder="Selecione o campus"
          />
        </UFormField>

        <UFormField label="Curso" name="courseId">
          <USelectMenu
            v-model="formState.courseId"
            :items="courses"
            label-key="name"
            value-key="id"
            class="w-full"
            placeholder="Selecione o curso"
          >
            <template #empty="{ searchTerm }">
              <p v-if="searchTerm" class="text-muted text-sm">
                Nenhum curso encontrado
              </p>
              <div v-else class="flex flex-col items-center gap-2 py-2 text-center">
                <p class="text-muted text-sm">
                  Nenhum curso possui grade curricular
                </p>
                <UButton
                  size="xs"
                  variant="subtle"
                  icon="i-lucide-plus"
                  label="Criar grade curricular"
                  @click="() => { open = false; navigateTo('/course-curriculums/new') }"
                />
              </div>
            </template>
          </USelectMenu>
        </UFormField>

        <UFormField label="Grade Curricular" name="courseCurriculumId">
          <USelectMenu
            v-model="formState.courseCurriculumId"
            :items="curriculums"
            :disabled="!formState.courseId"
            :loading="curriculumsLoading"
            label-key="name"
            value-key="id"
            class="w-full"
            :placeholder="formState.courseId ? 'Selecione a grade curricular' : 'Selecione o curso primeiro'"
          />
        </UFormField>

        <UFormField label="Período Acadêmico" name="academicPeriodId">
          <USelectMenu
            v-model="formState.academicPeriodId"
            :items="periods"
            label-key="name"
            value-key="id"
            class="w-full"
            placeholder="Selecione o período acadêmico"
          />
        </UFormField>

        <UFormField label="Turno" name="courseSession">
          <USelectMenu
            v-model="formState.courseSession"
            :items="sessionOptions"
            label-key="label"
            value-key="value"
            class="w-full"
            placeholder="Selecione o turno"
          />
        </UFormField>

        <div class="flex justify-end gap-2 pt-2">
          <UButton
            label="Cancelar"
            color="neutral"
            variant="subtle"
            :disabled="loading"
            @click="() => { open = false }"
          />
          <UButton
            label="Criar oferta"
            type="submit"
            :loading="loading"
          />
        </div>
      </UForm>
    </template>
  </UModal>
</template>
