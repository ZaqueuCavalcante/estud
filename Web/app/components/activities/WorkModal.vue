<script setup lang="ts">
import type { TeacherActivityWorkItem } from '~/types/classes'

const props = defineProps<{ activityId: number | string, work: TeacherActivityWorkItem | null }>()

const open = defineModel<boolean>('open', { default: false })
const emit = defineEmits<{ saved: [] }>()

const config = useRuntimeConfig()
const toast = useToast()
const isMobile = useIsMobile()

const maxLength = 10000

const saving = ref(false)
const comment = ref('')
const noteDisplay = ref('')
const note = ref<number | undefined>(undefined)
const status = ref<string | undefined>(undefined)

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
  note.value = sanitized ? Number(sanitized) : undefined
}

watch(open, (value) => {
  comment.value = ''
  noteDisplay.value = value ? String(props.work?.value ?? '') : ''
  note.value = value ? props.work?.value : undefined
  status.value = value ? props.work?.status : undefined
})

const tooLong = computed(() => comment.value.length > maxLength)
const invalidNote = computed(() => note.value !== undefined && (note.value < 0 || note.value > 10))

const changed = computed(() =>
  comment.value.trim().length > 0
  || note.value !== props.work?.value
  || status.value !== props.work?.status,
)

async function save() {
  if (!props.work) return

  saving.value = true
  try {
    await $fetch(`${config.public.backendUrl}/teachers/activities/${props.activityId}/works/${props.work.id}/entries`, {
      method: 'POST',
      body: {
        content: comment.value.trim() ? comment.value : null,
        note: note.value,
        status: status.value,
      },
      credentials: 'include',
    })

    toast.add({ title: 'Entrega atualizada', color: 'success' })
    open.value = false
    emit('saved')
  } catch (err: unknown) {
    toast.add({
      title: 'Não foi possível atualizar a entrega',
      description: (err as { data?: { message?: string } })?.data?.message ?? 'Tente novamente.',
      color: 'error',
    })
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <UModal
    v-model:open="open"
    :title="work ? `Entrega de ${work.student}` : 'Entrega'"
    description="Comente, altere a nota ou o status da entrega."
    :fullscreen="isMobile"
    :ui="{ content: 'sm:max-w-3xl' }"
  >
    <template #body>
      <div class="flex flex-col gap-6">
        <ActivitiesWorkTimeline
          :entries="work?.entries ?? []"
          empty-message="Nenhuma movimentação nesta entrega"
        />

        <USeparator />

        <div class="flex flex-col gap-4">
          <UFormField label="Comentário" name="comment">
            <RichEditor
              v-model="comment"
              placeholder="Escreva um comentário para o aluno."
            />
          </UFormField>

          <p v-if="tooLong" class="text-xs text-error">
            O comentário passou de {{ maxLength }} caracteres ({{ comment.length }}).
          </p>

          <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <UFormField label="Nota" name="note" hint="De 0 a 10">
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

            <UFormField label="Status" name="status">
              <USelect
                v-model="status"
                :items="classActivityWorkStatusOptions"
                value-key="value"
                class="w-full"
              />
            </UFormField>
          </div>

          <p v-if="invalidNote" class="text-xs text-error">
            A nota deve ficar entre 0 e 10.
          </p>

          <div class="flex justify-end gap-2">
            <UButton
              label="Cancelar"
              color="neutral"
              variant="subtle"
              :disabled="saving"
              @click="() => { open = false }"
            />
            <UButton
              label="Salvar"
              :loading="saving"
              :disabled="!changed || tooLong || invalidNote"
              @click="() => { save() }"
            />
          </div>
        </div>
      </div>
    </template>
  </UModal>
</template>
