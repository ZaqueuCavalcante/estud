<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { useDebounceFn } from '@vueuse/core'

const UBadge = resolveComponent('UBadge')
const UButton = resolveComponent('UButton')
const UTooltip = resolveComponent('UTooltip')

interface ClassItem {
  id: number
  discipline: string
  teachers: string[]
  period: string
  vacancies: number
  status: string
}

interface GetClassesOut {
  total: number
  page: number
  pageSize: number
  items: ClassItem[]
}

const statusLabels: Record<string, string> = {
  OnPreEnrollment: 'Pré-matrícula',
  OnEnrollment: 'Matrícula',
  OnReview: 'Revisão',
  Started: 'Iniciada',
  Finalized: 'Finalizada',
}

const statusColors: Record<string, 'neutral' | 'primary' | 'success' | 'warning' | 'error' | 'info'> = {
  OnPreEnrollment: 'neutral',
  OnEnrollment: 'info',
  OnReview: 'warning',
  Started: 'primary',
  Finalized: 'success',
}

const config = useRuntimeConfig()
const createModalOpen = ref(false)

const route = useRoute()
const router = useRouter()

interface PeriodItem {
  id: number
  name: string
}

interface GetAcademicPeriodsOut {
  total: number
  items: PeriodItem[]
}

const { data: periodsData } = await useFetch<GetAcademicPeriodsOut>(`${config.public.backendUrl}/periods/academic`, {
  credentials: 'include',
  server: false,
})

const periodOptions = computed(() =>
  (periodsData.value?.items ?? []).map(p => ({ label: p.name, value: p.id })),
)

const statusOptions = Object.entries(statusLabels).map(([value, label]) => ({ label, value }))

const filter = ref((route.query.filter as string) || '')
// Sem opção escolhida o select fica vazio, mostrando só o placeholder.
const periodId = ref<number | undefined>(Number(route.query.periodId) || undefined)
const classStatus = ref<string | undefined>((route.query.status as string) || undefined)

// The filter actually applied to the fetch. Typing updates it debounced;
// clearing updates it immediately so the reload feels instant.
const appliedFilter = ref(filter.value)
const applyFilter = useDebounceFn((value: string) => { appliedFilter.value = value }, 300)
watch(filter, (value) => { applyFilter(value) })

// The selects have no typing lag, so they go straight to the fetch
const appliedPeriodId = computed(() => periodId.value || undefined)
const appliedStatus = computed(() => classStatus.value || undefined)

const pageSize = 10
const page = ref(Number(route.query.page) || 1)

// Sync filters and page to URL
watch([filter, periodId, classStatus, page], () => {
  const query: Record<string, string> = {}
  if (filter.value) query.filter = filter.value
  if (appliedPeriodId.value) query.periodId = String(appliedPeriodId.value)
  if (appliedStatus.value) query.status = appliedStatus.value
  if (page.value > 1) query.page = String(page.value)
  router.replace({ query })
}, { flush: 'post' })

// A new search starts over from the first page
watch([appliedFilter, appliedPeriodId, appliedStatus], () => { page.value = 1 })

// Reflects the filters actually applied to the data being shown
const hasFilters = computed(() => appliedFilter.value.length > 0
  || !!appliedPeriodId.value
  || !!appliedStatus.value,
)

function clearFilters() {
  filter.value = ''
  appliedFilter.value = ''
  periodId.value = undefined
  classStatus.value = undefined
}

const { data, status, refresh } = await useFetch<GetClassesOut>(`${config.public.backendUrl}/classes`, {
  credentials: 'include',
  server: false,
  query: {
    filter: appliedFilter,
    periodId: appliedPeriodId,
    status: appliedStatus,
    page,
    pageSize,
  },
})

const columns: TableColumn<ClassItem>[] = [
  {
    accessorKey: 'discipline',
    header: 'Disciplina',
  },
  {
    accessorKey: 'teachers',
    header: 'Professores',
    cell: ({ row }) => row.original.teachers.join(', ') || '—',
  },
  {
    accessorKey: 'period',
    header: 'Período',
  },
  {
    accessorKey: 'status',
    header: 'Status',
    cell: ({ row }) => {
      const s = row.original.status
      return h(UBadge, {
        label: statusLabels[s] ?? s,
        color: statusColors[s] ?? 'neutral',
        variant: 'subtle',
      })
    },
  },
  {
    id: 'actions',
    header: '',
    cell: ({ row }) => h('div', { class: 'flex justify-end' }, h(UTooltip, { text: 'Ver detalhes' }, () => h(UButton, {
      icon: 'i-lucide-arrow-right',
      color: 'neutral',
      variant: 'ghost',
      to: `/classes/${row.original.id}`,
      'aria-label': 'Ver detalhes',
    }))),
  },
]
</script>

<template>
  <UDashboardPanel id="classes">
    <template #header>
      <UDashboardNavbar title="Turmas">
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
            placeholder="Buscar por nome ou código da disciplina..."
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
            v-model="periodId"
            :items="periodOptions"
            value-key="value"
            :search-input="false"
            clear
            class="w-full sm:w-46"
            :ui="{ base: 'h-8 text-base/5' }"
            icon="i-lucide-calendar-range"
            placeholder="Período"
          />
          <USelectMenu
            v-model="classStatus"
            :items="statusOptions"
            value-key="value"
            :search-input="false"
            clear
            class="w-full sm:w-46"
            :ui="{ base: 'h-8 text-base/5' }"
            icon="i-lucide-list-checks"
            placeholder="Status"
          />
        </div>
        <div class="flex items-center justify-between gap-2 self-stretch sm:self-auto">
          <UButton
            v-if="data?.items?.length || hasFilters"
            icon="i-lucide-plus"
            label="Turma"
            @click="() => { createModalOpen = true }"
          />
          <UBadge
            v-if="(data?.total ?? 0) > 0"
            color="neutral"
            variant="subtle"
            class="h-8 px-3 sm:hidden"
          >
            {{ data?.total }} {{ data?.total === 1 ? 'turma' : 'turmas' }}
          </UBadge>
        </div>
      </div>
      <DataTable :data="data?.items ?? []" :columns="columns" :loading="status === 'pending'">
        <template #empty>
          <TableEmptyState
            :loading="status === 'pending'"
            icon="i-lucide-presentation"
            message="Nenhuma turma cadastrada"
            button-label="Turma"
            :filtered="hasFilters"
            not-found-message="Nenhuma turma encontrada com os filtros aplicados"
            @create="() => { createModalOpen = true }"
            @clear-filters="clearFilters"
          />
        </template>
      </DataTable>

      <div v-if="(data?.total ?? 0) > 0" class="flex items-center justify-end sm:justify-between gap-2 mt-4">
        <UBadge color="neutral" variant="subtle" class="h-8 px-3 max-sm:hidden">
          {{ data?.total }} {{ data?.total === 1 ? 'turma encontrada' : 'turmas encontradas' }}
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

  <ClassesCreateModal v-model:open="createModalOpen" @created="refresh()" />
</template>
