<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
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
const coursesModalOpen = ref(false)

// Os modais de edição esperam o mesmo formato usado na listagem de disciplinas
const disciplineRef = computed(() => data.value
  ? { id: data.value.id, name: data.value.name }
  : null,
)

const courses = computed(() => data.value?.courses ?? [])
const teachers = computed(() => data.value?.teachers ?? [])
const classes = computed(() => data.value?.classes ?? [])

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
    accessorKey: 'teachers',
    header: 'Professores',
    cell: ({ row }) => {
      const classTeachers = row.original.teachers
      if (!classTeachers.length) return h('span', { class: 'text-muted' }, 'Sem professor')
      return h('div', { class: 'flex flex-col gap-0.5' }, classTeachers.map(t =>
        h('span', { class: 'text-xs text-muted' }, t.name),
      ))
    },
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
      <div v-if="status === 'pending'" class="flex justify-center py-12">
        <UIcon name="i-lucide-loader-circle" class="size-8 animate-spin text-muted" />
      </div>

      <div v-else-if="error || !data" class="flex flex-col items-center gap-4 py-12">
        <UIcon name="i-lucide-triangle-alert" class="size-16 text-muted" />
        <p class="text-muted text-sm">
          Disciplina não encontrada
        </p>
        <UButton icon="i-lucide-arrow-left" label="Voltar" to="/disciplines" />
      </div>

      <div v-else class="flex flex-col gap-10 py-2">
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
          <span class="flex items-center gap-1.5 text-sm text-muted">
            <UIcon name="i-lucide-hash" class="size-4" />
            {{ data.code }}
          </span>
        </div>

        <section class="flex flex-col gap-3">
          <div class="flex items-center gap-1.5">
            <h2 class="font-semibold text-highlighted">
              Cursos
            </h2>
            <UTooltip text="Editar">
              <UButton
                icon="i-lucide-pencil"
                color="neutral"
                variant="ghost"
                size="xs"
                @click="(e) => { (e.currentTarget as HTMLElement).blur(); coursesModalOpen = true }"
              />
            </UTooltip>
          </div>

          <div v-if="courses.length" class="flex flex-wrap gap-2">
            <div
              v-for="course in courses"
              :key="course.id"
              class="flex items-center gap-1.5 rounded-full border border-default bg-elevated/40 px-3 py-1"
            >
              <UIcon name="i-lucide-notebook" class="size-3.5 text-muted" />
              <span class="text-sm text-highlighted">{{ course.name }}</span>
            </div>
          </div>
          <div v-else class="flex items-center gap-2 text-sm text-muted">
            <UIcon name="i-lucide-book-dashed" class="size-4" />
            Nenhum curso vinculado
          </div>
        </section>

        <section class="flex flex-col gap-3">
          <h2 class="font-semibold text-highlighted">
            Professores
          </h2>

          <div v-if="teachers.length" class="flex flex-wrap gap-2">
            <NuxtLink
              v-for="teacher in teachers"
              :key="teacher.id"
              :to="`/teachers/${teacher.id}`"
              class="flex items-center gap-1.5 rounded-full border border-default bg-elevated/40 px-3 py-1 transition-colors hover:border-primary/50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            >
              <UIcon name="i-lucide-user-pen" class="size-3.5 text-muted" />
              <span class="text-sm text-highlighted">{{ teacher.name }}</span>
            </NuxtLink>
          </div>
          <div v-else class="flex items-center gap-2 text-sm text-muted">
            <UIcon name="i-lucide-user-round-x" class="size-4" />
            Nenhum professor apto a lecionar esta disciplina
          </div>
        </section>

        <section class="flex flex-col gap-3">
          <h2 class="font-semibold text-highlighted">
            Turmas
          </h2>

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
  <DisciplinesCoursesModal v-model:open="coursesModalOpen" :discipline="disciplineRef" @updated="refresh()" />
</template>
