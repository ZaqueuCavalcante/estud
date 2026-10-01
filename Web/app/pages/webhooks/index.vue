<script setup lang="ts">
interface WebhookSubscriptionItem {
  id: number
  name: string
  url: string
  isActive: boolean
  events: string[]
  createdAt: string
  customHeaders: Record<string, string>
}

interface GetWebhookSubscriptionsOut {
  total: number
  items: WebhookSubscriptionItem[]
}

const config = useRuntimeConfig()
const createModalOpen = ref(false)

const { data, status, refresh } = await useFetch<GetWebhookSubscriptionsOut>(
  `${config.public.backendUrl}/webhooks/subscriptions`,
  { credentials: 'include', server: false }
)
</script>

<template>
  <UDashboardPanel id="webhooks">
    <template #header>
      <UDashboardNavbar title="Webhooks">
        <template #leading>
          <PageIcon />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div v-if="!data && status !== 'error'" class="flex flex-1 items-center justify-center">
        <AppSpinner class="size-8" />
      </div>

      <TableEmptyState
        v-else-if="!data?.items?.length"
        :loading="false"
        icon="i-lucide-webhook"
        message="Nenhum webhook cadastrado"
        button-label="Webhook"
        @create="() => { createModalOpen = true }"
      />

      <div v-else class="space-y-4">
        <div class="flex flex-col items-start gap-3 sm:flex-row sm:items-start sm:justify-between sm:gap-4">
          <div>
            <p class="text-sm text-muted mt-0.5">Visualize e gerencie os webhooks da sua instituição.</p>
          </div>
          <UButton icon="i-lucide-plus" label="Webhook" class="shrink-0" @click="() => { createModalOpen = true }" />
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-4">
          <NuxtLink
            v-for="webhook in data.items"
            :key="webhook.id"
            :to="`/webhooks/${webhook.id}`"
            class="rounded-xl bg-elevated flex flex-col overflow-hidden hover:shadow-md hover:ring hover:ring-primary/50 transition-all duration-200 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          >
            <div class="px-6 pt-5 pb-5">
              <div class="flex items-center justify-between gap-2">
                <p class="font-bold text-lg text-highlighted truncate">{{ webhook.name }}</p>
                <UBadge
                  :color="webhook.isActive ? 'success' : 'neutral'"
                  variant="subtle"
                  size="md"
                  class="shrink-0"
                  :label="webhook.isActive ? 'Ativo' : 'Inativo'"
                />
              </div>
              <div class="flex items-center gap-1.5 mt-1.5">
                <UIcon name="i-lucide-link" class="size-4 text-muted shrink-0" />
                <p class="text-sm text-muted truncate">{{ webhook.url }}</p>
              </div>
            </div>

            <div class="flex flex-wrap gap-2 px-6 pb-6">
              <UBadge
                v-for="event in webhook.events"
                :key="event"
                color="neutral"
                variant="outline"
                size="md"
                :label="webhookEventLabels[event] ?? event"
              />
            </div>
          </NuxtLink>
        </div>
      </div>
    </template>
  </UDashboardPanel>

  <WebhooksCreateModal v-model:open="createModalOpen" @created="refresh()" />
</template>
