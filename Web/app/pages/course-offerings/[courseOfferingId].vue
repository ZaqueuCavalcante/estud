<script setup lang="ts">
import type { GetCourseOfferingDetailsOut } from '~/types/course-offerings'

const route = useRoute()
const config = useRuntimeConfig()
const courseOfferingId = Number(route.params.courseOfferingId)

const { data, status, error } = await useFetch<GetCourseOfferingDetailsOut>(
  `${config.public.backendUrl}/course-offerings/${courseOfferingId}/details`,
  { credentials: 'include', server: false },
)

const students = computed(() => data.value?.students ?? [])

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('pt-BR')
}

const breadcrumb = [
  { label: 'Ofertas', to: '/course-offerings', icon: 'i-lucide-library' },
  { label: 'Detalhes' },
]
</script>

<template>
  <UDashboardPanel id="course-offering-details">
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
          Oferta de curso não encontrada
        </p>
        <UButton icon="i-lucide-arrow-left" label="Voltar" to="/course-offerings" />
      </div>

      <div v-else class="w-full lg:max-w-2xl mx-auto min-w-0 flex flex-col gap-10 pb-2">
        <div class="flex flex-col gap-1">
          <h1 class="text-2xl font-semibold tracking-tight text-highlighted">
            {{ data.course }}
          </h1>
          <div class="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted">
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-graduation-cap" class="size-4 shrink-0" />
              {{ data.courseType }}
            </span>
            <NuxtLink
              :to="`/campi/${data.campusId}`"
              class="flex items-center gap-1.5 transition-colors hover:text-primary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            >
              <UIcon name="i-lucide-map-pin" class="size-4 shrink-0" />
              {{ data.campus }}
            </NuxtLink>
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-calendar" class="size-4 shrink-0" />
              {{ data.period }}
            </span>
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-sun" class="size-4 shrink-0" />
              {{ courseSessionLabels[data.session] ?? data.session }}
            </span>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3 lg:grid-cols-4">
          <NuxtLink
            :to="`/course-curriculums/${data.courseCurriculumId}`"
            class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4 transition-all duration-200 hover:border-primary/50 hover:shadow-md focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          >
            <span class="truncate text-base font-bold leading-none text-highlighted">{{ data.curriculum }}</span>
            <span class="mt-2 text-xs text-muted">Grade curricular</span>
          </NuxtLink>
          <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
            <span class="text-2xl font-bold tabular-nums leading-none text-highlighted">{{ data.disciplines }}</span>
            <span class="mt-2 text-xs text-muted">{{ data.disciplines === 1 ? 'Disciplina' : 'Disciplinas' }}</span>
          </div>
          <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
            <span class="text-2xl font-bold tabular-nums leading-none text-highlighted">{{ students.length }}</span>
            <span class="mt-2 text-xs text-muted">{{ students.length === 1 ? 'Aluno' : 'Alunos' }}</span>
          </div>
          <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
            <span class="text-base font-bold tabular-nums leading-none text-highlighted">
              {{ formatDate(data.periodStartAt) }}
            </span>
            <span class="mt-2 text-xs text-muted">até {{ formatDate(data.periodEndAt) }}</span>
          </div>
        </div>

        <section class="flex flex-col gap-3">
          <h2 class="flex items-center gap-1.5 font-semibold text-highlighted">
            <UIcon name="i-lucide-users" class="size-4 shrink-0 text-muted" />
            Alunos
          </h2>

          <div v-if="students.length" class="flex flex-col gap-2">
            <NuxtLink
              v-for="student in students"
              :key="student.id"
              :to="`/students/${student.id}`"
              class="flex flex-wrap items-center gap-x-4 gap-y-1 rounded-lg border border-default bg-elevated/40 px-3 py-2 transition-colors hover:border-primary/50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            >
              <span class="flex items-center gap-2 text-sm text-highlighted">
                <UAvatar :alt="student.name" size="2xs" />
                {{ student.name }}
              </span>
              <UBadge class="ml-auto" :color="studentStatusColors[student.status] ?? 'neutral'" variant="subtle">
                {{ studentStatusLabels[student.status] ?? student.status }}
              </UBadge>
            </NuxtLink>
          </div>
          <div v-else class="flex items-center gap-2 text-sm text-muted">
            <UIcon name="i-lucide-user-x" class="size-4" />
            Nenhum aluno matriculado nesta oferta
          </div>
        </section>
      </div>
    </template>
  </UDashboardPanel>
</template>
