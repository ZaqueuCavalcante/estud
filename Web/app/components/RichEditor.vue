<script setup lang="ts">
import type { EditorCustomHandlers, EditorToolbarItem } from '@nuxt/ui'

type Editor = Parameters<EditorCustomHandlers[string]['execute']>[0]
type EditorView = Editor['view']

const props = defineProps<{
  placeholder?: string
  autofocus?: boolean
  uploadImage?: (file: File) => Promise<string>
  uploadPdf?: (file: File) => Promise<string>
  readonly?: boolean
}>()

const value = defineModel<string>({ default: '' })
const uploading = defineModel<number>('uploading', { default: 0 })

const toast = useToast()

const imageTypes = ['image/png', 'image/jpeg', 'image/webp']
const maxImageSize = 5 * 1024 * 1024
const maxPdfSize = 10 * 1024 * 1024

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
    ...(props.uploadPdf ? [{ kind: 'pdf', icon: 'i-lucide-paperclip', tooltip: { text: 'Anexar PDF' } }] : []),
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
  pdf: {
    canExecute: () => !!props.uploadPdf,
    execute: (editor) => {
      pickPdfs(editor.view)
      return editor.chain()
    },
    isActive: () => false,
  },
}

const editorProps = props.uploadImage || props.uploadPdf
  ? {
      handlePaste: (view: EditorView, event: ClipboardEvent) => insertFiles(view, event.clipboardData?.files),
      handleDrop: (view: EditorView, event: DragEvent) => {
        const pos = view.posAtCoords({ left: event.clientX, top: event.clientY })?.pos
        return insertFiles(view, event.dataTransfer?.files, pos)
      },
    }
  : undefined

function insertFiles(view: EditorView, list?: FileList | null, pos?: number) {
  const images = props.uploadImage ? imageFiles(list) : []
  if (images.length > 0) {
    insertImage(view, images, pos)
    return true
  }

  const pdfs = props.uploadPdf ? pdfFiles(list) : []
  if (pdfs.length > 0) {
    insertPdf(view, pdfs, pos)
    return true
  }

  return false
}

function imageFiles(list?: FileList | null) {
  return Array.from(list ?? []).filter(file => file.type.startsWith('image/'))
}

function pdfFiles(list?: FileList | null) {
  return Array.from(list ?? []).filter(file => file.type === 'application/pdf')
}

function isAcceptedImage(files: File[]) {
  if (files.length > 1) {
    toast.add({
      title: 'Envie uma imagem por vez',
      description: `Foram selecionadas ${files.length} imagens.`,
      color: 'error',
    })
    return false
  }

  const file = files[0]!

  if (!imageTypes.includes(file.type)) {
    toast.add({
      title: 'Formato de imagem inválido',
      description: `${file.name} não é PNG, JPEG ou WebP.`,
      color: 'error',
    })
    return false
  }

  if (file.size > maxImageSize) {
    toast.add({
      title: 'Imagem muito grande',
      description: `${file.name} tem ${formatSize(file.size)}, e o limite é de ${formatSize(maxImageSize)}.`,
      color: 'error',
    })
    return false
  }

  return true
}

function isAcceptedPdf(files: File[]) {
  if (files.length > 1) {
    toast.add({
      title: 'Envie um PDF por vez',
      description: `Foram selecionados ${files.length} arquivos.`,
      color: 'error',
    })
    return false
  }

  const file = files[0]!

  if (file.size > maxPdfSize) {
    toast.add({
      title: 'PDF muito grande',
      description: `${file.name} tem ${formatSize(file.size)}, e o limite é de ${formatSize(maxPdfSize)}.`,
      color: 'error',
    })
    return false
  }

  return true
}

function formatSize(bytes: number) {
  return `${(bytes / 1024 / 1024).toFixed(1).replace('.', ',')} MB`
}

function pickImages(view: EditorView) {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = imageTypes.join(',')
  input.onchange = () => {
    const files = imageFiles(input.files)
    if (files.length > 0) insertImage(view, files)
  }
  input.click()
}

function pickPdfs(view: EditorView) {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = 'application/pdf'
  input.onchange = () => {
    const files = pdfFiles(input.files)
    if (files.length > 0) insertPdf(view, files)
  }
  input.click()
}

// Enquanto o envio não termina, o link aponta para uma âncora única que serve de marcador
// para achar o texto depois; `blob:` seria descartado pelo `isAllowedUri` do Tiptap.
function insertPdf(view: EditorView, files: File[], pos?: number) {
  if (!isAcceptedPdf(files)) return

  const file = files[0]!
  const placeholder = `#enviando-${crypto.randomUUID()}`
  const link = view.state.schema.marks.link!.create({ href: placeholder })
  const node = view.state.schema.text(file.name, [link])

  const tr = pos === undefined
    ? view.state.tr.replaceSelectionWith(node, false)
    : view.state.tr.insert(pos, node)

  // O link do Tiptap é `inclusive`: sem isso, o que for digitado logo depois vira parte do link.
  view.dispatch(tr.setStoredMarks([]))

  sendPdf(view, file, placeholder)
}

async function sendPdf(view: EditorView, file: File, placeholder: string) {
  uploading.value++
  try {
    const href = await props.uploadPdf!(file)
    replacePdf(view, placeholder, href)
  } catch (err: unknown) {
    replacePdf(view, placeholder, null)
    toast.add({
      title: 'Não foi possível enviar o PDF',
      description: (err as { data?: { message?: string } })?.data?.message ?? 'Tente novamente.',
      color: 'error',
    })
  } finally {
    uploading.value--
  }
}

function replacePdf(view: EditorView, placeholder: string, href: string | null) {
  if (view.isDestroyed) return

  const linkType = view.state.schema.marks.link!
  const tr = view.state.tr
  view.state.doc.descendants((node, pos) => {
    const mark = node.marks.find(m => m.type === linkType && m.attrs.href === placeholder)
    if (!node.isText || !mark) return

    const from = tr.mapping.map(pos)
    const to = tr.mapping.map(pos + node.nodeSize)

    if (href) tr.addMark(from, to, linkType.create({ ...mark.attrs, href }))
    else tr.delete(from, to)
  })
  view.dispatch(tr)
}

function insertImage(view: EditorView, files: File[], pos?: number) {
  if (!isAcceptedImage(files)) return

  const file = files[0]!
  const localSrc = URL.createObjectURL(file)
  const node = view.state.schema.nodes.image!.create({ src: localSrc })

  view.dispatch(pos === undefined
    ? view.state.tr.replaceSelectionWith(node)
    : view.state.tr.insert(pos, node))

  upload(view, file, localSrc)
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
    :editable="!readonly"
    :image="readonly || !!uploadImage"
    :mention="false"
    :handlers="editorHandlers"
    :editor-props="editorProps"
    class="flex flex-col gap-3 rounded-lg border border-default p-3"
    :class="{ 'min-h-64': !readonly }"
    :ui="{ base: 'sm:px-0' }"
  >
    <template v-if="!readonly" #default="{ editor }">
      <UEditorToolbar :editor="editor" :items="toolbarItems" class="border-b border-default pb-3" />
    </template>
  </UEditor>
</template>
