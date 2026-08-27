<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'
import type { GetCourseDetailsOut } from '~/types/courses'

const route = useRoute()
const config = useRuntimeConfig()
const courseId = Number(route.params.courseId)

const sessionLabels: Record<string, string> = {
  Morning: 'Matutino',
  Afternoon: 'Vespertino',
  Evening: 'Noturno',
}

const { data, status, error, refresh } = await useFetch<GetCourseDetailsOut>(
  `${config.public.backendUrl}/courses/${courseId}/details`,
  { credentials: 'include', server: false },
)

const editModalOpen = ref(false)

// O modal de edição espera o mesmo formato usado na listagem de cursos
const courseRef = computed(() => data.value
  ? { id: data.value.id, name: data.value.name, typeValue: data.value.typeValue }
  : null,
)

const disciplines = computed(() => data.value?.disciplines ?? [])
const curriculums = computed(() => data.value?.curriculums ?? [])
const offerings = computed(() => data.value?.offerings ?? [])

// Grades e ofertas são criadas em telas próprias, então trocar de aba busca os
// dados de novo — voltar pra uma aba nunca pode mostrar uma lista velha.
const activeTab = ref('disciplines')

function selectTab(tab: string) {
  activeTab.value = tab
  refresh()
}

const tabs = computed(() => [[
  { label: 'Disciplinas', icon: 'i-lucide-book-open', active: activeTab.value === 'disciplines', onSelect: () => { selectTab('disciplines') } },
  { label: 'Grades', icon: 'i-lucide-layout-list', active: activeTab.value === 'curriculums', onSelect: () => { selectTab('curriculums') } },
  { label: 'Ofertas', icon: 'i-lucide-library', active: activeTab.value === 'offerings', onSelect: () => { selectTab('offerings') } },
]] satisfies NavigationMenuItem[][])

const breadcrumb = [
  { label: 'Cursos', to: '/courses', icon: 'i-lucide-notebook' },
  { label: 'Detalhes' },
]
</script>

<template>
  <UDashboardPanel id="course-details">
    <template #header>
      <UDashboardNavbar>
        <template #title>
          <UBreadcrumb :items="breadcrumb" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div v-if="status === 'pending' && !data" class="flex justify-center py-12">
        <UIcon name="i-lucide-loader-circle" class="size-8 animate-spin text-muted" />
      </div>

      <div v-else-if="error || !data" class="flex flex-col items-center gap-4 py-12">
        <UIcon name="i-lucide-triangle-alert" class="size-16 text-muted" />
        <p class="text-muted text-sm">
          Curso não encontrado
        </p>
        <UButton icon="i-lucide-arrow-left" label="Voltar" to="/courses" />
      </div>

      <div v-else class="w-full lg:max-w-2xl mx-auto min-w-0 flex flex-col gap-6 pb-2">
        <div class="flex flex-col gap-6">
          <div class="flex flex-col gap-1">
            <h1 class="text-2xl font-semibold tracking-tight text-highlighted">
              {{ data.name }}<UTooltip text="Editar">
                <UButton
                  icon="i-lucide-pencil"
                  color="neutral"
                  variant="ghost"
                  size="xs"
                  class="ml-1.5 align-middle"
                  @click="(e) => { (e.currentTarget as HTMLElement).blur(); editModalOpen = true }"
                />
              </UTooltip>
            </h1>
            <p class="flex items-center gap-1.5 text-sm text-muted">
              <UIcon name="i-lucide-graduation-cap" class="size-4 shrink-0" />
              {{ data.type }}
            </p>
          </div>

          <UNavigationMenu :items="tabs" highlight class="-mx-1" />
        </div>

        <CoursesDisciplinesEditor
          v-show="activeTab === 'disciplines'"
          :course-id="data.id"
          :disciplines="disciplines"
          @updated="refresh()"
        />

        <section v-if="activeTab === 'curriculums'" class="flex flex-col gap-4">
          <p class="text-sm text-muted">
            As grades curriculares deste curso.
          </p>

          <div v-if="curriculums.length" class="grid gap-3 sm:grid-cols-2">
            <NuxtLink
              v-for="curriculum in curriculums"
              :key="curriculum.id"
              :to="`/course-curriculums/${curriculum.id}`"
              class="flex flex-col gap-2 rounded-xl border border-default bg-elevated/40 px-4 py-3 transition-all duration-200 hover:border-primary/50 hover:shadow-md focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            >
              <p class="truncate text-base font-bold text-highlighted">{{ curriculum.name }}</p>

              <span class="flex items-center gap-1.5 text-sm text-muted">
                <UIcon name="i-lucide-book-open" class="size-4 shrink-0" />
                {{ curriculum.disciplines }} {{ curriculum.disciplines === 1 ? 'disciplina' : 'disciplinas' }}
              </span>
            </NuxtLink>
          </div>
          <div v-else class="flex items-center gap-2 text-sm text-muted">
            <UIcon name="i-lucide-book-dashed" class="size-4" />
            Nenhuma grade curricular cadastrada
          </div>
        </section>

        <section v-else-if="activeTab === 'offerings'" class="flex flex-col gap-4">
          <p class="text-sm text-muted">
            As ofertas deste curso, por campus e período.
          </p>

          <div v-if="offerings.length" class="grid gap-3 sm:grid-cols-2">
            <NuxtLink
              v-for="offering in offerings"
              :key="offering.id"
              :to="`/course-offerings/${offering.id}`"
              class="flex flex-col gap-2 rounded-xl border border-default bg-elevated/40 px-4 py-3 transition-all duration-200 hover:border-primary/50 hover:shadow-md focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            >
              <p class="truncate text-base font-bold text-highlighted">{{ offering.curriculum }}</p>

              <div class="flex flex-col gap-1 text-sm text-muted">
                <span class="flex items-center gap-1.5">
                  <UIcon name="i-lucide-building-2" class="size-4 shrink-0" />
                  <span class="truncate">{{ offering.campus }}</span>
                </span>
                <span class="flex items-center gap-1.5">
                  <UIcon name="i-lucide-calendar" class="size-4 shrink-0" />
                  <span class="truncate">{{ offering.period }} · {{ sessionLabels[offering.session] ?? offering.session }}</span>
                </span>
                <span class="flex items-center gap-1.5">
                  <UIcon name="i-lucide-users" class="size-4 shrink-0" />
                  {{ offering.students }} {{ offering.students === 1 ? 'aluno' : 'alunos' }}
                </span>
              </div>
            </NuxtLink>
          </div>
          <div v-else class="flex items-center gap-2 text-sm text-muted">
            <UIcon name="i-lucide-calendar-x" class="size-4" />
            Nenhuma oferta cadastrada
          </div>
        </section>
      </div>
    </template>
  </UDashboardPanel>

  <CoursesEditModal v-model:open="editModalOpen" :course="courseRef" @updated="refresh()" />
</template>
