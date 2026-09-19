import { mergeAttributes, Node, VueNodeViewRenderer } from '@tiptap/vue-3'
import FileAttachmentView from '~/components/FileAttachmentView.vue'

const markdownPattern = /^\[((?:\\.|[^\\\]])*)\]\((\S+?\.pdf)\)/i

export const FileAttachment = Node.create({
  name: 'fileAttachment',
  group: 'inline',
  inline: true,
  atom: true,
  draggable: true,

  addAttributes() {
    return {
      href: { default: null },
      name: { default: '', rendered: false },
      uploadId: { default: null, rendered: false },
    }
  },

  parseHTML() {
    return [{
      tag: 'a[data-file-attachment]',
      priority: 60,
      getAttrs: el => ({ href: el.getAttribute('href'), name: el.textContent }),
    }]
  },

  renderHTML({ node, HTMLAttributes }) {
    return ['a', mergeAttributes(HTMLAttributes, { 'data-file-attachment': '' }), node.attrs.name]
  },

  addNodeView() {
    return VueNodeViewRenderer(FileAttachmentView)
  },

  markdownTokenizer: {
    name: 'fileAttachment',
    level: 'inline',
    start: '[',
    tokenize: (src) => {
      const match = markdownPattern.exec(src)
      if (!match) return

      return {
        type: 'fileAttachment',
        raw: match[0],
        name: match[1]!.replace(/\\(.)/g, '$1'),
        href: match[2],
      }
    },
  },

  parseMarkdown: (token, h) => h.createNode('fileAttachment', { href: token.href, name: token.name }),

  // Sem `href` o envio ainda não terminou; o nó não vai pro markdown até ter a URL.
  renderMarkdown: (node) => {
    const { href, name } = node.attrs ?? {}
    return href ? `[${String(name).replace(/[\\[\]]/g, '\\$&')}](${href})` : ''
  },
})
