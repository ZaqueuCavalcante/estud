<script setup lang="ts">
import type { StudentClassActivityItem } from '~/types/classes'

defineProps<{ activity: StudentClassActivityItem, to: string }>()
</script>

<template>
  <NuxtLink
    :to="to"
    class="flex flex-col gap-3 rounded-lg bg-elevated p-4 transition-colors hover:bg-accented/60"
  >
    <span class="font-medium text-highlighted">{{ activity.title }}</span>

    <div class="flex flex-wrap items-center gap-2">
      <UBadge
        :label="classActivityTypeLabels[activity.type] ?? activity.type"
        :icon="classActivityTypeIcons[activity.type] ?? 'i-lucide-clipboard-list'"
        color="neutral"
        variant="subtle"
      />
      <UBadge
        :label="`${activity.weight}%`"
        icon="i-lucide-scale"
        color="neutral"
        variant="subtle"
      />
    </div>

    <span class="flex items-center gap-1.5 text-sm text-muted">
      <UIcon name="i-lucide-calendar-clock" class="size-4" />
      {{ classActivityDueLabel(activity) }} {{ formatClassActivityDueDate(activity.dueDate, activity.dueHour) }}
    </span>

    <div class="mt-auto flex flex-wrap items-center gap-2 border-t border-default pt-3">
      <UBadge
        v-if="activity.workStatus === 'Finalized'"
        :label="`Nota ${activity.value} · ${activity.ponderedValue} pontos`"
        color="neutral"
        variant="subtle"
        icon="i-lucide-award"
      />
      <UBadge
        class="ms-auto"
        :label="classActivityWorkStatusLabel(activity, activity.workStatus)"
        :color="classActivityWorkStatusColors[activity.workStatus] ?? 'neutral'"
        variant="subtle"
      />
    </div>
  </NuxtLink>
</template>
