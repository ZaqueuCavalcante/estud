<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import type { GetWebhookCallsOut, WebhookCallItem } from '~/types/webhooks'

const UBadge = resolveComponent('UBadge')
const UButton = resolveComponent('UButton')
const UTooltip = resolveComponent('UTooltip')

const config = useRuntimeConfig()

const statuses = Object.entries(webhookCallStatusLabels).map(([value, label]) => ({ label, value }))

const status = ref<string | undefined>(undefined)
const appliedStatus = computed(() => status.value || undefined)
const hasFilters = computed(() => !!appliedStatus.value)

const page = ref(1)
const pageSize = 20

watch(appliedStatus, () => {
  page.value = 1
})

function clearFilters() {
  status.value = undefined
}

const detailsOpen = ref(false)
const selectedCallId = ref<number | null>(null)

function openDetails(item: WebhookCallItem) {
  selectedCallId.value = item.id
  detailsOpen.value = true
}

const { data, status: fetchStatus, refresh } = await useFetch<GetWebhookCallsOut>(
  `${config.public.backendUrl}/webhooks/calls`,
  {
    credentials: 'include',
    server: false,
    query: { status: appliedStatus, page, pageSize },
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

const columns: TableColumn<WebhookCallItem>[] = [
  {
    accessorKey: 'eventType',
    header: 'Evento',
    cell: ({ row }) => webhookEventLabels[row.original.eventType] ?? row.original.eventType,
  },
  {
    accessorKey: 'status',
    header: 'Status',
    cell: ({ row }) => h(
      UBadge,
      { variant: 'subtle', color: webhookCallStatusColors[row.original.status] ?? 'neutral' },
      () => webhookCallStatusLabels[row.original.status] ?? row.original.status,
    ),
  },
  {
    accessorKey: 'attemptsCount',
    header: 'Tentativas',
  },
  {
    accessorKey: 'createdAt',
    header: 'Criada em',
    cell: ({ row }) => formatDateTime(row.original.createdAt),
  },
  {
    id: 'actions',
    cell: ({ row }) => h(UTooltip, { text: 'Detalhes' }, () => h(UButton, {
      icon: 'i-lucide-eye',
      color: 'neutral',
      variant: 'ghost',
      size: 'sm',
      onClick: (e: MouseEvent) => {
        (e.currentTarget as HTMLElement).blur()
        openDetails(row.original)
      },
    })),
  },
]
</script>

<template>
  <div class="flex flex-col gap-4 sm:gap-6">
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

    <DataTable :data="data?.items ?? []" :columns="columns" :loading="loading">
      <template #empty>
        <TableEmptyState
          :loading="loading"
          icon="i-lucide-radio"
          message="Nenhuma chamada de webhook realizada"
          :filtered="hasFilters"
          not-found-message="Nenhuma chamada de webhook encontrada com o status selecionado"
          @clear-filters="clearFilters"
        />
      </template>
    </DataTable>

    <div v-if="(data?.total ?? 0) > pageSize" class="flex justify-end mt-4">
      <UPagination
        v-model:page="page"
        :items-per-page="pageSize"
        :total="data?.total ?? 0"
      />
    </div>

    <IntegrationsCallDetailsSlideover
      v-model:open="detailsOpen"
      :call-id="selectedCallId"
      @changed="() => { refresh() }"
    />
  </div>
</template>
