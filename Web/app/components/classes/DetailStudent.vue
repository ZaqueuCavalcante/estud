<script setup lang="ts">
import { useDebounceFn } from '@vueuse/core'
import type { GetStudentClassActivitiesOut, GetStudentClassLessonsOut, GetStudentClassOut } from '~/types/classes'

const props = defineProps<{ classId: string }>()

const config = useRuntimeConfig()

// "Turmas" não é link: pro aluno as turmas vivem no grupo da sidebar, não
// numa página de listagem.
const breadcrumb = [
  { label: 'Turmas', icon: 'i-lucide-presentation' },
  { label: 'Detalhes' },
]

const { data, status, error } = await useFetch<GetStudentClassOut>(
  `${config.public.backendUrl}/students/classes/${props.classId}`,
  { credentials: 'include', server: false },
)

const { data: activitiesData } = await useFetch<GetStudentClassActivitiesOut>(
  `${config.public.backendUrl}/students/classes/${props.classId}/activities`,
  { credentials: 'include', server: false },
)

const lessonsSearch = ref('')
const appliedLessonsSearch = ref('')
const applyLessonsSearch = useDebounceFn((value: string) => {
  const search = value.trim()
  appliedLessonsSearch.value = search.length >= 3 ? search : ''
}, 300)
watch(lessonsSearch, (value) => { applyLessonsSearch(value) })

const { data: lessonsData, status: lessonsStatus } = await useFetch<GetStudentClassLessonsOut>(
  `${config.public.backendUrl}/students/classes/${props.classId}/lessons`,
  {
    credentials: 'include',
    server: false,
    query: { search: computed(() => appliedLessonsSearch.value || undefined) },
  },
)

const activities = computed(() => activitiesData.value?.activities ?? [])
const notes = computed(() => groupStudentActivitiesByNote(activitiesData.value?.notes ?? [], activities.value))
const activityGroups = computed(() => groupActivitiesByNote(activities.value))
const lessons = computed(() => lessonsData.value?.lessons ?? [])

const tab = ref('performance')
const tabItems = [
  { label: 'Notas', value: 'performance', slot: 'performance' as const, icon: 'i-lucide-chart-column' },
  { label: 'Atividades', value: 'activities', slot: 'activities' as const, icon: 'i-lucide-clipboard-list' },
  { label: 'Aulas', value: 'lessons', slot: 'lessons' as const, icon: 'i-lucide-calendar-days' },
]
</script>

<template>
  <UDashboardPanel id="class-details">
    <template #header>
      <UDashboardNavbar>
        <template #title>
          <UBreadcrumb :items="breadcrumb" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div v-if="!data && status !== 'error'" class="flex flex-1 items-center justify-center">
        <AppSpinner class="size-8" />
      </div>

      <div v-else-if="error || !data" class="flex flex-col items-center gap-4 py-12">
        <UIcon name="i-lucide-triangle-alert" class="size-16 text-muted" />
        <p class="text-muted text-sm">
          Turma não encontrada
        </p>
        <UButton icon="i-lucide-arrow-left" label="Voltar" to="/agenda" />
      </div>

      <div v-else class="flex flex-col gap-10 py-2">
        <div class="grid grid-cols-1 items-start gap-x-4 gap-y-1 sm:grid-cols-[1fr_auto]">
          <h1 class="order-1 text-2xl font-semibold tracking-tight text-highlighted sm:col-start-1 sm:row-start-1">
            {{ data.discipline }}
          </h1>
          <div class="order-2 flex flex-wrap items-center gap-x-6 gap-y-1 text-sm text-muted sm:col-start-1 sm:row-start-2">
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-calendar" class="size-4" />
              {{ data.period }}
            </span>
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-clock" class="size-4" />
              {{ formatClassWorkload(data.workload) }}
            </span>
            <UBadge
              :label="studentClassStatusLabels[data.myStatus] ?? data.myStatus"
              :color="studentClassStatusColors[data.myStatus] ?? 'neutral'"
              variant="subtle"
            />
          </div>
        </div>

        <div v-if="data.schedules.length" class="flex flex-wrap gap-2">
          <div
            v-for="(s, i) in data.schedules"
            :key="i"
            class="flex w-full flex-col gap-0.5 rounded-lg border border-default bg-elevated/40 px-3 py-2 sm:w-auto"
          >
            <span
              class="flex items-center gap-1 text-sm font-medium"
              :class="s.teacher ? 'text-highlighted' : 'text-muted'"
            >
              <UIcon :name="s.teacher ? 'i-lucide-user-pen' : 'i-lucide-user-x'" class="size-3.5" />
              {{ s.teacher ?? 'Sem professor' }}
            </span>
            <span class="flex items-center gap-1 text-xs text-muted">
              <UIcon name="i-lucide-clock" class="size-3.5" />
              {{ formatClassSchedule(s) }}
            </span>
          </div>
        </div>
        <div v-else class="flex items-center gap-2 text-sm text-muted">
          <UIcon name="i-lucide-clock" class="size-4" />
          Nenhum horário cadastrado
        </div>

        <UTabs
          v-model="tab"
          :items="tabItems"
          variant="link"
          :ui="{ list: 'border-b-0 mb-4' }"
        >
          <template #performance>
            <div v-if="notes.length" class="flex flex-col gap-3">
              <p class="text-sm text-muted">
                Cada coluna é uma atividade: a largura é o peso dela na nota e a altura é a sua nota.
              </p>

              <div class="grid grid-cols-1 gap-4" :class="notes.length === 2 ? '2xl:grid-cols-2' : '2xl:grid-cols-3'">
                <ClassesStudentNoteChart
                  v-for="group in notes"
                  :key="group.note"
                  :note="group.note"
                  :performance="group.performance"
                  :activities="group.activities"
                />
              </div>
            </div>
            <div v-else class="flex flex-col items-center gap-3 py-6">
              <UIcon name="i-lucide-chart-column" class="size-10 text-muted" />
              <p class="text-sm text-muted">
                Nenhuma nota lançada
              </p>
            </div>
          </template>

          <template #activities>
            <div v-if="activityGroups.length" class="flex flex-col gap-6">
              <section
                v-for="group in activityGroups"
                :key="group.note"
                class="flex flex-col gap-3"
              >
                <div class="flex items-center gap-2">
                  <h2 class="font-medium text-highlighted">
                    {{ group.note }}
                  </h2>
                  <span class="text-xs text-muted">
                    {{ group.activities.length }} {{ group.activities.length === 1 ? 'atividade' : 'atividades' }}
                  </span>
                </div>

                <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
                  <ActivitiesCardStudent
                    v-for="activity in group.activities"
                    :key="activity.id"
                    :activity="activity"
                    :to="`/classes/${props.classId}/activities/${activity.id}`"
                  />
                </div>
              </section>
            </div>
            <div v-else class="flex flex-col items-center gap-3 py-6">
              <UIcon name="i-lucide-clipboard-list" class="size-10 text-muted" />
              <p class="text-sm text-muted">
                Nenhuma atividade cadastrada
              </p>
            </div>
          </template>

          <template #lessons>
            <div class="flex flex-col gap-3">
              <UInput
                v-model="lessonsSearch"
                class="w-full sm:max-w-sm"
                :ui="{ base: 'h-8' }"
                icon="i-lucide-search"
                placeholder="Buscar no plano de aula..."
                :maxlength="100"
                :loading="lessonsStatus === 'pending'"
              >
                <template v-if="lessonsSearch" #trailing>
                  <UButton
                    icon="i-lucide-x"
                    color="neutral"
                    variant="link"
                    size="sm"
                    aria-label="Limpar busca"
                    @click="() => { lessonsSearch = ''; appliedLessonsSearch = '' }"
                  />
                </template>
              </UInput>

              <div v-if="lessons.length" class="flex flex-col divide-y divide-default">
                <div
                  v-for="lesson in lessons"
                  :key="lesson.id"
                  class="flex items-center justify-between gap-2 py-3"
                >
                  <div class="flex flex-col gap-1">
                    <span class="text-sm text-highlighted">Aula {{ lesson.number }}</span>
                    <span class="text-xs text-muted">{{ formatClassLesson(lesson) }}</span>
                  </div>

                  <div class="flex items-center gap-2">
                    <UBadge
                      :label="classLessonStatusLabels[lesson.status] ?? lesson.status"
                      :color="classLessonStatusColors[lesson.status] ?? 'neutral'"
                      variant="subtle"
                    />
                    <UTooltip text="Ver detalhes">
                      <UButton
                        icon="i-lucide-arrow-right"
                        color="neutral"
                        variant="ghost"
                        size="sm"
                        :to="`/classes/${props.classId}/lessons/${lesson.id}`"
                        aria-label="Ver detalhes"
                      />
                    </UTooltip>
                  </div>
                </div>
              </div>
              <TableEmptyState
                v-else-if="appliedLessonsSearch"
                :loading="false"
                icon="i-lucide-calendar-days"
                message="Nenhuma aula cadastrada"
                filtered
                not-found-message="Nenhuma aula encontrada com esse conteúdo no plano"
                @clear-filters="() => { lessonsSearch = ''; appliedLessonsSearch = '' }"
              />
              <div v-else class="flex flex-col items-center gap-3 py-6">
                <UIcon name="i-lucide-calendar-days" class="size-10 text-muted" />
                <p class="text-sm text-muted">
                  Nenhuma aula cadastrada
                </p>
              </div>
            </div>
          </template>
        </UTabs>
      </div>
    </template>
  </UDashboardPanel>
</template>
