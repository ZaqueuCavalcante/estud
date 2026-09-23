<script setup lang="ts">
import * as z from 'zod'
import { DateFormatter, getLocalTimeZone, parseDate, type CalendarDate } from '@internationalized/date'
import type { FormSubmitEvent } from '@nuxt/ui'
import type { GetInstitutionNoteTypesOut } from '~/types/configs'
import type { CreateClassActivityFileOut, GetTeacherClassActivityOut } from '~/types/classes'

const props = defineProps<{ classId: string, activityId?: string }>()

const config = useRuntimeConfig()
const toast = useToast()
const loading = ref(false)
const uploading = ref(0)

const isEdit = !!props.activityId
const activityUrl = `/classes/${props.classId}/activities/${props.activityId}`

const breadcrumb = computed(() => [
  { label: 'Turmas', icon: 'i-lucide-presentation' },
  { label: 'Detalhes', to: `/classes/${props.classId}` },
  ...(isEdit
    ? [{ label: 'Atividade', to: activityUrl }, { label: 'Editar' }]
    : [{ label: 'Nova atividade' }]),
])

const df = new DateFormatter('pt-BR', { dateStyle: 'medium' })

const toDateString = (d: CalendarDate) =>
  `${d.year}-${String(d.month).padStart(2, '0')}-${String(d.day).padStart(2, '0')}`

const dueDate = ref<CalendarDate | undefined>()
const dueDateOpen = ref(false)

const descriptionMaxLength = 10000

// ── Options ───────────────────────────────────────────────────
const { data: noteTypesData } = await useFetch<GetInstitutionNoteTypesOut>(
  `${config.public.backendUrl}/institutions/note-types`,
  { credentials: 'include', server: false },
)

const noteOptions = computed(() =>
  (noteTypesData.value?.noteTypes ?? []).map(note => ({ label: note, value: note })),
)

const defaultNote = computed(() => noteOptions.value[0]?.value)

const typeOptions = [
  { label: 'Prova', value: 'Exam' },
  { label: 'Projeto', value: 'Project' },
  { label: 'Trabalho', value: 'Work' },
  { label: 'Apresentação', value: 'Presentation' },
]

function buildHourOptions() {
  const opts = []
  for (let h = 7; h <= 23; h++) {
    for (let m = 0; m < 60; m += 15) {
      const hh = h.toString().padStart(2, '0')
      const mm = m.toString().padStart(2, '0')
      opts.push({ label: `${hh}:${mm}`, value: `H${hh}_${mm}` })
    }
  }
  return opts
}
const hourOptions = buildHourOptions()

// ── Weight input ──────────────────────────────────────────────
const weightDisplay = ref('')

const ALLOWED_KEYS = new Set(['Backspace', 'Delete', 'Tab', 'ArrowLeft', 'ArrowRight', 'Home', 'End'])

function onWeightKeydown(e: KeyboardEvent) {
  if (ALLOWED_KEYS.has(e.key)) return
  if (!/^\d$/.test(e.key)) { e.preventDefault(); return }
  const input = e.target as HTMLInputElement
  const hasSelection = input.selectionStart !== input.selectionEnd
  const digitCount = input.value.replace(/\D/g, '').length
  if (!hasSelection && digitCount >= 3) e.preventDefault()
}

function onWeightInput(e: Event) {
  const input = e.target as HTMLInputElement
  const digits = input.value.replace(/\D/g, '').slice(0, 3)
  weightDisplay.value = digits
  input.value = digits
  formState.weight = digits ? Number(digits) : undefined
}

// ── Form schema ───────────────────────────────────────────────
const schema = z.object({
  note: z.string({ error: 'Campo obrigatório' }).min(1, 'Campo obrigatório'),
  title: z.string().min(1, 'Título obrigatório').max(100, 'Máximo 100 caracteres'),
  description: z.string({ error: 'Descrição obrigatória' })
    .trim()
    .min(1, 'Descrição obrigatória')
    .max(descriptionMaxLength, `Máximo ${descriptionMaxLength} caracteres`),
  type: z.string({ error: 'Campo obrigatório' }).min(1, 'Campo obrigatório'),
  weight: z.coerce.number({ error: 'Campo obrigatório' }).int().min(0, 'Mínimo 0').max(100, 'Máximo 100'),
  dueDate: z.string({ error: 'Campo obrigatório' }).min(1, 'Data limite obrigatória'),
  dueHour: z.string({ error: 'Campo obrigatório' }).min(1, 'Hora limite obrigatória'),
})

type Schema = z.output<typeof schema>

const formState = reactive<Partial<Schema>>({
  note: undefined,
  title: '',
  description: '',
  type: undefined,
  weight: undefined,
  dueDate: '',
  dueHour: undefined,
})

watch(dueDate, (val) => {
  formState.dueDate = val ? toDateString(val) : ''
  if (val) dueDateOpen.value = false
})

watch(defaultNote, (note) => {
  if (!formState.note) formState.note = note
}, { immediate: true })

const { data: activity, status: activityStatus } = await useFetch<GetTeacherClassActivityOut>(
  `${config.public.backendUrl}/teachers/classes/${props.classId}/activities/${props.activityId}`,
  { credentials: 'include', server: false, immediate: isEdit },
)

watch(activity, (value) => {
  if (!value) return

  formState.note = value.note
  formState.title = value.title
  formState.description = value.description
  formState.type = value.type
  formState.weight = value.weight
  formState.dueHour = value.dueHour
  weightDisplay.value = String(value.weight)
  dueDate.value = parseDate(value.dueDate)
}, { immediate: true })

async function uploadFile(file: File) {
  const { uploadUrl, publicUrl } = await $fetch<CreateClassActivityFileOut>(
    `${config.public.backendUrl}/teachers/classes/${props.classId}/activities/files`,
    {
      method: 'POST',
      body: { contentType: file.type, sizeInBytes: file.size },
      credentials: 'include',
    },
  )

  await $fetch(uploadUrl, {
    method: 'PUT',
    body: file,
    headers: { 'Content-Type': file.type },
  })

  return publicUrl
}

const notifyOpen = ref(false)
const notifyStudents = ref<boolean | null>(null)
const pendingData = ref<Schema | null>(null)

function errorMessage(err: unknown, fallback: string) {
  return (err as { data?: { message?: string } })?.data?.message ?? fallback
}

async function onSubmit(event: FormSubmitEvent<Schema>) {
  if (uploading.value > 0) return

  if (isEdit) {
    pendingData.value = event.data
    notifyOpen.value = true
    return
  }

  loading.value = true
  try {
    const created = await $fetch<{ id: number }>(`${config.public.backendUrl}/teachers/classes/${props.classId}/activities`, {
      method: 'POST',
      body: event.data,
      credentials: 'include',
    })
    toast.add({ title: 'Atividade criada com sucesso', color: 'success' })
    await navigateTo(`/classes/${props.classId}/activities/${created.id}`, { replace: true })
  } catch (err: unknown) {
    toast.add({ title: 'Erro', description: errorMessage(err, 'Erro ao criar atividade.'), color: 'error' })
  } finally {
    loading.value = false
  }
}

async function onUpdate(notify: boolean) {
  if (!pendingData.value) return

  loading.value = true
  notifyStudents.value = notify
  try {
    await $fetch(`${config.public.backendUrl}/teachers/classes/${props.classId}/activities/${props.activityId}`, {
      method: 'PUT',
      body: { ...pendingData.value, notifyStudents: notify },
      credentials: 'include',
    })
    notifyOpen.value = false
    toast.add({ title: 'Atividade atualizada com sucesso', color: 'success' })
    await navigateTo(activityUrl, { replace: true })
  } catch (err: unknown) {
    notifyOpen.value = false
    toast.add({ title: 'Erro', description: errorMessage(err, 'Erro ao atualizar atividade.'), color: 'error' })
  } finally {
    loading.value = false
    notifyStudents.value = null
  }
}
</script>

<template>
  <UDashboardPanel id="activity-create">
    <template #header>
      <UDashboardNavbar>
        <template #title>
          <UBreadcrumb :items="breadcrumb" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div v-if="isEdit && activityStatus === 'error'" class="flex flex-col items-center gap-4 py-12">
        <UIcon name="i-lucide-triangle-alert" class="size-16 text-muted" />
        <p class="text-muted text-sm">
          Atividade não encontrada
        </p>
        <UButton icon="i-lucide-arrow-left" label="Voltar" :to="`/classes/${props.classId}`" />
      </div>

      <div v-else-if="isEdit && activityStatus !== 'success'" class="flex flex-1 items-center justify-center">
        <AppSpinner class="size-8" />
      </div>

      <div v-else class="flex flex-col gap-6 py-2">
        <div class="flex flex-col gap-1">
          <h1 class="text-2xl font-semibold tracking-tight text-highlighted">
            {{ isEdit ? 'Editar atividade' : 'Nova atividade' }}
          </h1>
          <p class="text-sm text-muted">
            {{ isEdit ? 'Altere os dados da atividade da turma.' : 'Preencha os dados para cadastrar uma nova atividade da turma.' }}
          </p>
        </div>

        <UForm
          :schema="schema"
          :state="formState"
          class="flex flex-col gap-4"
          @submit="onSubmit"
        >
          <UFormField label="Título" name="title" required>
            <UInput v-model="formState.title" class="w-full" placeholder="Ex: Modelagem de Banco de Dados" />
          </UFormField>

          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
            <UFormField label="Nota" name="note" required>
              <USelect
                v-model="formState.note"
                :items="noteOptions"
                value-key="value"
                class="w-full"
                placeholder="Selecione"
              />
            </UFormField>

            <UFormField label="Tipo" name="type" required>
              <USelect
                v-model="formState.type"
                :items="typeOptions"
                value-key="value"
                class="w-full"
                placeholder="Selecione"
              />
            </UFormField>

            <UFormField label="Peso" name="weight" required>
              <UInput
                :model-value="weightDisplay"
                type="text"
                inputmode="numeric"
                class="w-full"
                placeholder="Ex: 40"
                @keydown="onWeightKeydown"
                @input="onWeightInput"
              />
            </UFormField>

            <UFormField label="Data limite" name="dueDate" required>
              <UPopover v-model:open="dueDateOpen" :content="{ align: 'start' }" :modal="true" class="w-full">
                <UButton color="neutral" variant="outline" class="w-full">
                  <div class="flex items-center w-full gap-2">
                    <UIcon name="i-lucide-calendar" class="size-4 shrink-0" />
                    <span class="flex-1 text-left truncate">{{ dueDate ? df.format(dueDate.toDate(getLocalTimeZone())) : 'Selecionar' }}</span>
                    <UIcon :name="dueDateOpen ? 'i-lucide-chevron-up' : 'i-lucide-chevron-down'" class="size-4 shrink-0" />
                  </div>
                </UButton>
                <template #content>
                  <UCalendar v-model="dueDate" class="p-2" />
                </template>
              </UPopover>
            </UFormField>

            <UFormField label="Hora limite" name="dueHour" required>
              <USelect
                v-model="formState.dueHour"
                :items="hourOptions"
                value-key="value"
                class="w-full"
                placeholder="Selecione"
              />
            </UFormField>
          </div>

          <UFormField label="Descrição" name="description" required>
            <RichEditor
              v-model="formState.description"
              v-model:uploading="uploading"
              :upload-image="uploadFile"
              :upload-pdf="uploadFile"
              placeholder="Descreva o que o aluno deve entregar."
            />
          </UFormField>

          <div class="flex justify-end gap-2 pt-2">
            <UButton
              label="Cancelar"
              color="neutral"
              variant="subtle"
              :disabled="loading"
              :to="isEdit ? activityUrl : `/classes/${props.classId}`"
            />
            <UButton
              :label="uploading > 0 ? 'Enviando arquivo...' : 'Salvar'"
              type="submit"
              :loading="loading || uploading > 0"
              :disabled="uploading > 0"
            />
          </div>
        </UForm>
      </div>
    </template>
  </UDashboardPanel>

  <UModal
    v-model:open="notifyOpen"
    title="Notificar alunos"
    description="Notificar os alunos da turma sobre essa alteração na atividade?"
    :dismissible="!loading"
  >
    <template #footer>
      <div class="flex justify-end gap-2 w-full">
        <UButton
          label="Não notificar"
          color="neutral"
          variant="subtle"
          :loading="notifyStudents === false"
          :disabled="loading"
          @click="() => { onUpdate(false) }"
        />
        <UButton
          icon="i-lucide-bell"
          label="Notificar alunos"
          :loading="notifyStudents === true"
          :disabled="loading"
          @click="() => { onUpdate(true) }"
        />
      </div>
    </template>
  </UModal>
</template>
