<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { useDebounceFn } from '@vueuse/core'

interface CourseCurriculumItem {
  id: number
  name: string
  course: string
  disciplines: number
}

interface GetCourseCurriculumsOut {
  total: number
  page: number
  pageSize: number
  items: CourseCurriculumItem[]
}

const UButton = resolveComponent('UButton')
const UTooltip = resolveComponent('UTooltip')

const config = useRuntimeConfig()

const route = useRoute()
const router = useRouter()

const filter = ref((route.query.filter as string) || '')

// The filter actually applied to the fetch. Typing updates it debounced;
// clearing updates it immediately so the reload feels instant.
const appliedFilter = ref(filter.value)
const applyFilter = useDebounceFn((value: string) => { appliedFilter.value = value }, 300)
watch(filter, (value) => { applyFilter(value) })

const pageSize = 10
const page = ref(Number(route.query.page) || 1)

// Sync filter and page to URL
watch([filter, page], () => {
  const query: Record<string, string> = {}
  if (filter.value) query.filter = filter.value
  if (page.value > 1) query.page = String(page.value)
  router.replace({ query })
}, { flush: 'post' })

// A new search starts over from the first page
watch(appliedFilter, () => { page.value = 1 })

// Reflects the filters actually applied to the data being shown
const hasFilters = computed(() => appliedFilter.value.length > 0)

function clearFilters() {
  filter.value = ''
  appliedFilter.value = ''
}

const { data, status } = await useFetch<GetCourseCurriculumsOut>(`${config.public.backendUrl}/course-curriculums`, {
  credentials: 'include',
  server: false,
  query: { filter: appliedFilter, page, pageSize }
})

const columns: TableColumn<CourseCurriculumItem>[] = [
  {
    accessorKey: 'name',
    header: 'Nome',
  },
  {
    accessorKey: 'course',
    header: 'Curso',
  },
  {
    accessorKey: 'disciplines',
    header: 'Disciplinas',
  },
  {
    id: 'actions',
    header: '',
    cell: ({ row }) => h('div', { class: 'flex gap-1' }, [
      h(UTooltip, { text: 'Ver detalhes' }, () => h(UButton, {
        icon: 'i-lucide-arrow-right',
        color: 'neutral',
        variant: 'ghost',
        size: 'sm',
        to: `/course-curriculums/${row.original.id}`,
        'aria-label': 'Ver detalhes',
      })),
    ]),
  },
]
</script>

<template>
  <UDashboardPanel id="course-curriculums">
    <template #header>
      <UDashboardNavbar title="Grades Curriculares">
        <template #leading>
          <PageIcon />
        </template>

      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pt-4">
        <UInput
          v-model="filter"
          class="w-full sm:max-w-sm"
          icon="i-lucide-search"
          placeholder="Buscar por nome ou curso..."
          :loading="status === 'pending'"
        >
          <template v-if="filter" #trailing>
            <UButton
              icon="i-lucide-x"
              color="neutral"
              variant="link"
              size="sm"
              aria-label="Remover filtro"
              @click="clearFilters"
            />
          </template>
        </UInput>
        <div class="flex items-center justify-between gap-2 self-stretch sm:self-auto">
          <UButton
            v-if="data?.items?.length || filter"
            icon="i-lucide-plus"
            label="Grade"
            to="/course-curriculums/new"
          />
          <UBadge
            v-if="(data?.total ?? 0) > 0"
            color="neutral"
            variant="subtle"
            class="h-8 px-3 sm:hidden"
          >
            {{ data?.total }} {{ data?.total === 1 ? 'grade' : 'grades' }}
          </UBadge>
        </div>
      </div>
      <DataTable :data="data?.items ?? []" :columns="columns" :loading="status === 'pending'">
        <template #empty>
          <TableEmptyState
            :loading="status === 'pending'"
            icon="i-lucide-layout-list"
            message="Nenhuma grade curricular cadastrada"
            button-label="Grade"
            :filtered="hasFilters"
            not-found-message="Nenhuma grade curricular encontrada com os filtros aplicados"
            @create="() => { navigateTo('/course-curriculums/new') }"
            @clear-filters="clearFilters"
          />
        </template>
      </DataTable>

      <div v-if="(data?.total ?? 0) > 0" class="flex items-center justify-end sm:justify-between gap-2 mt-4">
        <UBadge color="neutral" variant="subtle" class="h-8 px-3 max-sm:hidden">
          {{ data?.total }} {{ data?.total === 1 ? 'grade encontrada' : 'grades encontradas' }}
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
</template>
