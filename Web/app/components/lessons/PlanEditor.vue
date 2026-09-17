<script setup lang="ts">
import type { CreateLessonPlanImageOut } from '~/types/classes'

const props = defineProps<{ lessonId: number, plannedContent: string | null }>()
const emit = defineEmits<{ updated: [] }>()

const config = useRuntimeConfig()
const toast = useToast()

const maxLength = 10000

const editing = ref(false)
const saving = ref(false)
const uploading = ref(0)
const plan = ref('')

const saved = computed(() => props.plannedContent ?? '')
const hasPlan = computed(() => saved.value.trim().length > 0)
const dirty = computed(() => plan.value.trim() !== saved.value.trim())
const tooLong = computed(() => plan.value.length > maxLength)

function startEditing() {
  plan.value = saved.value
  editing.value = true
}

function cancelEditing() {
  editing.value = false
  plan.value = ''
}

async function uploadImage(file: File) {
  const { uploadUrl, publicUrl } = await $fetch<CreateLessonPlanImageOut>(
    `${config.public.backendUrl}/teachers/lessons/${props.lessonId}/plan/images`,
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
    await $fetch(`${config.public.backendUrl}/teachers/lessons/${props.lessonId}/plan`, {
      method: 'PUT',
      body: { plannedContent: plan.value },
      credentials: 'include',
    })

    toast.add({ title: 'Planejamento salvo', color: 'success' })
    cancelEditing()
    emit('updated')
  } catch (err: unknown) {
    toast.add({
      title: 'Não foi possível salvar o planejamento',
      description: (err as { data?: { message?: string } })?.data?.message ?? 'Tente novamente.',
      color: 'error',
    })
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <p class="text-sm text-muted">
        O que será abordado na aula, visível para os alunos da turma.
      </p>

      <div class="flex shrink-0 items-center gap-2">
        <template v-if="editing">
          <UButton
            label="Cancelar"
            color="neutral"
            variant="subtle"
            :disabled="saving"
            @click="() => { cancelEditing() }"
          />
          <UButton
            :label="uploading > 0 ? 'Enviando imagem...' : 'Salvar'"
            :loading="saving || uploading > 0"
            :disabled="!dirty || tooLong || uploading > 0"
            @click="() => { save() }"
          />
        </template>
        <UButton
          v-else-if="hasPlan"
          icon="i-lucide-pencil"
          label="Editar"
          color="neutral"
          variant="subtle"
          @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); startEditing() }"
        />
      </div>
    </div>

    <template v-if="!editing">
      <div v-if="hasPlan" class="rounded-lg border border-default p-3">
        <MarkdownContent :value="saved" />
      </div>

      <TableEmptyState
        v-else
        :loading="false"
        icon="i-lucide-notebook-pen"
        message="Nenhum planejamento para esta aula"
        button-label="Planejar aula"
        @create="() => { startEditing() }"
      />
    </template>

    <template v-else>
      <RichEditor
        v-model="plan"
        v-model:uploading="uploading"
        :upload-image="uploadImage"
        placeholder="Descreva o que será abordado na aula."
        autofocus
      />

      <p v-if="tooLong" class="text-xs text-error">
        O planejamento passou de {{ maxLength }} caracteres ({{ plan.length }}).
      </p>
    </template>
  </section>
</template>
