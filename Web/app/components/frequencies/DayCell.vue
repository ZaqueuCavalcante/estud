<script setup lang="ts">
import type { StudentDayAttendanceStatus } from '~/types/frequencies'

const props = defineProps<{
  date: string
  status: StudentDayAttendanceStatus
  class?: string
}>()

const popoverMode = usePopoverMode()

const weekday = computed(() => attendanceWeekDayNames[new Date(`${props.date}T00:00:00`).getDay()]!)
</script>

<template>
  <UPopover
    :mode="popoverMode"
    :content="{ side: 'top' }"
    arrow
    :class="props.class"
  >
    <div
      class="aspect-square rounded-[2px] ring-1 ring-inset ring-default/40"
      :class="attendanceCellClass(props.status)"
    />

    <template #content>
      <div class="flex flex-col gap-1 px-2.5 py-1.5 text-xs">
        <span>{{ props.date }}</span>
        <span>{{ weekday }}</span>
        <span class="flex items-center gap-1.5">
          <span
            class="size-3 shrink-0 rounded-[2px] ring-1 ring-inset ring-default/40"
            :class="attendanceCellClass(props.status)"
          />
          {{ attendanceStatusLabels[props.status] }}
        </span>
      </div>
    </template>
  </UPopover>
</template>
