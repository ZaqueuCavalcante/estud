<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { useDebounceFn } from '@vueuse/core'
import { DateFormatter, getLocalTimeZone, parseDate, type CalendarDate } from '@internationalized/date'

interface DomainEventItem {
  id: number
  institutionId: number
  entityUid: string
  type: string
  status: string
  occurredAt: string
  processedAt: string | null
  duration: number
  error: string | null
}

interface GetDomainEventsOut {
  total: number
  page: number
  pageSize: number
  items: DomainEventItem[]
}

const UBadge = resolveComponent('UBadge')
const UTooltip = resolveComponent('UTooltip')

const config = useRuntimeConfig()
const route = useRoute()
const router = useRouter()

const df = new DateFormatter('pt-BR', { dateStyle: 'medium' })

const statuses = [
  { label: 'Pendente', value: 'Pending' },
  { label: 'Processando', value: 'Processing' },
  { label: 'Sucesso', value: 'Success' },
  { label: 'Erro', value: 'Error' },
]

const statusColors: Record<string, 'neutral' | 'info' | 'success' | 'error'> = {
  Pending: 'neutral',
  Processing: 'info',
  Success: 'success',
  Error: 'error',
}

const statusLabels = Object.fromEntries(statuses.map(s => [s.value, s.label]))

// O `type` gravado é o nome completo do tipo .NET do evento; o select manda ele
// inteiro pro filtro e a tabela mostra só a descrição.
const types = [
  { label: 'Aluno criado', value: 'Estud.Back.Domain.Students.StudentCreatedDomainEvent' },
  { label: 'Atividade criada', value: 'Estud.Back.Domain.Classes.ClassActivityCreatedDomainEvent' },
]

const typeLabels = Object.fromEntries(types.map(t => [t.value, t.label]))

function parseQueryDate(value: unknown) {
  if (typeof value !== 'string' || !value) return undefined
  try {
    return parseDate(value)
  } catch {
    return undefined
  }
}

const status = ref<string | undefined>((route.query.status as string) || undefined)
const type = ref<string | undefined>((route.query.type as string) || undefined)
const institutionId = ref((route.query.institutionId as string) || '')
const entityUid = ref((route.query.entityUid as string) || '')
const from = ref<CalendarDate | undefined>(parseQueryDate(route.query.from))
const to = ref<CalendarDate | undefined>(parseQueryDate(route.query.to))
const fromOpen = ref(false)
const toOpen = ref(false)

// Os campos de texto vão pro fetch com debounce; os demais são clique único e
// podem ir direto.
const institutionIdFilter = ref(institutionId.value)
const applyInstitutionId = useDebounceFn((value: string) => { institutionIdFilter.value = value }, 300)
watch(institutionId, (value) => { applyInstitutionId(String(value ?? '')) })

const entityUidFilter = ref(entityUid.value)
const applyEntityUid = useDebounceFn((value: string) => { entityUidFilter.value = value }, 300)
watch(entityUid, (value) => { applyEntityUid(value) })

const appliedStatus = computed(() => status.value || undefined)
const appliedType = computed(() => type.value || undefined)
const appliedInstitutionId = computed(() => institutionIdFilter.value || undefined)
const appliedEntityUid = computed(() => entityUidFilter.value || undefined)
const appliedFrom = computed(() => from.value ? `${from.value.toString()}T00:00:00Z` : undefined)
const appliedTo = computed(() => to.value ? `${to.value.toString()}T23:59:59Z` : undefined)

const pageSize = 20
const page = ref(Number(route.query.page) || 1)

watch([status, type, institutionId, entityUid, from, to, page], () => {
  const query: Record<string, string> = {}
  if (status.value) query.status = status.value
  if (type.value) query.type = type.value
  if (institutionId.value) query.institutionId = institutionId.value
  if (entityUid.value) query.entityUid = entityUid.value
  if (from.value) query.from = from.value.toString()
  if (to.value) query.to = to.value.toString()
  if (page.value > 1) query.page = String(page.value)
  router.replace({ query })
}, { flush: 'post' })

watch([appliedStatus, appliedType, appliedInstitutionId, appliedEntityUid, appliedFrom, appliedTo], () => {
  page.value = 1
})

const hasFilters = computed(() => !!appliedStatus.value
  || !!appliedType.value
  || !!appliedInstitutionId.value
  || !!appliedEntityUid.value
  || !!appliedFrom.value
  || !!appliedTo.value)

function clearFilters() {
  status.value = undefined
  type.value = undefined
  institutionId.value = ''
  institutionIdFilter.value = ''
  entityUid.value = ''
  entityUidFilter.value = ''
  from.value = undefined
  to.value = undefined
}

const { data, status: fetchStatus } = await useFetch<GetDomainEventsOut>(
  `${config.public.backendUrl}/admin/domain-events`,
  {
    credentials: 'include',
    server: false,
    query: {
      status: appliedStatus,
      type: appliedType,
      institutionId: appliedInstitutionId,
      entityUid: appliedEntityUid,
      from: appliedFrom,
      to: appliedTo,
      page,
      pageSize,
    },
  },
)

const loading = computed(() => fetchStatus.value === 'pending')

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

const columns: TableColumn<DomainEventItem>[] = [
  {
    accessorKey: 'id',
    header: 'Id',
  },
  {
    accessorKey: 'institutionId',
    header: 'Instituição',
  },
  {
    accessorKey: 'type',
    header: 'Evento',
    cell: ({ row }) => typeLabels[row.original.type] ?? row.original.type.split('.').pop(),
  },
  {
    accessorKey: 'entityUid',
    header: 'Entidade',
    cell: ({ row }) => h('span', { class: 'font-mono text-xs' }, row.original.entityUid),
  },
  {
    accessorKey: 'status',
    header: 'Status',
    cell: ({ row }) => h(
      UBadge,
      { variant: 'subtle', color: statusColors[row.original.status] ?? 'neutral' },
      () => statusLabels[row.original.status] ?? row.original.status,
    ),
  },
  {
    accessorKey: 'occurredAt',
    header: 'Ocorrido em',
    cell: ({ row }) => formatDateTime(row.original.occurredAt),
  },
  {
    accessorKey: 'duration',
    header: 'Duração',
    cell: ({ row }) => row.original.processedAt ? `${row.original.duration} ms` : '—',
  },
  {
    accessorKey: 'error',
    header: 'Erro',
    cell: ({ row }) => row.original.error
      ? h(UTooltip, { text: row.original.error }, () => h(
        'span',
        { class: 'block truncate text-error' },
        row.original.error,
      ))
      : '—',
  },
]
</script>

<template>
  <UDashboardPanel id="admin-domain-events">
    <template #header>
      <UDashboardNavbar title="Eventos de domínio">
        <template #leading>
          <PageIcon />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex flex-col sm:flex-row sm:flex-wrap sm:items-center gap-2 pt-4">
        <USelectMenu
          v-model="status"
          :items="statuses"
          value-key="value"
          :search-input="false"
          clear
          class="w-full sm:w-40"
          :ui="{ base: 'h-8 text-base/5' }"
          placeholder="Status"
        />

        <USelectMenu
          v-model="type"
          :items="types"
          value-key="value"
          :search-input="false"
          clear
          class="w-full sm:w-52"
          :ui="{ base: 'h-8 text-base/5' }"
          placeholder="Evento"
        />

        <UInput
          v-model="institutionId"
          type="number"
          class="w-full sm:w-40"
          :ui="{ base: 'h-8' }"
          icon="i-lucide-building-2"
          placeholder="Instituição"
        >
          <template v-if="institutionId" #trailing>
            <UButton
              icon="i-lucide-x"
              color="neutral"
              variant="link"
              size="sm"
              aria-label="Remover filtro de instituição"
              @click="() => { institutionId = ''; institutionIdFilter = '' }"
            />
          </template>
        </UInput>

        <UInput
          v-model="entityUid"
          class="w-full sm:w-64"
          :ui="{ base: 'h-8' }"
          icon="i-lucide-fingerprint"
          placeholder="Entidade (uid)"
          :loading="loading"
        >
          <template v-if="entityUid" #trailing>
            <UButton
              icon="i-lucide-x"
              color="neutral"
              variant="link"
              size="sm"
              aria-label="Remover filtro de entidade"
              @click="() => { entityUid = ''; entityUidFilter = '' }"
            />
          </template>
        </UInput>

        <UPopover v-model:open="fromOpen" :content="{ align: 'start' }" :modal="true">
          <UButton color="neutral" variant="outline" class="w-full sm:w-44 h-8">
            <div class="flex items-center w-full gap-2">
              <UIcon name="i-lucide-calendar" class="size-4 shrink-0" />
              <span class="flex-1 text-left truncate">
                {{ from ? df.format(from.toDate(getLocalTimeZone())) : 'De' }}
              </span>
              <UIcon
                v-if="from"
                name="i-lucide-x"
                class="size-4 shrink-0"
                @click.stop="() => { from = undefined }"
              />
            </div>
          </UButton>
          <template #content>
            <UCalendar v-model="from" class="p-2" />
          </template>
        </UPopover>

        <UPopover v-model:open="toOpen" :content="{ align: 'start' }" :modal="true">
          <UButton color="neutral" variant="outline" class="w-full sm:w-44 h-8">
            <div class="flex items-center w-full gap-2">
              <UIcon name="i-lucide-calendar" class="size-4 shrink-0" />
              <span class="flex-1 text-left truncate">
                {{ to ? df.format(to.toDate(getLocalTimeZone())) : 'Até' }}
              </span>
              <UIcon
                v-if="to"
                name="i-lucide-x"
                class="size-4 shrink-0"
                @click.stop="() => { to = undefined }"
              />
            </div>
          </UButton>
          <template #content>
            <UCalendar v-model="to" class="p-2" />
          </template>
        </UPopover>

        <UButton
          v-if="hasFilters"
          icon="i-lucide-filter-x"
          color="neutral"
          variant="ghost"
          class="h-8"
          label="Limpar"
          @click="() => { clearFilters() }"
        />
      </div>

      <DataTable class="mt-4" :data="data?.items ?? []" :columns="columns" :loading="loading">
        <template #empty>
          <TableEmptyState
            :loading="loading"
            icon="i-lucide-zap"
            message="Nenhum evento de domínio registrado"
            :filtered="hasFilters"
            not-found-message="Nenhum evento de domínio encontrado com os filtros aplicados"
            @clear-filters="clearFilters"
          />
        </template>
      </DataTable>

      <div v-if="(data?.total ?? 0) > 0" class="flex items-center justify-between gap-2 mt-4">
        <UBadge color="neutral" variant="subtle" class="h-8 px-3">
          {{ data?.total }} {{ data?.total === 1 ? 'evento encontrado' : 'eventos encontrados' }}
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
