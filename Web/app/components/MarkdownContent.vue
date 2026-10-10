<script setup lang="ts">
defineProps<{ value: string }>()
</script>

<template>
  <LazyUEditor
    :model-value="value"
    content-type="markdown"
    :editable="false"
    :extensions="[FileAttachment]"
    :ui="{ base: 'sm:px-0' }"
  />
</template>
