<script setup lang="ts">
import type { EditorCustomHandlers, EditorToolbarItem } from '@nuxt/ui'

type Editor = Parameters<EditorCustomHandlers[string]['execute']>[0]
type EditorView = Editor['view']

const props = defineProps<{
  placeholder?: string
  autofocus?: boolean
  uploadImage?: (file: File) => Promise<string>
}>()

const value = defineModel<string>({ default: '' })
const uploading = defineModel<number>('uploading', { default: 0 })

const toast = useToast()

const imageTypes = ['image/png', 'image/jpeg', 'image/webp']
const maxImageSize = 5 * 1024 * 1024

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
    ...(props.uploadImage ? [{ kind: 'image', icon: 'i-lucide-image', tooltip: { text: 'Imagem' } } as const] : []),
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
  image: {
    canExecute: () => !!props.uploadImage,
    execute: (editor) => {
      pickImages(editor.view)
      return editor.chain()
    },
    isActive: () => false,
  },
}

const editorProps = props.uploadImage
  ? {
      handlePaste: (view: EditorView, event: ClipboardEvent) => {
        const files = imageFiles(event.clipboardData?.files)
        if (files.length === 0) return false

        insertImages(view, files)
        return true
      },
      handleDrop: (view: EditorView, event: DragEvent) => {
        const files = imageFiles(event.dataTransfer?.files)
        if (files.length === 0) return false

        const pos = view.posAtCoords({ left: event.clientX, top: event.clientY })?.pos
        insertImages(view, files, pos)
        return true
      },
    }
  : undefined

function imageFiles(list?: FileList | null) {
  return Array.from(list ?? []).filter(file => file.type.startsWith('image/'))
}

function acceptedImageFiles(files: File[]) {
  const accepted: File[] = []

  for (const file of files) {
    if (!imageTypes.includes(file.type)) {
      toast.add({
        title: 'Formato de imagem inválido',
        description: `${file.name} não é PNG, JPEG ou WebP.`,
        color: 'error',
      })
      continue
    }

    if (file.size > maxImageSize) {
      toast.add({
        title: 'Imagem muito grande',
        description: `${file.name} tem ${formatSize(file.size)}, e o limite é de ${formatSize(maxImageSize)}.`,
        color: 'error',
      })
      continue
    }

    accepted.push(file)
  }

  return accepted
}

function formatSize(bytes: number) {
  return `${(bytes / 1024 / 1024).toFixed(1).replace('.', ',')} MB`
}

function pickImages(view: EditorView) {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = imageTypes.join(',')
  input.multiple = true
  input.onchange = () => { insertImages(view, imageFiles(input.files)) }
  input.click()
}

function insertImages(view: EditorView, files: File[], pos?: number) {
  for (const file of acceptedImageFiles(files)) {
    const localSrc = URL.createObjectURL(file)
    const node = view.state.schema.nodes.image!.create({ src: localSrc })

    view.dispatch(pos === undefined
      ? view.state.tr.replaceSelectionWith(node)
      : view.state.tr.insert(pos, node))

    upload(view, file, localSrc)
  }
}

async function upload(view: EditorView, file: File, localSrc: string) {
  uploading.value++
  try {
    const src = await props.uploadImage!(file)
    replaceImage(view, localSrc, src)
  } catch (err: unknown) {
    replaceImage(view, localSrc, null)
    toast.add({
      title: 'Não foi possível enviar a imagem',
      description: (err as { data?: { message?: string } })?.data?.message ?? 'Tente novamente.',
      color: 'error',
    })
  } finally {
    uploading.value--
    URL.revokeObjectURL(localSrc)
  }
}

function replaceImage(view: EditorView, localSrc: string, src: string | null) {
  if (view.isDestroyed) return

  const tr = view.state.tr
  view.state.doc.descendants((node, pos) => {
    if (node.type.name !== 'image' || node.attrs.src !== localSrc) return

    if (src) tr.setNodeMarkup(tr.mapping.map(pos), undefined, { ...node.attrs, src })
    else tr.delete(tr.mapping.map(pos), tr.mapping.map(pos + node.nodeSize))
  })
  view.dispatch(tr)
}
</script>

<template>
  <UEditor
    v-model="value"
    content-type="markdown"
    :placeholder="placeholder"
    :autofocus="autofocus ? 'end' : false"
    :image="!!uploadImage"
    :mention="false"
    :handlers="editorHandlers"
    :editor-props="editorProps"
    class="flex min-h-64 flex-col gap-3 rounded-lg border border-default p-3"
    :ui="{ base: 'sm:px-0' }"
  >
    <template #default="{ editor }">
      <UEditorToolbar :editor="editor" :items="toolbarItems" class="border-b border-default pb-3" />
    </template>
  </UEditor>
</template>
