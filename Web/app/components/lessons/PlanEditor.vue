<script setup lang="ts">
import type { EditorCustomHandlers, EditorToolbarItem } from '@nuxt/ui'

const props = defineProps<{ lessonId: number, plannedContent: string | null }>()
const emit = defineEmits<{ updated: [] }>()

const config = useRuntimeConfig()
const toast = useToast()

const maxLength = 10000

const editing = ref(false)
const saving = ref(false)
const plan = ref('')

const saved = computed(() => props.plannedContent ?? '')
const hasPlan = computed(() => saved.value.trim().length > 0)
const dirty = computed(() => plan.value.trim() !== saved.value.trim())
const tooLong = computed(() => plan.value.length > maxLength)

const toolbarItems: EditorToolbarItem[][] = [
  [
    { kind: 'mark', mark: 'bold', icon: 'i-lucide-bold', tooltip: { text: 'Negrito' } },
    { kind: 'mark', mark: 'italic', icon: 'i-lucide-italic', tooltip: { text: 'Itálico' } },
    { kind: 'mark', mark: 'strike', icon: 'i-lucide-strikethrough', tooltip: { text: 'Riscado' } },
    { kind: 'mark', mark: 'code', icon: 'i-lucide-code', tooltip: { text: 'Código' } },
  ],
  [
    { kind: 'heading', level: 1, icon: 'i-lucide-heading-1', tooltip: { text: 'Título 1' } },
    { kind: 'heading', level: 2, icon: 'i-lucide-heading-2', tooltip: { text: 'Título 2' } },
    { kind: 'heading', level: 3, icon: 'i-lucide-heading-3', tooltip: { text: 'Título 3' } },
  ],
  [
    { kind: 'bulletList', icon: 'i-lucide-list', tooltip: { text: 'Lista' } },
    { kind: 'orderedList', icon: 'i-lucide-list-ordered', tooltip: { text: 'Lista numerada' } },
    { kind: 'blockquote', icon: 'i-lucide-quote', tooltip: { text: 'Citação' } },
    { kind: 'codeBlock', icon: 'i-lucide-square-code', tooltip: { text: 'Bloco de código' } },
    { kind: 'link', icon: 'i-lucide-link', tooltip: { text: 'Link' } },
  ],
  [
    { kind: 'undo', icon: 'i-lucide-undo', tooltip: { text: 'Desfazer' } },
    { kind: 'redo', icon: 'i-lucide-redo', tooltip: { text: 'Refazer' } },
  ],
]

// O handler de link do Nuxt UI abre um `prompt('Enter the URL:')` em inglês.
const editorHandlers: EditorCustomHandlers = {
  link: {
    canExecute: editor => editor.can().setLink({ href: '' }) || editor.can().unsetLink(),
    execute: (editor) => {
      const chain = editor.chain()
      if (editor.getAttributes('link').href) return chain.focus().unsetLink()

      const href = prompt('Endereço do link:')
      return href ? chain.focus().setLink({ href }) : chain
    },
    isActive: editor => editor.isActive('link'),
    isDisabled: editor => editor.state.selection.empty && !editor.isActive('link'),
  },
}

function startEditing() {
  plan.value = saved.value
  editing.value = true
}

function cancelEditing() {
  editing.value = false
  plan.value = ''
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
            label="Salvar"
            :loading="saving"
            :disabled="!dirty || tooLong"
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
      <UEditor
        v-model="plan"
        content-type="markdown"
        placeholder="Descreva o que será abordado na aula."
        autofocus="end"
        :image="false"
        :mention="false"
        :handlers="editorHandlers"
        class="flex min-h-64 flex-col gap-3 rounded-lg border border-default p-3"
        :ui="{ base: 'sm:px-0' }"
      >
        <template #default="{ editor }">
          <UEditorToolbar :editor="editor" :items="toolbarItems" class="border-b border-default pb-3" />
        </template>
      </UEditor>

      <p v-if="tooLong" class="text-xs text-error">
        O planejamento passou de {{ maxLength }} caracteres ({{ plan.length }}).
      </p>
    </template>
  </section>
</template>
