<script setup lang="ts">
defineProps<{
  // Preenchimento do anel e posição do ponteiro das horas, de 0 a 100
  percent: number
  // Linha principal, à direita do mostrador
  title: string
  // Linha secundária, em muted
  subtitle: string
  // Classe de cor (ex: "text-primary", "text-dimmed")
  colorClass: string
}>()
</script>

<template>
  <div class="flex items-center gap-3">
    <ClassroomsUsedMinutesRing :percent="percent" class="size-14" :class="colorClass" />
    <div class="flex flex-col leading-tight">
      <span class="text-sm font-medium text-highlighted">{{ title }}</span>
      <span class="text-xs text-muted">{{ subtitle }}</span>
    </div>
  </div>
</template>
