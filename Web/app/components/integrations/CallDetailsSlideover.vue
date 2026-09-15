<script setup lang="ts">
import type { GetWebhookCallOut } from '~/types/webhooks'

const open = defineModel<boolean>('open', { default: false })
const props = defineProps<{ callId: number | null }>()
const emit = defineEmits<{ changed: [] }>()

const config = useRuntimeConfig()
const toast = useToast()
const { can } = usePolicy()
const canRetry = can('RetryWebhookCall')

const call = ref<GetWebhookCallOut | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)
const retrying = ref(false)
const polling = ref(false)

const POLL_INTERVAL_MS = 2000
const POLL_MAX_ATTEMPTS = 10
let pollTimer: ReturnType<typeof setTimeout> | undefined
let pollRun = 0

const showRetry = computed(() =>
  canRetry.value
  && !polling.value
  && call.value?.status === 'Error'
  && call.value.attempts[0]?.status === 'Error',
)

function isInProgress(value: GetWebhookCallOut | null) {
  return value?.status === 'Pending' || value?.status === 'Processing'
}

function fetchCall(callId: number) {
  return $fetch<GetWebhookCallOut>(
    `${config.public.backendUrl}/webhooks/calls/${callId}`,
    { credentials: 'include' },
  )
}

function stopPolling() {
  clearTimeout(pollTimer)
  pollRun++
  polling.value = false
}

function startPolling(callId: number) {
  stopPolling()
  const run = pollRun
  let remaining = POLL_MAX_ATTEMPTS
  polling.value = true

  const tick = async () => {
    const updated = await fetchCall(callId).catch(() => null)
    if (run !== pollRun) return
    if (updated) call.value = updated
    remaining--

    if (isInProgress(call.value) && remaining > 0) {
      pollTimer = setTimeout(tick, POLL_INTERVAL_MS)
      return
    }

    polling.value = false
    emit('changed')
  }

  pollTimer = setTimeout(tick, POLL_INTERVAL_MS)
}

async function load(callId: number) {
  loading.value = true
  error.value = null
  call.value = null
  try {
    call.value = await fetchCall(callId)
  } catch (err: unknown) {
    call.value = null
    error.value = (err as { data?: { message?: string } })?.data?.message
      ?? 'Erro ao carregar os detalhes da chamada.'
  } finally {
    loading.value = false
  }
}

async function retry() {
  if (!call.value) return
  const callId = call.value.id
  retrying.value = true
  try {
    await $fetch(`${config.public.backendUrl}/webhooks/calls/${callId}/retry`, {
      method: 'POST',
      credentials: 'include',
    })
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao reprocessar a chamada.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
    retrying.value = false
    return
  }

  toast.add({ title: 'Reprocessamento solicitado', color: 'success' })
  emit('changed')

  const updated = await fetchCall(callId).catch(() => null)
  retrying.value = false
  if (!open.value || props.callId !== callId) return

  if (updated) call.value = updated
  if (!updated || isInProgress(updated)) startPolling(callId)
}

watch([open, () => props.callId], ([isOpen, callId]) => {
  stopPolling()
  if (!isOpen || !callId) return
  if (call.value?.id === callId && !isInProgress(call.value)) return
  load(callId)
}, { immediate: true })

onBeforeUnmount(stopPolling)

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
}

function statusCodeColor(statusCode: number) {
  if (statusCode >= 200 && statusCode < 300) return 'success'
  if (statusCode >= 400 && statusCode < 500) return 'warning'
  return 'error'
}
</script>

<template>
  <USlideover
    v-model:open="open"
    title="Detalhes da chamada"
    :ui="{ content: 'max-w-xl' }"
  >
    <template #header>
      <div class="flex items-center justify-between w-full gap-3">
        <p class="text-base font-semibold text-highlighted">Detalhes da chamada</p>
        <UButton
          variant="ghost"
          color="neutral"
          icon="i-lucide-x"
          @click="() => { open = false }"
        />
      </div>
    </template>

    <template #body>
      <div v-if="loading" class="flex items-center justify-center py-12">
        <AppSpinner class="size-6 text-muted" />
      </div>

      <div v-else-if="error" class="flex flex-col items-center justify-center gap-3 py-12 text-center">
        <UIcon name="i-lucide-triangle-alert" class="size-8 text-muted" />
        <p class="text-sm text-muted">{{ error }}</p>
        <UButton
          label="Tentar novamente"
          color="neutral"
          variant="subtle"
          @click="() => { if (callId) load(callId) }"
        />
      </div>

      <div v-else-if="call" class="flex flex-col gap-6">
        <section class="flex flex-col gap-3">
          <h3 class="text-sm font-semibold text-highlighted">Resumo</h3>
          <dl class="grid grid-cols-2 gap-x-4 gap-y-3 text-sm">
            <div class="flex flex-col gap-1">
              <dt class="text-muted">Evento</dt>
              <dd class="text-highlighted">{{ webhookEventLabels[call.eventType] ?? call.eventType }}</dd>
            </div>
            <div class="flex flex-col gap-1">
              <dt class="text-muted">Status</dt>
              <dd>
                <UBadge variant="subtle" :color="webhookCallStatusColors[call.status] ?? 'neutral'">
                  {{ webhookCallStatusLabels[call.status] ?? call.status }}
                </UBadge>
              </dd>
            </div>
            <div class="flex flex-col gap-1">
              <dt class="text-muted">Tentativas</dt>
              <dd class="text-highlighted">{{ call.attemptsCount }}</dd>
            </div>
            <div class="flex flex-col gap-1">
              <dt class="text-muted">Criada em</dt>
              <dd class="text-highlighted">{{ formatDateTime(call.createdAt) }}</dd>
            </div>
            <div class="flex flex-col gap-1 col-span-2">
              <dt class="text-muted">Id do evento</dt>
              <dd class="text-highlighted font-mono text-xs break-all">{{ call.uid }}</dd>
            </div>
          </dl>
        </section>

        <section class="flex flex-col gap-3">
          <h3 class="text-sm font-semibold text-highlighted">Requisição</h3>

          <div class="flex items-center gap-2 text-sm">
            <UBadge variant="subtle" color="neutral">{{ call.request.method }}</UBadge>
            <span class="text-highlighted break-all">{{ call.request.url }}</span>
          </div>

          <div class="flex flex-col gap-2">
            <p class="text-xs font-medium text-muted uppercase">Headers</p>
            <div
              v-for="(value, key) in call.request.headers"
              :key="key"
              class="flex flex-col sm:flex-row sm:items-baseline gap-1 sm:gap-2 text-sm"
            >
              <span class="text-muted shrink-0">{{ key }}:</span>
              <span class="text-highlighted break-all">{{ value }}</span>
            </div>
          </div>

          <div class="flex flex-col gap-2">
            <p class="text-xs font-medium text-muted uppercase">Body</p>
            <pre class="text-xs bg-elevated/50 rounded-lg p-3 overflow-x-auto whitespace-pre">{{ formatWebhookJson(call.request.body) }}</pre>
          </div>
        </section>

        <section class="flex flex-col gap-3">
          <div class="flex items-center justify-between gap-3">
            <h3 class="text-sm font-semibold text-highlighted">Tentativas</h3>
            <span v-if="polling" class="flex items-center gap-2 text-xs text-muted">
              <AppSpinner class="size-4" />
              Aguardando nova tentativa
            </span>
            <UButton
              v-if="showRetry"
              label="Reprocessar"
              icon="i-lucide-refresh-cw"
              size="xs"
              color="neutral"
              variant="subtle"
              :loading="retrying"
              @click="retry"
            />
          </div>

          <p v-if="!call.attempts.length" class="text-sm text-muted">
            Nenhuma tentativa realizada até o momento.
          </p>

          <div
            v-for="attempt in call.attempts"
            :key="attempt.id"
            class="flex flex-col gap-3 border border-default rounded-lg p-3"
          >
            <div class="flex flex-wrap items-center gap-2">
              <UBadge variant="subtle" :color="webhookCallAttemptStatusColors[attempt.status] ?? 'neutral'">
                {{ webhookCallAttemptStatusLabels[attempt.status] ?? attempt.status }}
              </UBadge>
              <UBadge variant="subtle" :color="statusCodeColor(attempt.statusCode)">
                {{ attempt.statusCode }}
              </UBadge>
              <span class="text-sm text-muted">{{ formatWebhookDuration(attempt.durationMs) }}</span>
              <span class="text-sm text-muted ml-auto">{{ formatDateTime(attempt.createdAt) }}</span>
            </div>

            <div class="flex flex-col gap-2">
              <p class="text-xs font-medium text-muted uppercase">Response</p>
              <pre class="text-xs bg-elevated/50 rounded-lg p-3 overflow-x-auto whitespace-pre">{{ formatWebhookJson(attempt.response) }}</pre>
            </div>
          </div>
        </section>
      </div>
    </template>
  </USlideover>
</template>
