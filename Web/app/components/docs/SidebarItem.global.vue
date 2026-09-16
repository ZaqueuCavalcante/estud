<script setup lang="ts">
const props = defineProps<{
  to?: string
  label?: string
}>()

const { sidebarGroups } = useSidebarNav()

const link = computed(() => {
  const items = sidebarGroups.flatMap(group => group.items)
  return items.find(item => item.to === props.to) ?? items.find(item => item.label === props.label)
})
</script>

<template>
  <span class="not-prose inline-flex items-baseline gap-1 rounded bg-elevated px-1 py-px text-[0.9em] font-medium text-highlighted ring ring-default">
    <UIcon v-if="link?.icon" :name="link.icon" class="size-3.5 self-center text-dimmed" />
    {{ link?.label ?? props.label ?? props.to }}
  </span>
</template>
