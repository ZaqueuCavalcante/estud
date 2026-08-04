<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { useDebounceFn } from '@vueuse/core'

interface InstitutionItem {
  id: number
  name: string
  createdAt: string
  usersCount: number
}

interface GetInstitutionsOut {
  total: number
  page: number
  pageSize: number
  items: InstitutionItem[]
}

const config = useRuntimeConfig()
const route = useRoute()
const router = useRouter()

const name = ref((route.query.name as string) || '')

// O filtro que de fato vai pro fetch: digitar atualiza com debounce, limpar
// atualiza na hora pra o reload parecer instantâneo.
const appliedName = ref(name.value)
const applyName = useDebounceFn((value: string) => { appliedName.value = value }, 300)
watch(name, (value) => { applyName(value) })

const pageSize = 10
const page = ref(Number(route.query.page) || 1)

watch([name, page], () => {
  const query: Record<string, string> = {}
  if (name.value) query.name = name.value
  if (page.value > 1) query.page = String(page.value)
  router.replace({ query })
}, { flush: 'post' })

// Uma busca nova recomeça da primeira página
watch(appliedName, () => { page.value = 1 })

const hasFilters = computed(() => appliedName.value.length > 0)

function clearFilters() {
  name.value = ''
  appliedName.value = ''
}

const { data, status } = await useFetch<GetInstitutionsOut>(`${config.public.backendUrl}/admin/institutions`, {
  credentials: 'include',
  server: false,
  query: { name: appliedName, page, pageSize },
})

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

const columns: TableColumn<InstitutionItem>[] = [
  {
    accessorKey: 'name',
    header: 'Nome',
  },
  {
    accessorKey: 'usersCount',
    header: 'Usuários',
  },
  {
    accessorKey: 'createdAt',
    header: 'Criada em',
    cell: ({ row }) => formatDate(row.original.createdAt),
  },
]
</script>

<template>
  <UDashboardPanel id="admin-institutions">
    <template #header>
      <UDashboardNavbar title="Instituições">
        <template #leading>
          <PageIcon />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pt-4">
        <UInput
          v-model="name"
          class="w-full sm:max-w-sm"
          :ui="{ base: 'h-8' }"
          icon="i-lucide-search"
          placeholder="Buscar por nome..."
          :loading="status === 'pending'"
        >
          <template v-if="name" #trailing>
            <UButton
              icon="i-lucide-x"
              color="neutral"
              variant="link"
              size="sm"
              aria-label="Remover filtro"
              @click="() => { name = ''; appliedName = '' }"
            />
          </template>
        </UInput>
      </div>

      <DataTable :data="data?.items ?? []" :columns="columns" :loading="status === 'pending'">
        <template #empty>
          <TableEmptyState
            :loading="status === 'pending'"
            icon="i-lucide-building-2"
            message="Nenhuma instituição cadastrada"
            :filtered="hasFilters"
            not-found-message="Nenhuma instituição encontrada com os filtros aplicados"
            @clear-filters="clearFilters"
          />
        </template>
      </DataTable>

      <div v-if="(data?.total ?? 0) > 0" class="flex items-center justify-between gap-2 mt-4">
        <UBadge color="neutral" variant="subtle" class="h-8 px-3">
          {{ data?.total }} {{ data?.total === 1 ? 'instituição encontrada' : 'instituições encontradas' }}
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
