<script setup lang="ts">
import type { CreateClassActivityWorkFileOut } from '~/types/classes'

const props = defineProps<{ activityId: number | string, content: string | null, editable: boolean }>()
const emit = defineEmits<{ delivered: [] }>()

const config = useRuntimeConfig()
const toast = useToast()

const maxLength = 10000

const editing = ref(false)
const saving = ref(false)
const uploading = ref(0)
const work = ref('')

const saved = computed(() => props.content ?? '')
const hasWork = computed(() => saved.value.trim().length > 0)
const empty = computed(() => work.value.trim().length === 0)
const dirty = computed(() => work.value.trim() !== saved.value.trim())
const tooLong = computed(() => work.value.length > maxLength)

function startEditing() {
  work.value = saved.value
  editing.value = true
}

function cancelEditing() {
  editing.value = false
  work.value = ''
}

async function uploadFile(file: File) {
  const { uploadUrl, publicUrl } = await $fetch<CreateClassActivityWorkFileOut>(
    `${config.public.backendUrl}/students/activities/${props.activityId}/works/files`,
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

async function save() {
  saving.value = true
  try {
    await $fetch(`${config.public.backendUrl}/students/activities/${props.activityId}/works`, {
      method: 'POST',
      body: { content: work.value },
      credentials: 'include',
    })

    toast.add({ title: 'Entrega feita com sucesso', color: 'success' })
    cancelEditing()
    emit('delivered')
  } catch (err: unknown) {
    toast.add({
      title: 'Não foi possível salvar a entrega',
      description: (err as { data?: { message?: string } })?.data?.message ?? 'Tente novamente.',
      color: 'error',
    })
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="flex flex-col gap-4">
    <div v-if="editing || (hasWork && editable)" class="flex justify-end gap-2">
      <template v-if="editing">
        <UButton
          label="Cancelar"
          color="neutral"
          variant="subtle"
          :disabled="saving"
          @click="() => { cancelEditing() }"
        />
        <UButton
          :label="uploading > 0 ? 'Enviando arquivo...' : 'Entregar'"
          :loading="saving || uploading > 0"
          :disabled="empty || !dirty || tooLong || uploading > 0"
          @click="() => { save() }"
        />
      </template>
      <UButton
        v-else
        icon="i-lucide-pencil"
        label="Editar"
        color="neutral"
        variant="subtle"
        size="sm"
        @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); startEditing() }"
      />
    </div>

    <template v-if="!editing">
      <div v-if="hasWork" class="rounded-lg border border-default p-3">
        <MarkdownContent :value="saved" />
      </div>

      <TableEmptyState
        v-else
        :loading="false"
        icon="i-lucide-file-up"
        message="Você ainda não entregou esta atividade"
        :button-label="editable ? 'Fazer entrega' : undefined"
        @create="() => { startEditing() }"
      />
    </template>

    <template v-else>
      <RichEditor
        v-model="work"
        v-model:uploading="uploading"
        :upload-image="uploadFile"
        :upload-pdf="uploadFile"
        placeholder="Escreva sua entrega. Você pode anexar imagens e PDFs."
        autofocus
      />

      <p v-if="tooLong" class="text-xs text-error">
        A entrega passou de {{ maxLength }} caracteres ({{ work.length }}).
      </p>
    </template>
  </div>
</template>
