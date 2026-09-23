<script setup lang="ts">
import { NodeViewWrapper, nodeViewProps } from '@tiptap/vue-3'

const props = defineProps(nodeViewProps)

const uploading = computed(() => !!props.node.attrs.uploadId)
</script>

<template>
  <NodeViewWrapper as="span" class="inline-block align-top m-1" data-drag-handle>
    <component
      :is="editor.isEditable ? 'span' : 'a'"
      :href="editor.isEditable ? undefined : node.attrs.href"
      :target="editor.isEditable ? undefined : '_blank'"
      rel="noopener"
      :title="node.attrs.name"
      class="flex w-28 flex-col items-center gap-1 rounded-md p-2 text-center no-underline! border-0! font-normal! text-muted! transition-colors hover:text-primary!"
      :class="{ 'bg-elevated ring-2 ring-primary': selected && editor.isEditable }"
    >
      <AppSpinner v-if="uploading" class="size-10" />
      <UIcon v-else name="i-lucide-file-text" class="size-10" />
      <span class="w-full text-xs leading-tight break-all line-clamp-2">{{ node.attrs.name }}</span>
    </component>
  </NodeViewWrapper>
</template>
