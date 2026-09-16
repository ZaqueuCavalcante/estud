<script setup lang="ts">
defineProps<{ value: string }>()
</script>

<template>
  <LazyUEditor
    :model-value="value"
    content-type="markdown"
    :editable="false"
    :image="false"
    :mention="false"
    :ui="{ base: 'sm:px-0' }"
  />
</template>
