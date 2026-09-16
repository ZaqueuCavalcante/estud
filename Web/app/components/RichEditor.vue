<script setup lang="ts">
import type { EditorCustomHandlers, EditorToolbarItem } from '@nuxt/ui'

defineProps<{ placeholder?: string, autofocus?: boolean }>()

const value = defineModel<string>({ default: '' })

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
</script>

<template>
  <UEditor
    v-model="value"
    content-type="markdown"
    :placeholder="placeholder"
    :autofocus="autofocus ? 'end' : false"
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
</template>
