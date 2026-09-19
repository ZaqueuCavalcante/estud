<script setup lang="ts">
import type { TeacherActivityWorkItem } from '~/types/classes'

defineProps<{ work: TeacherActivityWorkItem | null }>()

const open = defineModel<boolean>('open', { default: false })

const isMobile = useIsMobile()
</script>

<template>
  <UModal
    v-model:open="open"
    :title="work ? `Entrega de ${work.student}` : 'Entrega'"
    :fullscreen="isMobile"
    :ui="{ content: 'sm:max-w-3xl' }"
  >
    <template #body>
      <MarkdownContent v-if="work?.content" :value="work.content" />
    </template>
  </UModal>
</template>
