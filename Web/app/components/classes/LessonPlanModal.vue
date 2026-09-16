<script setup lang="ts">
import * as z from 'zod'
import type { FormSubmitEvent } from '@nuxt/ui'
import type { ClassLessonItem } from '~/types/classes'

const props = defineProps<{ lesson: ClassLessonItem | null }>()

const open = defineModel<boolean>('open', { default: false })
const emit = defineEmits<{ saved: [] }>()

const isMobile = useIsMobile()
const config = useRuntimeConfig()
const toast = useToast()
const loading = ref(false)

const schema = z.object({
  plannedContent: z.string().max(2000, 'Máximo 2000 caracteres'),
})

type Schema = z.output<typeof schema>

const formState = reactive<Schema>({ plannedContent: '' })

watch(open, (val) => {
  if (val) formState.plannedContent = props.lesson?.plannedContent ?? ''
})

const title = computed(() =>
  props.lesson ? `Planejamento · Aula ${props.lesson.number}` : 'Planejamento',
)

const description = computed(() =>
  props.lesson
    ? `${formatClassLesson(props.lesson)} — descreva o que será abordado na aula.`
    : 'Descreva o que será abordado na aula.',
)

async function onSubmit(event: FormSubmitEvent<Schema>) {
  if (!props.lesson) return

  loading.value = true
  try {
    await $fetch(`${config.public.backendUrl}/teachers/lessons/${props.lesson.id}/plan`, {
      method: 'PUT',
      body: { plannedContent: event.data.plannedContent },
      credentials: 'include',
    })
    toast.add({ title: 'Planejamento salvo com sucesso', color: 'success' })
    open.value = false
    emit('saved')
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao salvar o planejamento.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <UModal
    v-model:open="open"
    :title="title"
    :description="description"
    :fullscreen="isMobile"
  >
    <template #body>
      <UForm
        :schema="schema"
        :state="formState"
        class="space-y-4"
        @submit="onSubmit"
      >
        <UFormField label="Conteúdo planejado" name="plannedContent">
          <template #hint>
            <span
              class="text-xs"
              :class="formState.plannedContent.length > 2000 ? 'text-error' : 'text-muted'"
            >
              {{ formState.plannedContent.length }} / 2000
            </span>
          </template>

          <UTextarea
            v-model="formState.plannedContent"
            class="w-full"
            :rows="6"
            :maxrows="16"
            autoresize
            placeholder="Ex: Introdução a grafos: definições, representação por matriz de adjacência e exercícios."
          />
        </UFormField>

        <div class="flex justify-end gap-2 pt-2">
          <UButton
            label="Cancelar"
            color="neutral"
            variant="subtle"
            :disabled="loading"
            @click="() => { open = false }"
          />
          <UButton label="Salvar" type="submit" :loading="loading" />
        </div>
      </UForm>
    </template>
  </UModal>
</template>
