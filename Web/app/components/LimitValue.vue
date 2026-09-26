<script setup lang="ts">
defineProps<{
  label: string
  below: boolean
}>()
</script>

<template>
  <span
    class="inline-flex items-center gap-1 font-medium tabular-nums"
    :class="below ? 'text-error' : 'text-success'"
  >
    <UIcon v-if="below" name="i-lucide-triangle-alert" class="size-3.5 shrink-0" />
    <span v-if="below" class="sr-only">Abaixo do mínimo:</span>
    {{ label }}
  </span>
</template>
