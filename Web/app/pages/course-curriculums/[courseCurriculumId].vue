<script setup lang="ts">
import type { GetCourseCurriculumDetailsOut } from '~/types/course-curriculums'

const route = useRoute()
const config = useRuntimeConfig()
const courseCurriculumId = Number(route.params.courseCurriculumId)

const sessionLabels: Record<string, string> = {
  Morning: 'Matutino',
  Afternoon: 'Vespertino',
  Evening: 'Noturno',
}

const { data, status, error, refresh } = await useFetch<GetCourseCurriculumDetailsOut>(
  `${config.public.backendUrl}/course-curriculums/${courseCurriculumId}/details`,
  { credentials: 'include', server: false },
)

const editModalOpen = ref(false)

// O modal de edição espera o mesmo formato usado na listagem de grades
const curriculumRef = computed(() => data.value
  ? { id: data.value.id, name: data.value.name }
  : null,
)

const disciplines = computed(() => data.value?.disciplines ?? [])
const offerings = computed(() => data.value?.offerings ?? [])

const periods = computed(() => {
  const groups = new Map<number, typeof disciplines.value>()
  for (const discipline of disciplines.value) {
    const group = groups.get(discipline.period)
    if (group) group.push(discipline)
    else groups.set(discipline.period, [discipline])
  }
  return [...groups.entries()]
    .sort(([a], [b]) => a - b)
    .map(([period, items]) => ({
      period,
      items,
      credits: items.reduce((total, d) => total + d.credits, 0),
      workload: items.reduce((total, d) => total + d.workload, 0),
    }))
})

const breadcrumb = [
  { label: 'Grades', to: '/course-curriculums', icon: 'i-lucide-layout-list' },
  { label: 'Detalhes' },
]
</script>

<template>
  <UDashboardPanel id="course-curriculum-details">
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
          Grade curricular não encontrada
        </p>
        <UButton icon="i-lucide-arrow-left" label="Voltar" to="/course-curriculums" />
      </div>

      <div v-else class="flex flex-col gap-10 py-2">
        <div class="flex flex-col gap-1">
          <div class="flex items-center gap-1.5">
            <h1 class="text-2xl font-semibold tracking-tight text-highlighted">
              {{ data.name }}
            </h1>
            <UTooltip text="Editar">
              <UButton
                icon="i-lucide-pencil"
                color="neutral"
                variant="ghost"
                size="xs"
                @click="(e) => { (e.currentTarget as HTMLElement).blur(); editModalOpen = true }"
              />
            </UTooltip>
          </div>
          <div class="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted">
            <NuxtLink
              :to="`/courses/${data.courseId}`"
              class="flex items-center gap-1.5 transition-colors hover:text-primary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            >
              <UIcon name="i-lucide-notebook" class="size-4 shrink-0" />
              {{ data.course }}
            </NuxtLink>
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-graduation-cap" class="size-4 shrink-0" />
              {{ data.courseType }}
            </span>
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-users" class="size-4 shrink-0" />
              {{ data.students }} {{ data.students === 1 ? 'aluno' : 'alunos' }}
            </span>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3 lg:grid-cols-4">
          <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
            <span class="text-2xl font-bold tabular-nums leading-none text-highlighted">{{ disciplines.length }}</span>
            <span class="mt-2 text-xs text-muted">{{ disciplines.length === 1 ? 'Disciplina' : 'Disciplinas' }}</span>
          </div>
          <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
            <span class="text-2xl font-bold tabular-nums leading-none text-highlighted">{{ data.periods }}</span>
            <span class="mt-2 text-xs text-muted">{{ data.periods === 1 ? 'Período' : 'Períodos' }}</span>
          </div>
          <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
            <span class="text-2xl font-bold tabular-nums leading-none text-highlighted">{{ data.totalCredits }}</span>
            <span class="mt-2 text-xs text-muted">{{ data.totalCredits === 1 ? 'Crédito' : 'Créditos' }}</span>
          </div>
          <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
            <span class="text-2xl font-bold tabular-nums leading-none text-highlighted">{{ data.totalWorkload }}h</span>
            <span class="mt-2 text-xs text-muted">Carga horária</span>
          </div>
        </div>

        <section class="flex flex-col gap-3">
          <div class="flex items-center gap-1.5">
            <h2 class="font-semibold text-highlighted">
              Disciplinas
            </h2>
            <UTooltip text="Editar">
              <UButton
                icon="i-lucide-pencil"
                color="neutral"
                variant="ghost"
                size="xs"
                @click="(e) => { (e.currentTarget as HTMLElement).blur(); editModalOpen = true }"
              />
            </UTooltip>
          </div>

          <div v-if="periods.length" class="flex flex-col gap-5">
            <div v-for="group in periods" :key="group.period" class="flex flex-col gap-2">
              <div class="flex flex-wrap items-center gap-x-3 gap-y-1">
                <span class="text-sm font-medium text-highlighted">{{ group.period }}º período</span>
                <span class="text-xs text-muted tabular-nums">
                  {{ group.credits }} {{ group.credits === 1 ? 'crédito' : 'créditos' }} · {{ group.workload }}h
                </span>
              </div>

              <div class="flex flex-col gap-2">
                <NuxtLink
                  v-for="discipline in group.items"
                  :key="discipline.id"
                  :to="`/disciplines/${discipline.id}`"
                  class="flex flex-wrap items-center gap-x-4 gap-y-1 rounded-lg border border-default bg-elevated/40 px-3 py-2 transition-colors hover:border-primary/50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                >
                  <span class="flex items-center gap-1.5 text-sm text-highlighted">
                    <UIcon name="i-lucide-book-open" class="size-4 shrink-0 text-muted" />
                    {{ discipline.name }}
                  </span>
                  <span class="flex items-center gap-1.5 text-sm text-muted">
                    <UIcon name="i-lucide-hash" class="size-4 shrink-0" />
                    {{ discipline.code }}
                  </span>
                  <span class="ml-auto flex items-center gap-4 text-sm text-muted tabular-nums">
                    <span>{{ discipline.credits }} {{ discipline.credits === 1 ? 'crédito' : 'créditos' }}</span>
                    <span>{{ discipline.workload }}h</span>
                  </span>
                </NuxtLink>
              </div>
            </div>
          </div>
          <div v-else class="flex items-center gap-2 text-sm text-muted">
            <UIcon name="i-lucide-book-dashed" class="size-4" />
            Nenhuma disciplina nesta grade
          </div>
        </section>

        <section class="flex flex-col gap-3">
          <h2 class="font-semibold text-highlighted">
            Ofertas
          </h2>

          <div v-if="offerings.length" class="flex flex-col gap-2">
            <div
              v-for="offering in offerings"
              :key="offering.id"
              class="flex flex-wrap items-center gap-x-4 gap-y-1 rounded-lg border border-default bg-elevated/40 px-3 py-2"
            >
              <span class="flex items-center gap-1.5 text-sm text-highlighted">
                <UIcon name="i-lucide-building-2" class="size-4 shrink-0 text-muted" />
                {{ offering.campus }}
              </span>
              <span class="flex items-center gap-1.5 text-sm text-muted">
                <UIcon name="i-lucide-calendar" class="size-4 shrink-0" />
                {{ offering.period }}
              </span>
              <UBadge color="neutral" variant="subtle">
                {{ sessionLabels[offering.session] ?? offering.session }}
              </UBadge>
              <span class="flex items-center gap-1.5 text-sm text-muted">
                <UIcon name="i-lucide-users" class="size-4 shrink-0" />
                {{ offering.students }} {{ offering.students === 1 ? 'aluno' : 'alunos' }}
              </span>
            </div>
          </div>
          <div v-else class="flex items-center gap-2 text-sm text-muted">
            <UIcon name="i-lucide-calendar-x" class="size-4" />
            Nenhuma oferta usa esta grade
          </div>
        </section>
      </div>
    </template>
  </UDashboardPanel>

  <CourseCurriculumsEditModal v-model:open="editModalOpen" :curriculum="curriculumRef" @updated="refresh()" />
</template>
