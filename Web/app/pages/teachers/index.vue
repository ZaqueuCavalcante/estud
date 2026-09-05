<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { useDebounceFn } from '@vueuse/core'

interface TeacherItem {
  id: number
  name: string
  email: string
  campi: number
  disciplines: number
}

interface GetTeachersOut {
  total: number
  page: number
  pageSize: number
  items: TeacherItem[]
}

const UButton = resolveComponent('UButton')
const UTooltip = resolveComponent('UTooltip')

const config = useRuntimeConfig()
const createModalOpen = ref(false)

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

const { data, status, refresh } = await useFetch<GetTeachersOut>(`${config.public.backendUrl}/teachers`, {
  credentials: 'include',
  server: false,
  query: { filter: appliedFilter, page, pageSize }
})

const columns: TableColumn<TeacherItem>[] = [
  {
    accessorKey: 'name',
    header: 'Nome',
  },
  {
    accessorKey: 'email',
    header: 'Email',
  },
  {
    accessorKey: 'campi',
    header: 'Campi',
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
        to: `/teachers/${row.original.id}`,
        'aria-label': 'Ver detalhes',
      })),
    ]),
  },
]
</script>

<template>
  <UDashboardPanel id="teachers">
    <template #header>
      <UDashboardNavbar title="Professores">
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
          placeholder="Buscar por nome ou email..."
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
            label="Professor"
            @click="() => { createModalOpen = true }"
          />
          <UBadge
            v-if="(data?.total ?? 0) > 0"
            color="neutral"
            variant="subtle"
            class="h-8 px-3 sm:hidden"
          >
            {{ data?.total }} {{ data?.total === 1 ? 'professor' : 'professores' }}
          </UBadge>
        </div>
      </div>
      <DataTable :data="data?.items ?? []" :columns="columns" :loading="status === 'pending'">
        <template #empty>
          <TableEmptyState
            :loading="status === 'pending'"
            icon="i-lucide-user-pen"
            message="Nenhum professor cadastrado"
            button-label="Professor"
            :filtered="hasFilters"
            not-found-message="Nenhum professor encontrado com os filtros aplicados"
            @create="() => { createModalOpen = true }"
            @clear-filters="clearFilters"
          />
        </template>
      </DataTable>

      <div v-if="(data?.total ?? 0) > 0" class="flex items-center justify-end sm:justify-between gap-2 mt-4">
        <UBadge color="neutral" variant="subtle" class="h-8 px-3 max-sm:hidden">
          {{ data?.total }} {{ data?.total === 1 ? 'professor encontrado' : 'professores encontrados' }}
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

  <TeachersCreateModal v-model:open="createModalOpen" @created="refresh()" />
</template>
