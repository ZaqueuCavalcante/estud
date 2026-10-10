<script setup lang="ts">
import type { PeriodItem } from '~/types/calendar'
import type { GetPendingAttendanceOut } from '~/types/frequencies'

const { period } = defineProps<{ period?: PeriodItem }>()

const config = useRuntimeConfig()
const { can } = usePolicy()
const canGetPendingAttendance = can('GetPendingAttendance')

const { data, execute } = await useFetch<GetPendingAttendanceOut>(`${config.public.backendUrl}/insights/classes/pending-attendance`, {
  query: computed(() => ({ periodId: period?.id })),
  credentials: 'include',
  server: false,
  immediate: false,
  watch: false,
})

watch(() => period?.id, (id) => {
  if (id && canGetPendingAttendance.value) execute()
}, { immediate: true })

const summary = computed(() => period ? data.value : undefined)
const upToDate = computed(() => summary.value?.upToDate ?? 100)
const pastLessons = computed(() => summary.value?.pastLessons ?? 0)
const classes = computed(() => summary.value?.classes ?? [])

const formatPercent = (value: number) => `${value.toFixed(1).replace('.', ',')}%`
</script>

<template>
  <UCard :ui="{ body: 'p-0!' }">
    <template #header>
      <p class="text-xs text-muted uppercase mb-1.5">
        Chamadas em dia
      </p>
      <p v-if="!pastLessons" class="text-3xl font-semibold text-dimmed">
        —
      </p>
      <p v-else class="text-3xl font-semibold tabular-nums" :class="upToDate < 90 ? 'text-error' : 'text-success'">
        {{ formatPercent(upToDate) }}
      </p>
    </template>

    <div v-if="!pastLessons" class="flex flex-col items-center justify-center gap-3 py-12 text-center">
      <UIcon name="i-lucide-list-ordered" class="size-8 text-muted" />
      <p class="text-sm text-muted">
        Nenhuma aula ocorrida no período
      </p>
    </div>

    <div v-else-if="!classes.length" class="flex flex-col items-center justify-center gap-3 py-12 text-center">
      <UIcon name="i-lucide-circle-check" class="size-8 text-success" />
      <p class="text-sm text-muted">
        Todas as chamadas estão em dia
      </p>
    </div>

    <ul v-else class="divide-y divide-default">
      <li v-for="c in classes" :key="c.id" class="flex items-center gap-3 px-4 py-3 sm:px-6">
        <p class="flex-1 min-w-0 truncate text-sm font-medium text-highlighted">
          {{ c.discipline }}
        </p>

        <UBadge
          :label="`${c.pendingLessons} ${c.pendingLessons === 1 ? 'aula' : 'aulas'}`"
          color="error"
          variant="subtle"
          class="shrink-0 tabular-nums"
        />
      </li>
    </ul>
  </UCard>
</template>
