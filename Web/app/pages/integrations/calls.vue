<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import type { GetWebhookCallsOut, WebhookCallItem } from '~/types/webhooks'

const UBadge = resolveComponent('UBadge')
const UButton = resolveComponent('UButton')
const UTooltip = resolveComponent('UTooltip')

const config = useRuntimeConfig()

const page = ref(1)
const pageSize = 20

const detailsOpen = ref(false)
const selectedCallId = ref<number | null>(null)

function openDetails(item: WebhookCallItem) {
  selectedCallId.value = item.id
  detailsOpen.value = true
}

const { data, status } = await useFetch<GetWebhookCallsOut>(
  `${config.public.backendUrl}/webhooks/calls`,
  {
    credentials: 'include',
    server: false,
    query: { page, pageSize },
  },
)

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
  <div>
    <DataTable :data="data?.items ?? []" :columns="columns" :loading="status === 'pending'">
      <template #empty>
        <div class="flex flex-col items-center justify-center gap-3 py-12 text-center">
          <UIcon name="i-lucide-radio" class="size-8 text-muted" />
          <p class="text-sm text-muted">Nenhuma chamada de webhook realizada</p>
        </div>
      </template>
    </DataTable>

    <div v-if="(data?.total ?? 0) > pageSize" class="flex justify-end mt-4">
      <UPagination
        v-model:page="page"
        :items-per-page="pageSize"
        :total="data?.total ?? 0"
      />
    </div>

    <IntegrationsCallDetailsSlideover v-model:open="detailsOpen" :call-id="selectedCallId" />
  </div>
</template>
