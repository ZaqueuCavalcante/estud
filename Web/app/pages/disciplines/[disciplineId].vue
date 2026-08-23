<script setup lang="ts">
import type { NavigationMenuItem, TableColumn } from '@nuxt/ui'
import type { DisciplineClassItem, GetDisciplineDetailsOut } from '~/types/disciplines'

const UBadge = resolveComponent('UBadge')
const UButton = resolveComponent('UButton')
const UTooltip = resolveComponent('UTooltip')

const route = useRoute()
const config = useRuntimeConfig()
const disciplineId = route.params.disciplineId as string

const { data, status, error, refresh } = await useFetch<GetDisciplineDetailsOut>(
  `${config.public.backendUrl}/disciplines/${disciplineId}/details`,
  { credentials: 'include', server: false },
)

const editModalOpen = ref(false)

// O modal de edição espera o mesmo formato usado na listagem de disciplinas
const disciplineRef = computed(() => data.value
  ? { id: data.value.id, name: data.value.name }
  : null,
)

const courses = computed(() => data.value?.courses ?? [])
const teachers = computed(() => data.value?.teachers ?? [])
const classes = computed(() => data.value?.classes ?? [])

// Professores e turmas são mantidos em telas próprias, então trocar de aba
// busca os dados de novo — voltar pra uma aba nunca pode mostrar uma lista
// velha.
const activeTab = ref('courses')

function selectTab(tab: string) {
  activeTab.value = tab
  refresh()
}

const tabs = computed(() => [[
  { label: 'Cursos', icon: 'i-lucide-notebook', active: activeTab.value === 'courses', onSelect: () => { selectTab('courses') } },
  { label: 'Professores', icon: 'i-lucide-user-pen', active: activeTab.value === 'teachers', onSelect: () => { selectTab('teachers') } },
  { label: 'Turmas', icon: 'i-lucide-door-open', active: activeTab.value === 'classes', onSelect: () => { selectTab('classes') } },
]] satisfies NavigationMenuItem[][])

const classColumns: TableColumn<DisciplineClassItem>[] = [
  {
    accessorKey: 'period',
    header: 'Período',
    cell: ({ row }) => h('span', { class: 'font-medium text-highlighted' }, row.original.period),
  },
  {
    accessorKey: 'campus',
    header: 'Campus',
    cell: ({ row }) => row.original.campus ?? h('span', { class: 'text-muted' }, 'Sem campus'),
  },
  {
    accessorKey: 'students',
    header: 'Alunos',
    cell: ({ row }) => `${row.original.students} / ${row.original.vacancies}`,
  },
  {
    accessorKey: 'status',
    header: 'Status',
    cell: ({ row }) => h(UBadge, {
      label: classStatusLabels[row.original.status] ?? row.original.status,
      color: classStatusColors[row.original.status] ?? 'neutral',
      variant: 'subtle',
    }),
  },
  {
    id: 'actions',
    header: '',
    cell: ({ row }) => h('div', { class: 'flex justify-end' }, h(UTooltip, { text: 'Ver turma' }, () => h(UButton, {
      icon: 'i-lucide-arrow-right',
      color: 'neutral',
      variant: 'ghost',
      to: `/classes/${row.original.id}`,
      'aria-label': 'Ver turma',
    }))),
  },
]

const breadcrumb = [
  { label: 'Disciplinas', to: '/disciplines', icon: 'i-lucide-book-open' },
  { label: 'Detalhes' },
]
</script>

<template>
  <UDashboardPanel id="discipline-details">
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
          Disciplina não encontrada
        </p>
        <UButton icon="i-lucide-arrow-left" label="Voltar" to="/disciplines" />
      </div>

      <div v-else class="w-full lg:max-w-2xl mx-auto min-w-0 flex flex-col gap-6 py-2">
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
              <UIcon name="i-lucide-hash" class="size-4 shrink-0" />
              {{ data.code }}
            </p>
          </div>

          <UNavigationMenu :items="tabs" highlight class="-mx-1" />
        </div>

        <DisciplinesCoursesEditor
          v-show="activeTab === 'courses'"
          :discipline-id="data.id"
          :courses="courses"
          @updated="refresh()"
        />

        <DisciplinesTeachersEditor
          v-show="activeTab === 'teachers'"
          :discipline-id="data.id"
          :teachers="teachers"
          @updated="refresh()"
        />

        <section v-if="activeTab === 'classes'" class="flex flex-col gap-4">
          <p class="text-sm text-muted">
            As turmas abertas nesta disciplina.
          </p>

          <DataTable :data="classes" :columns="classColumns">
            <template #empty>
              <div class="flex items-center justify-center gap-2 py-6 text-sm text-muted">
                <UIcon name="i-lucide-door-closed" class="size-4" />
                Nenhuma turma nesta disciplina
              </div>
            </template>
          </DataTable>
        </section>
      </div>
    </template>
  </UDashboardPanel>

  <DisciplinesEditModal v-model:open="editModalOpen" :discipline="disciplineRef" @updated="refresh()" />
</template>
