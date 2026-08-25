<script setup lang="ts">
import type { HighlightItem } from '~/types/calendar'

withDefaults(defineProps<{
  title: string
  items: HighlightItem[]
  selectedKey: string | null
  emptyText?: string
  loading?: boolean
}>(), { emptyText: 'Nada neste ano.' })

const emit = defineEmits<{
  hover: [string | null]
  select: [string]
}>()
</script>

<template>
  <div class="w-full rounded-lg border border-default p-4 sm:w-fit sm:max-w-full">
    <p class="mb-3 text-sm font-semibold text-highlighted">
      {{ title }}
    </p>

    <div v-if="loading" class="flex justify-center py-4">
      <UIcon name="i-lucide-loader-circle" class="size-5 animate-spin text-muted" />
    </div>

    <p v-else-if="!items.length" class="py-1 text-sm text-dimmed">
      {{ emptyText }}
    </p>

    <ul v-else class="space-y-1.5">
      <li v-for="item in items" :key="item.key">
        <button
          type="button"
          :aria-pressed="item.key === selectedKey"
          class="flex w-full cursor-pointer items-center justify-between gap-4 rounded-md px-3 py-1.5 text-sm transition-colors"
          :class="item.key === selectedKey
            ? 'bg-primary/15 text-primary'
            : 'bg-elevated/50 text-default hover:bg-elevated'"
          @mouseenter="() => { emit('hover', item.key) }"
          @mouseleave="() => { emit('hover', null) }"
          @click="() => { emit('select', item.key) }"
        >
          <span class="whitespace-nowrap">{{ item.label }}</span>
          <span
            v-if="item.hint"
            class="shrink-0 whitespace-nowrap tabular-nums"
            :class="item.key === selectedKey ? 'text-primary' : 'text-muted'"
          >
            {{ item.hint }}
          </span>
        </button>
      </li>
    </ul>
  </div>
</template>
