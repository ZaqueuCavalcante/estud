<script setup lang="ts">
import type { PeriodItem } from '~/types/calendar'
import type { GetLowestGradeClassesOut } from '~/types/frequencies'

const { noteLimit, period } = defineProps<{ noteLimit: number, period?: PeriodItem }>()

const config = useRuntimeConfig()
const { can } = usePolicy()
const canGetClasses = can('GetLowestGradeClasses')

const { data, execute } = await useFetch<GetLowestGradeClassesOut>(`${config.public.backendUrl}/insights/classes/lowest-grade`, {
  query: computed(() => ({ periodId: period?.id })),
  credentials: 'include',
  server: false,
  immediate: false,
  watch: false,
})

watch(() => period?.id, (id) => {
  if (id && canGetClasses.value) execute()
}, { immediate: true })

const classes = computed(() => period ? data.value?.classes ?? [] : [])

const formatGrade = (value: number) => value.toFixed(1).replace('.', ',')
</script>

<template>
  <UCard :ui="{ body: 'p-0!' }">
    <template #header>
      <p class="text-xs text-muted uppercase">
        Turmas com menor nota média
      </p>
    </template>

    <div v-if="!classes.length" class="flex flex-col items-center justify-center gap-3 py-12 text-center">
      <UIcon name="i-lucide-list-ordered" class="size-8 text-muted" />
      <p class="text-sm text-muted">
        Nenhuma nota lançada no período
      </p>
    </div>

    <ol v-else class="divide-y divide-default">
      <li v-for="(c, i) in classes" :key="c.id" class="flex items-center gap-3 px-4 py-3 sm:px-6">
        <span class="w-4 shrink-0 text-sm font-semibold text-dimmed tabular-nums">
          {{ i + 1 }}
        </span>

        <div class="flex-1 min-w-0 space-y-1.5">
          <div class="flex items-baseline justify-between gap-2">
            <p class="truncate text-sm font-medium text-highlighted">
              {{ c.discipline }}
            </p>
            <span
              class="shrink-0 text-sm font-semibold tabular-nums"
              :class="c.average < noteLimit ? 'text-error' : 'text-success'"
            >
              {{ formatGrade(c.average) }}
            </span>
          </div>

          <UProgress
            :model-value="c.average"
            :max="10"
            size="xs"
            :color="c.average < noteLimit ? 'error' : 'success'"
          />
        </div>
      </li>
    </ol>
  </UCard>
</template>
