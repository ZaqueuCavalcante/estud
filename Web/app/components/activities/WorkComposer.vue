<script setup lang="ts">
import type { CreateClassActivityWorkFileOut } from '~/types/classes'

const props = defineProps<{ activityId: number | string }>()
const emit = defineEmits<{ created: [] }>()

const config = useRuntimeConfig()
const toast = useToast()

const maxLength = 10000

const writing = ref(false)
const saving = ref(false)
const uploading = ref(0)
const comment = ref('')

const empty = computed(() => comment.value.trim().length === 0)
const tooLong = computed(() => comment.value.length > maxLength)

function startWriting() {
  comment.value = ''
  writing.value = true
}

function cancelWriting() {
  writing.value = false
  comment.value = ''
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
    await $fetch(`${config.public.backendUrl}/students/activities/${props.activityId}/works/comments`, {
      method: 'POST',
      body: { content: comment.value },
      credentials: 'include',
    })

    toast.add({ title: 'Comentário enviado', color: 'success' })
    cancelWriting()
    emit('created')
  } catch (err: unknown) {
    toast.add({
      title: 'Não foi possível enviar o comentário',
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
    <template v-if="writing">
      <RichEditor
        v-model="comment"
        v-model:uploading="uploading"
        :upload-image="uploadFile"
        :upload-pdf="uploadFile"
        placeholder="Escreva seu comentário. Você pode anexar imagens e PDFs."
        autofocus
      />

      <p v-if="tooLong" class="text-xs text-error">
        O comentário passou de {{ maxLength }} caracteres ({{ comment.length }}).
      </p>

      <div class="flex justify-end gap-2">
        <UButton
          label="Cancelar"
          color="neutral"
          variant="subtle"
          :disabled="saving"
          @click="() => { cancelWriting() }"
        />
        <UButton
          :label="uploading > 0 ? 'Enviando arquivo...' : 'Comentar'"
          :loading="saving || uploading > 0"
          :disabled="empty || tooLong || uploading > 0"
          @click="() => { save() }"
        />
      </div>
    </template>

    <div v-else class="flex justify-end">
      <UButton
        icon="i-lucide-message-square-plus"
        label="Comentar"
        color="neutral"
        variant="subtle"
        size="sm"
        @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); startWriting() }"
      />
    </div>
  </div>
</template>
