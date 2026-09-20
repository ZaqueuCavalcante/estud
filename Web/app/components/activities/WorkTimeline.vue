<script setup lang="ts">
import type { ClassActivityWorkEntry, ClassActivityWorkNoteChange, ClassActivityWorkStatusChange } from '~/types/classes'

defineProps<{ entries: ClassActivityWorkEntry[], emptyMessage?: string }>()

function noteChange(entry: ClassActivityWorkEntry) {
  return entry.metadata as ClassActivityWorkNoteChange | null
}

function statusChange(entry: ClassActivityWorkEntry) {
  return entry.metadata as ClassActivityWorkStatusChange | null
}
</script>

<template>
  <div v-if="entries.length" class="flex flex-col gap-5">
    <div v-for="entry in entries" :key="entry.id" class="flex flex-col gap-1.5">
      <div class="flex flex-wrap items-center gap-x-2 gap-y-1">
        <UAvatar :src="entry.userPhoto ?? undefined" :alt="entry.user ?? ''" size="2xs" />
        <span class="text-sm font-medium text-highlighted">{{ entry.user }}</span>
        <time :datetime="entry.createdAt" class="text-xs text-muted">
          {{ formatClassActivityWorkEntryDate(entry.createdAt) }}
        </time>
      </div>

      <div v-if="entry.type === 'Comment'" class="rounded-lg border border-default p-3">
        <MarkdownContent :value="entry.content ?? ''" />
      </div>

      <div v-else-if="entry.type === 'StatusChange'" class="flex flex-wrap items-center gap-2 text-sm text-muted">
        <UIcon name="i-lucide-git-branch" class="size-4" />
        <span>{{ classActivityWorkEntryTypeLabels[entry.type] ?? entry.type }}</span>
        <template v-if="statusChange(entry)">
          <UBadge
            :label="classActivityWorkStatusLabels[statusChange(entry)!.fromStatus] ?? statusChange(entry)!.fromStatus"
            :color="classActivityWorkStatusColors[statusChange(entry)!.fromStatus] ?? 'neutral'"
            variant="subtle"
          />
          <UIcon name="i-lucide-arrow-right" class="size-3.5" />
          <UBadge
            :label="classActivityWorkStatusLabels[statusChange(entry)!.toStatus] ?? statusChange(entry)!.toStatus"
            :color="classActivityWorkStatusColors[statusChange(entry)!.toStatus] ?? 'neutral'"
            variant="subtle"
          />
        </template>
      </div>

      <div v-else class="flex flex-wrap items-center gap-2 text-sm text-muted">
        <UIcon name="i-lucide-award" class="size-4" />
        <span>{{ classActivityWorkEntryTypeLabels[entry.type] ?? entry.type }}</span>
        <template v-if="noteChange(entry)">
          <UBadge
            :label="formatClassActivityNote(noteChange(entry)!.fromNote)"
            color="error"
            variant="subtle"
          />
          <UIcon name="i-lucide-arrow-right" class="size-3.5" />
          <UBadge
            :label="formatClassActivityNote(noteChange(entry)!.toNote)"
            color="success"
            variant="subtle"
          />
        </template>
      </div>
    </div>
  </div>

  <p v-else class="text-sm text-muted">
    {{ emptyMessage ?? 'Nada por aqui ainda' }}
  </p>
</template>
