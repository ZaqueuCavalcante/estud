<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { useDebounceFn } from '@vueuse/core'

interface DisciplineItem {
  id: number
  name: string
  code: string
  hasCourses: boolean
  hasTeachers: boolean
}

interface GetDisciplinesOut {
  total: number
  page: number
  pageSize: number
  items: DisciplineItem[]
}

const UBadge = resolveComponent('UBadge')
const UButton = resolveComponent('UButton')
const UTooltip = resolveComponent('UTooltip')

const config = useRuntimeConfig()
const createModalOpen = ref(false)

const route = useRoute()
const router = useRouter()

const courseOptions = [
  { label: 'Vinculadas', value: 'true' },
  { label: 'Sem curso', value: 'false' },
]

const teacherOptions = [
  { label: 'Vinculadas', value: 'true' },
  { label: 'Sem professor', value: 'false' },
]

const filter = ref((route.query.filter as string) || '')
// Sem opção escolhida o select fica vazio, mostrando só o placeholder.
const hasCourses = ref<string | undefined>((route.query.hasCourses as string) || undefined)
const hasTeachers = ref<string | undefined>((route.query.hasTeachers as string) || undefined)

// The filter actually applied to the fetch. Typing updates it debounced;
// clearing updates it immediately so the reload feels instant.
const appliedFilter = ref(filter.value)
const applyFilter = useDebounceFn((value: string) => { appliedFilter.value = value }, 300)
watch(filter, (value) => { applyFilter(value) })

// The selects have no typing lag, so they go straight to the fetch
const appliedHasCourses = computed(() => hasCourses.value || undefined)
const appliedHasTeachers = computed(() => hasTeachers.value || undefined)

const pageSize = 10
const page = ref(Number(route.query.page) || 1)

// Sync filters and page to URL
watch([filter, hasCourses, hasTeachers, page], () => {
  const query: Record<string, string> = {}
  if (filter.value) query.filter = filter.value
  if (appliedHasCourses.value) query.hasCourses = appliedHasCourses.value
  if (appliedHasTeachers.value) query.hasTeachers = appliedHasTeachers.value
  if (page.value > 1) query.page = String(page.value)
  router.replace({ query })
}, { flush: 'post' })

// A new search starts over from the first page
watch([appliedFilter, appliedHasCourses, appliedHasTeachers], () => { page.value = 1 })

// Reflects the filters actually applied to the data being shown
const hasFilters = computed(() => appliedFilter.value.length > 0
  || !!appliedHasCourses.value
  || !!appliedHasTeachers.value,
)

function clearFilters() {
  filter.value = ''
  appliedFilter.value = ''
  hasCourses.value = undefined
  hasTeachers.value = undefined
}

const { data, status, refresh } = await useFetch<GetDisciplinesOut>(`${config.public.backendUrl}/disciplines`, {
  credentials: 'include',
  server: false,
  query: {
    filter: appliedFilter,
    hasCourses: appliedHasCourses,
    hasTeachers: appliedHasTeachers,
    page,
    pageSize
  }
})

function linkBadge(linked: boolean, missingLabel: string) {
  return h(UBadge, {
    label: linked ? 'Vinculada' : missingLabel,
    color: linked ? 'success' : 'neutral',
    variant: 'subtle',
  })
}

const columns: TableColumn<DisciplineItem>[] = [
  {
    accessorKey: 'name',
    header: 'Nome',
  },
  {
    accessorKey: 'code',
    header: 'Código',
  },
  {
    accessorKey: 'hasCourses',
    header: 'Cursos',
    cell: ({ row }) => linkBadge(row.original.hasCourses, 'Sem curso'),
  },
  {
    accessorKey: 'hasTeachers',
    header: 'Professores',
    cell: ({ row }) => linkBadge(row.original.hasTeachers, 'Sem professor'),
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
        to: `/disciplines/${row.original.id}`,
        'aria-label': 'Ver detalhes',
      })),
    ]),
  },
]
</script>

<template>
  <UDashboardPanel id="disciplines">
    <template #header>
      <UDashboardNavbar title="Disciplinas">
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
            placeholder="Buscar por nome ou código..."
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
            v-model="hasCourses"
            :items="courseOptions"
            value-key="value"
            :search-input="false"
            clear
            class="w-full sm:w-46"
            :ui="{ base: 'h-8 text-base/5' }"
            icon="i-lucide-notebook"
            placeholder="Cursos"
          />
          <USelectMenu
            v-model="hasTeachers"
            :items="teacherOptions"
            value-key="value"
            :search-input="false"
            clear
            class="w-full sm:w-54"
            :ui="{ base: 'h-8 text-base/5' }"
            icon="i-lucide-user-pen"
            placeholder="Professores"
          />
        </div>
        <div class="flex items-center justify-between gap-2 self-stretch sm:self-auto">
          <UButton
            v-if="data?.items?.length || hasFilters"
            icon="i-lucide-plus"
            label="Disciplina"
            @click="() => { createModalOpen = true }"
          />
          <UBadge
            v-if="(data?.total ?? 0) > 0"
            color="neutral"
            variant="subtle"
            class="h-8 px-3 sm:hidden"
          >
            {{ data?.total }} {{ data?.total === 1 ? 'disciplina' : 'disciplinas' }}
          </UBadge>
        </div>
      </div>
      <DataTable :data="data?.items ?? []" :columns="columns" :loading="status === 'pending'">
        <template #empty>
          <TableEmptyState
            :loading="status === 'pending'"
            icon="i-lucide-book-open"
            message="Nenhuma disciplina cadastrada"
            button-label="Disciplina"
            :filtered="hasFilters"
            not-found-message="Nenhuma disciplina encontrada com os filtros aplicados"
            @create="() => { createModalOpen = true }"
            @clear-filters="clearFilters"
          />
        </template>
      </DataTable>

      <div v-if="(data?.total ?? 0) > 0" class="flex items-center justify-end sm:justify-between gap-2 mt-4">
        <UBadge color="neutral" variant="subtle" class="h-8 px-3 max-sm:hidden">
          {{ data?.total }} {{ data?.total === 1 ? 'disciplina encontrada' : 'disciplinas encontradas' }}
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

  <DisciplinesCreateModal v-model:open="createModalOpen" @created="refresh()" />
</template>
