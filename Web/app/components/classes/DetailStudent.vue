<script setup lang="ts">
import type { GetStudentClassActivitiesOut, GetStudentClassOut } from '~/types/classes'

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

const activities = computed(() => activitiesData.value?.activities ?? [])
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
      <div v-if="status === 'pending'" class="flex justify-center py-12">
        <UIcon name="i-lucide-loader-circle" class="size-8 animate-spin text-muted" />
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

        <div v-if="data.teachers.length" class="flex flex-wrap items-center gap-x-6 gap-y-1 text-sm">
          <span
            v-for="teacher in data.teachers"
            :key="teacher"
            class="flex items-center gap-1.5 text-highlighted"
          >
            <UIcon name="i-lucide-user-pen" class="size-4" />
            {{ teacher }}
          </span>
        </div>
        <div v-else class="flex items-center gap-1.5 text-sm text-muted">
          <UIcon name="i-lucide-user-x" class="size-4" />
          Nenhum professor definido
        </div>

        <section class="flex flex-col gap-3">
          <h2 class="font-semibold text-highlighted">
            Horários
          </h2>

          <div v-if="data.schedules.length" class="flex flex-wrap gap-2">
            <div
              v-for="(s, i) in data.schedules"
              :key="i"
              class="flex flex-col gap-0.5 rounded-lg border border-default bg-elevated/40 px-3 py-2"
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
        </section>

        <section class="flex flex-col gap-3">
          <h2 class="font-semibold text-highlighted">
            Atividades ({{ activities.length }})
          </h2>

          <div v-if="activities.length" class="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
            <ActivitiesCardStudent
              v-for="activity in activities"
              :key="activity.id"
              :activity="activity"
              :to="`/classes/${props.classId}/activities/${activity.id}`"
            />
          </div>
          <div v-else class="flex flex-col items-center gap-3 py-6">
            <UIcon name="i-lucide-clipboard-list" class="size-10 text-muted" />
            <p class="text-sm text-muted">
              Nenhuma atividade cadastrada
            </p>
          </div>
        </section>
      </div>
    </template>
  </UDashboardPanel>
</template>
