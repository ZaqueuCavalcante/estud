<script setup lang="ts">
import * as z from 'zod'
import type { FormSubmitEvent } from '@nuxt/ui'
import type { GetWebhookSubscriptionOut } from '~/types/webhooks'

const open = defineModel<boolean>('open', { default: false })
const props = defineProps<{ subscription: GetWebhookSubscriptionOut | null }>()
const emit = defineEmits<{ updated: [] }>()

const isMobile = useIsMobile()
const config = useRuntimeConfig()
const toast = useToast()
const loading = ref(false)

const backoffStrategyDescriptions: Record<string, string> = {
  None: 'Retenta na hora',
  Fixed: 'Mesmo intervalo sempre',
  Linear: 'Cresce a cada tentativa',
  Exponential: 'Dobra a cada tentativa',
}

const backoffStrategyOptions = Object.entries(webhookBackoffStrategyLabels).map(([value, label]) => ({
  label,
  value,
  icon: webhookBackoffStrategyIcons[value]!,
  description: backoffStrategyDescriptions[value],
}))

const schema = z.object({
  maxRetries: z.number({ error: 'Campo obrigatório' }).int('Deve ser um número inteiro').min(0, 'Mínimo 0').max(5, 'Máximo 5'),
  baseDelaySeconds: z.number({ error: 'Campo obrigatório' }).int('Deve ser um número inteiro').min(0, 'Mínimo 0').max(30, 'Máximo 30'),
  backoffStrategy: z.string({ error: 'Campo obrigatório' }).min(1, 'Campo obrigatório'),
})

type Schema = z.output<typeof schema>

const formState = reactive<Partial<Schema>>({
  maxRetries: 0,
  baseDelaySeconds: 0,
  backoffStrategy: 'None',
})

watch(open, (val) => {
  if (val && props.subscription) {
    formState.maxRetries = props.subscription.maxRetries
    formState.baseDelaySeconds = props.subscription.baseDelaySeconds
    formState.backoffStrategy = props.subscription.backoffStrategy
  }
})

const retryDelays = computed(() =>
  webhookRetryDelays(formState.maxRetries ?? 0, formState.baseDelaySeconds ?? 0, formState.backoffStrategy ?? 'None'),
)

async function onSubmit(event: FormSubmitEvent<Schema>) {
  loading.value = true
  try {
    await $fetch(`${config.public.backendUrl}/webhooks/subscriptions/${props.subscription!.id}/retry-configs`, {
      method: 'PUT',
      body: event.data,
      credentials: 'include',
    })
    toast.add({ title: 'Configurações de retentativa atualizadas', color: 'success' })
    open.value = false
    emit('updated')
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao atualizar configurações de retentativa.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <UModal
    v-model:open="open"
    title="Configurações de retentativa"
    :fullscreen="isMobile"
    description="Defina como as chamadas com falha devem ser retentadas."
  >
    <template #body>
      <UForm
        :schema="schema"
        :state="formState"
        class="space-y-4"
        @submit="onSubmit"
      >
        <UFormField
          label="Máximo de retentativas"
          name="maxRetries"
          :hint="`${formState.maxRetries}`"
        >
          <USlider v-model="formState.maxRetries" class="py-2" :min="0" :max="5" />
        </UFormField>

        <UFormField
          label="Intervalo base"
          name="baseDelaySeconds"
          :hint="`${formState.baseDelaySeconds}s`"
        >
          <USlider v-model="formState.baseDelaySeconds" class="py-2" :min="0" :max="30" />
        </UFormField>

        <UFormField label="Estratégia de backoff" name="backoffStrategy">
          <div role="radiogroup" aria-label="Estratégia de backoff" class="grid grid-cols-2 gap-2">
            <button
              v-for="opt in backoffStrategyOptions"
              :key="opt.value"
              type="button"
              role="radio"
              :aria-checked="formState.backoffStrategy === opt.value"
              class="flex cursor-pointer items-start gap-2.5 rounded-lg px-3 py-2.5 text-left ring ring-inset transition-colors"
              :class="formState.backoffStrategy === opt.value
                ? 'bg-primary/10 ring-primary'
                : 'ring-default hover:bg-elevated'"
              @click="() => { formState.backoffStrategy = opt.value }"
            >
              <UIcon
                :name="opt.icon"
                class="mt-0.5 size-4 shrink-0"
                :class="formState.backoffStrategy === opt.value ? 'text-primary' : 'text-muted'"
              />
              <span class="flex flex-col gap-0.5">
                <span
                  class="text-sm font-medium"
                  :class="formState.backoffStrategy === opt.value ? 'text-primary' : 'text-highlighted'"
                >
                  {{ opt.label }}
                </span>
                <span class="text-xs text-muted">{{ opt.description }}</span>
              </span>
            </button>
          </div>
        </UFormField>

        <WebhooksRetryDelaysChart :delays="retryDelays" />

        <div class="flex justify-end gap-2 pt-2">
          <UButton
            label="Cancelar"
            color="neutral"
            variant="subtle"
            :disabled="loading"
            @click="() => { open = false }"
          />
          <UButton
            label="Salvar"
            type="submit"
            :loading="loading"
          />
        </div>
      </UForm>
    </template>
  </UModal>
</template>
