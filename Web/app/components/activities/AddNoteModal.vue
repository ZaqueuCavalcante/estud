<script setup lang="ts">
import * as z from 'zod'
import type { FormSubmitEvent } from '@nuxt/ui'
import type { TeacherActivityWorkItem } from '~/types/classes'

const props = defineProps<{ activityId: number | string, work: TeacherActivityWorkItem | null }>()

const open = defineModel<boolean>('open', { default: false })
const emit = defineEmits<{ saved: [] }>()

const isMobile = useIsMobile()
const config = useRuntimeConfig()
const toast = useToast()
const loading = ref(false)

const isEditing = computed(() => props.work?.status === 'Finalized')

const noteDisplay = ref('')

const ALLOWED_KEYS = new Set(['Backspace', 'Delete', 'Tab', 'ArrowLeft', 'ArrowRight', 'Home', 'End'])

function onNoteKeydown(e: KeyboardEvent) {
  if (ALLOWED_KEYS.has(e.key)) return
  if (!/^[\d,.]$/.test(e.key)) e.preventDefault()
}

function onNoteInput(e: Event) {
  const input = e.target as HTMLInputElement
  const [first, ...rest] = input.value.replace(',', '.').replace(/[^\d.]/g, '').split('.')
  const integer = (first ?? '').slice(0, 2)
  const decimals = rest.join('').slice(0, 2)

  const sanitized = rest.length ? `${integer}.${decimals}` : integer
  noteDisplay.value = sanitized
  input.value = sanitized
  formState.note = sanitized ? Number(sanitized) : undefined
}

const schema = z.object({
  note: z.coerce.number({ error: 'Campo obrigatório' }).min(0, 'Mínimo 0').max(10, 'Máximo 10'),
})

type Schema = z.output<typeof schema>

const formState = reactive<Partial<Schema>>({
  note: undefined,
})

watch(open, (val) => {
  if (!val) {
    noteDisplay.value = ''
    formState.note = undefined
    return
  }

  noteDisplay.value = isEditing.value ? String(props.work?.value ?? '') : ''
  formState.note = isEditing.value ? props.work?.value : undefined
})

async function onSubmit(event: FormSubmitEvent<Schema>) {
  if (!props.work) return

  loading.value = true
  try {
    await $fetch(`${config.public.backendUrl}/teachers/activities/${props.activityId}/works/${props.work.id}/note`, {
      method: 'PUT',
      body: event.data,
      credentials: 'include',
    })
    toast.add({ title: 'Nota salva com sucesso', color: 'success' })
    open.value = false
    emit('saved')
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao salvar a nota.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <UModal
    v-model:open="open"
    :title="isEditing ? 'Editar nota' : 'Adicionar nota'"
    :fullscreen="isMobile"
    :description="`Nota da entrega de ${props.work?.student ?? 'aluno'}, entre 0 e 10.`"
  >
    <template #body>
      <UForm
        :schema="schema"
        :state="formState"
        class="space-y-4"
        @submit="onSubmit"
      >
        <UFormField label="Nota" name="note" required>
          <UInput
            :model-value="noteDisplay"
            type="text"
            inputmode="decimal"
            class="w-full"
            placeholder="Ex: 8,5"
            @keydown="onNoteKeydown"
            @input="onNoteInput"
          />
        </UFormField>

        <div class="flex justify-end gap-2 pt-2">
          <UButton label="Cancelar" color="neutral" variant="subtle" :disabled="loading" @click="() => { open = false }" />
          <UButton label="Salvar" type="submit" :loading="loading" />
        </div>
      </UForm>
    </template>
  </UModal>
</template>
