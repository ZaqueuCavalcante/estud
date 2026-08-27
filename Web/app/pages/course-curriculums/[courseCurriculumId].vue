<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'
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

const disciplines = computed(() => data.value?.disciplines ?? [])
const offerings = computed(() => data.value?.offerings ?? [])

const activeTab = ref('disciplines')

const tabs = computed(() => [[
  { label: 'Disciplinas', icon: 'i-lucide-book-open', active: activeTab.value === 'disciplines', onSelect: () => { activeTab.value = 'disciplines' } },
  { label: 'Ofertas', icon: 'i-lucide-library', active: activeTab.value === 'offerings', onSelect: () => { activeTab.value = 'offerings' } },
]] satisfies NavigationMenuItem[][])

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

      <div v-else class="w-full lg:max-w-2xl mx-auto min-w-0 flex flex-col gap-6 pb-2">
        <div class="flex flex-col gap-6">
          <div class="flex flex-col gap-1">
            <h1 class="text-2xl font-semibold tracking-tight text-highlighted">
              {{ data.name }}
            </h1>
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

          <UNavigationMenu :items="tabs" highlight class="-mx-1" />
        </div>

        <CourseCurriculumsDisciplinesEditor
          v-show="activeTab === 'disciplines'"
          :curriculum-id="data.id"
          :curriculum-name="data.name"
          :course-id="data.courseId"
          :disciplines="disciplines"
          @updated="refresh()"
        />

        <section v-if="activeTab === 'offerings'" class="flex flex-col gap-4">
          <p class="text-sm text-muted">
            As ofertas que usam esta grade.
          </p>

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
</template>
