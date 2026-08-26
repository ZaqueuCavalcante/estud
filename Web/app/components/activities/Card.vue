<script setup lang="ts">
import type { ClassActivityItem } from '~/types/classes'

const props = defineProps<{ activity: ClassActivityItem, to: string }>()

const deliveredPercent = computed(() => props.activity.totalWorks > 0
  ? Math.round((props.activity.deliveredWorks / props.activity.totalWorks) * 100)
  : 0,
)
</script>

<template>
  <NuxtLink
    :to="to"
    class="flex flex-col gap-3 rounded-lg border border-default bg-elevated/40 p-4 transition-colors hover:bg-elevated"
  >
    <span class="font-medium text-highlighted">{{ activity.title }}</span>

    <div class="flex flex-wrap items-center gap-2">
      <UBadge :label="activity.note" color="neutral" variant="subtle" />
      <UBadge
        :label="classActivityTypeLabels[activity.type] ?? activity.type"
        :icon="classActivityTypeIcons[activity.type] ?? 'i-lucide-clipboard-list'"
        color="neutral"
        variant="subtle"
      />
      <span class="text-xs text-muted">Peso {{ activity.weight }}</span>
    </div>

    <span class="flex items-center gap-1.5 text-sm text-muted">
      <UIcon name="i-lucide-calendar-clock" class="size-4" />
      Entrega até {{ formatClassActivityDueDate(activity.dueDate, activity.dueHour) }}
    </span>

    <div class="mt-auto flex flex-wrap items-center justify-between gap-2 border-t border-default pt-3">
      <UBadge
        :label="`${activity.deliveredWorks} / ${activity.totalWorks} entregas`"
        :color="deliveredPercent === 100 ? 'success' : 'neutral'"
        variant="subtle"
        icon="i-lucide-file-check"
      />
      <UBadge
        :label="classActivityStatusLabels[activity.status] ?? activity.status"
        :color="classActivityStatusColors[activity.status] ?? 'neutral'"
        variant="subtle"
      />
    </div>
  </NuxtLink>
</template>
