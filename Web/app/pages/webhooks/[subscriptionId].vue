<script setup lang="ts">
import type { GetWebhookSubscriptionOut } from '~/types/webhooks'

const route = useRoute()
const config = useRuntimeConfig()

const subscriptionId = Number(route.params.subscriptionId)

const breadcrumb = [
  { label: 'Webhooks', to: '/webhooks', icon: 'i-lucide-webhook' },
  { label: 'Detalhes' },
]

const { data, status, error, refresh } = await useFetch<GetWebhookSubscriptionOut>(
  `${config.public.backendUrl}/webhooks/subscriptions/${subscriptionId}`,
  { credentials: 'include', server: false },
)

const editModalOpen = ref(false)
const retryConfigsModalOpen = ref(false)

const customHeaders = computed(() => Object.entries(data.value?.customHeaders ?? {}))
</script>

<template>
  <UDashboardPanel id="webhook-details">
    <template #header>
      <UDashboardNavbar>
        <template #title>
          <UBreadcrumb :items="breadcrumb" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div v-if="!data && status !== 'error'" class="flex flex-1 items-center justify-center">
        <AppSpinner class="size-8" />
      </div>

      <div v-else-if="error || !data" class="flex flex-col items-center gap-4 py-12">
        <UIcon name="i-lucide-triangle-alert" class="size-16 text-muted" />
        <p class="text-muted text-sm">
          Webhook não encontrado
        </p>
        <UButton icon="i-lucide-arrow-left" label="Voltar" to="/webhooks" />
      </div>

      <div v-else class="flex flex-col gap-10 py-2">
        <div class="flex flex-col gap-1">
          <div class="flex items-center gap-1.5">
            <h1 class="text-2xl font-semibold tracking-tight text-highlighted">
              {{ data.name }}
            </h1>
            <UTooltip text="Editar">
              <UButton
                icon="i-lucide-pencil"
                color="neutral"
                variant="ghost"
                size="xs"
                @click="(e) => { (e.currentTarget as HTMLElement).blur(); editModalOpen = true }"
              />
            </UTooltip>
          </div>
          <div class="flex flex-col gap-1 text-sm text-muted">
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-link" class="size-4 shrink-0" />
              <span class="break-all">{{ data.url }}</span>
            </span>
            <div class="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-3">
              <span class="flex items-center gap-1.5">
                <UIcon name="i-lucide-calendar" class="size-4" />
                Criado em {{ formatDateTime(data.createdAt) }}
              </span>
              <UBadge
                class="self-start"
                :label="data.isActive ? 'Ativo' : 'Inativo'"
                :color="data.isActive ? 'success' : 'neutral'"
                variant="subtle"
              />
            </div>
          </div>
        </div>

        <section class="flex flex-col gap-3">
          <h2 class="font-semibold text-highlighted">
            Eventos
          </h2>
          <div class="flex flex-wrap gap-2">
            <UBadge
              v-for="event in data.events"
              :key="event"
              :label="webhookEventLabels[event] ?? event"
              color="neutral"
              variant="subtle"
            />
          </div>
        </section>

        <section class="flex flex-col gap-3">
          <h2 class="font-semibold text-highlighted">
            Headers
          </h2>
          <div
            v-if="customHeaders.length"
            class="flex flex-col gap-2 rounded-lg bg-elevated px-4 py-3"
          >
            <div
              v-for="[key, value] in customHeaders"
              :key="key"
              class="flex flex-col sm:flex-row sm:items-baseline gap-1 sm:gap-2 text-sm"
            >
              <span class="text-muted shrink-0">{{ key }}:</span>
              <span class="text-highlighted break-all">{{ value }}</span>
            </div>
          </div>
          <div v-else class="flex items-center gap-2 text-sm text-muted">
            <UIcon name="i-lucide-list" class="size-4" />
            Nenhum header customizado
          </div>
        </section>

        <section class="flex flex-col gap-3">
          <div class="flex items-center gap-1.5">
            <h2 class="font-semibold text-highlighted">
              Retentativas
            </h2>
            <UTooltip text="Editar retentativas">
              <UButton
                icon="i-lucide-pencil"
                color="neutral"
                variant="ghost"
                size="xs"
                @click="(e) => { (e.currentTarget as HTMLElement).blur(); retryConfigsModalOpen = true }"
              />
            </UTooltip>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-2 rounded-lg bg-elevated px-4 py-3 text-sm">
            <div class="flex flex-col gap-0.5">
              <span class="text-muted">Máximo de retentativas</span>
              <span class="text-highlighted">{{ data.maxRetries }}</span>
            </div>
            <div class="flex flex-col gap-0.5">
              <span class="text-muted">Intervalo base</span>
              <span class="text-highlighted">{{ data.baseDelaySeconds }}s</span>
            </div>
            <div class="flex flex-col gap-0.5">
              <span class="text-muted">Estratégia de backoff</span>
              <span class="flex items-center gap-1.5 text-highlighted">
                <UIcon
                  v-if="webhookBackoffStrategyIcons[data.backoffStrategy]"
                  :name="webhookBackoffStrategyIcons[data.backoffStrategy]!"
                  class="size-4 text-muted"
                />
                {{ webhookBackoffStrategyLabels[data.backoffStrategy] ?? data.backoffStrategy }}
              </span>
            </div>
          </div>
        </section>

        <section class="flex flex-col gap-3">
          <h2 class="font-semibold text-highlighted">
            Chamadas
          </h2>
          <WebhooksCallsTable :subscription-id="data.id" />
        </section>

        <WebhooksEditModal
          v-model:open="editModalOpen"
          :subscription="data"
          @updated="() => { refresh() }"
        />

        <WebhooksRetryConfigsModal
          v-model:open="retryConfigsModalOpen"
          :subscription="data"
          @updated="() => { refresh() }"
        />
      </div>
    </template>
  </UDashboardPanel>
</template>
