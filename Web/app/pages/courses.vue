<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { useDebounceFn } from '@vueuse/core'

interface CourseItem {
  id: number
  name: string
  type: string
  typeValue: string
  disciplines: number
}

interface GetCoursesOut {
  total: number
  page: number
  pageSize: number
  items: CourseItem[]
}

const UButton = resolveComponent('UButton')
const UTooltip = resolveComponent('UTooltip')

const config = useRuntimeConfig()
const createModalOpen = ref(false)
const editModalOpen = ref(false)
const disciplinesModalOpen = ref(false)
const selectedCourse = ref<CourseItem | null>(null)

function openEdit(course: CourseItem) {
  selectedCourse.value = course
  editModalOpen.value = true
}

function openDisciplines(course: CourseItem) {
  selectedCourse.value = course
  disciplinesModalOpen.value = true
}

const route = useRoute()
const router = useRouter()

const courseTypes = [
  { label: 'Bacharelado', value: 'Bacharelado' },
  { label: 'Licenciatura', value: 'Licenciatura' },
  { label: 'Tecnólogo', value: 'Tecnologo' },
  { label: 'Especialização', value: 'Especializacao' },
  { label: 'Mestrado', value: 'Mestrado' },
  { label: 'Doutorado', value: 'Doutorado' },
  { label: 'Pós-Doutorado', value: 'PosDoutorado' },
]

const filter = ref((route.query.filter as string) || '')
// Sem tipo escolhido o select fica vazio, mostrando só o placeholder. O botão
// de limpar do próprio select devolve o valor pra '' / undefined.
const type = ref<string | undefined>((route.query.type as string) || undefined)

// The filter actually applied to the fetch. Typing updates it debounced;
// clearing updates it immediately so the reload feels instant.
const appliedFilter = ref(filter.value)
const applyFilter = useDebounceFn((value: string) => { appliedFilter.value = value }, 300)
watch(filter, (value) => { applyFilter(value) })

// The select has no typing lag, so it goes straight to the fetch
const appliedType = computed(() => type.value || undefined)

const pageSize = 10
const page = ref(Number(route.query.page) || 1)

// Sync filters and page to URL
watch([filter, type, page], () => {
  const query: Record<string, string> = {}
  if (filter.value) query.filter = filter.value
  if (appliedType.value) query.type = appliedType.value
  if (page.value > 1) query.page = String(page.value)
  router.replace({ query })
}, { flush: 'post' })

// A new search starts over from the first page
watch([appliedFilter, appliedType], () => { page.value = 1 })

// Reflects the filters actually applied to the data being shown
const hasFilters = computed(() => appliedFilter.value.length > 0 || !!appliedType.value)

function clearFilters() {
  filter.value = ''
  appliedFilter.value = ''
  type.value = undefined
}

const { data, status, refresh } = await useFetch<GetCoursesOut>(`${config.public.backendUrl}/courses`, {
  credentials: 'include',
  server: false,
  query: { filter: appliedFilter, type: appliedType, page, pageSize }
})

const columns: TableColumn<CourseItem>[] = [
  {
    accessorKey: 'name',
    header: 'Nome',
  },
  {
    accessorKey: 'type',
    header: 'Tipo',
  },
  {
    accessorKey: 'disciplines',
    header: 'Disciplinas',
  },
  {
    id: 'actions',
    cell: ({ row }) => h('div', { class: 'flex gap-1' }, [
      h(UTooltip, { text: 'Editar' }, () => h(UButton, {
        icon: 'i-lucide-pencil',
        color: 'neutral',
        variant: 'ghost',
        size: 'sm',
        onClick: (e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); openEdit(row.original) },
      })),
      h(UTooltip, { text: 'Disciplinas' }, () => h(UButton, {
        icon: 'i-lucide-book-open',
        color: 'neutral',
        variant: 'ghost',
        size: 'sm',
        onClick: (e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); openDisciplines(row.original) },
      })),
    ]),
  },
]
</script>

<template>
  <UDashboardPanel id="courses">
    <template #header>
      <UDashboardNavbar title="Cursos">
        <template #leading>
          <PageIcon />
        </template>

      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pt-4">
        <div class="flex flex-col sm:flex-row gap-2">
          <UInput
            v-model="filter"
            class="w-full sm:max-w-sm"
            :ui="{ base: 'h-8' }"
            icon="i-lucide-search"
            placeholder="Buscar por nome..."
            :loading="status === 'pending'"
          >
            <template v-if="filter" #trailing>
              <UButton
                icon="i-lucide-x"
                color="neutral"
                variant="link"
                size="sm"
                aria-label="Remover filtro"
                @click="() => { filter = ''; appliedFilter = '' }"
              />
            </template>
          </UInput>
          <USelectMenu
            v-model="type"
            :items="courseTypes"
            value-key="value"
            :search-input="false"
            clear
            class="w-full sm:w-48"
            :ui="{ base: 'h-8 text-base/5' }"
            placeholder="Tipo"
          />
        </div>
        <UButton
          v-if="data?.items?.length || hasFilters"
          class="self-end sm:self-auto"
          icon="i-lucide-plus"
          label="Curso"
          @click="() => { createModalOpen = true }"
        />
      </div>
      <DataTable :data="data?.items ?? []" :columns="columns" :loading="status === 'pending'">
        <template #empty>
          <TableEmptyState
            :loading="status === 'pending'"
            icon="i-lucide-notebook"
            message="Nenhum curso cadastrado"
            button-label="Curso"
            :filtered="hasFilters"
            not-found-message="Nenhum curso encontrado com os filtros aplicados"
            @create="() => { createModalOpen = true }"
            @clear-filters="clearFilters"
          />
        </template>
      </DataTable>

      <div v-if="(data?.total ?? 0) > 0" class="flex items-center justify-between gap-2 mt-4">
        <UBadge color="neutral" variant="subtle" class="h-8 px-3">
          {{ data?.total }} {{ data?.total === 1 ? 'curso encontrado' : 'cursos encontrados' }}
        </UBadge>

        <UPagination
          v-if="(data?.total ?? 0) > pageSize"
          v-model:page="page"
          :items-per-page="pageSize"
          :total="data?.total ?? 0"
        />
      </div>
    </template>
  </UDashboardPanel>

  <CoursesCreateModal v-model:open="createModalOpen" @created="refresh()" />
  <CoursesEditModal v-model:open="editModalOpen" :course="selectedCourse" @updated="refresh()" />
  <CoursesDisciplinesModal v-model:open="disciplinesModalOpen" :course="selectedCourse" @updated="refresh()" />
</template>
