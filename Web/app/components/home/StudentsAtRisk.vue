<script setup lang="ts">
import type { PeriodItem } from '~/types/calendar'
import type { GetStudentsAtRiskOut } from '~/types/frequencies'

const { period } = defineProps<{ period?: PeriodItem }>()

const config = useRuntimeConfig()
const { can } = usePolicy()
const canGetStudentsAtRisk = can('GetStudentsAtRisk')

const { data, execute } = await useFetch<GetStudentsAtRiskOut>(`${config.public.backendUrl}/insights/students/at-risk`, {
  query: computed(() => ({ periodId: period?.id })),
  credentials: 'include',
  server: false,
  immediate: false,
  watch: false,
})

watch(() => period?.id, (id) => {
  if (id && canGetStudentsAtRisk.value) execute()
}, { immediate: true })

const risk = computed(() => period ? data.value : undefined)
const totalStudents = computed(() => risk.value?.totalStudents ?? 0)
const totalAtRisk = computed(() => (risk.value?.onlyFrequency ?? 0) + (risk.value?.onlyGrade ?? 0) + (risk.value?.both ?? 0))

const percentOf = (value: number, total: number) => total ? (value / total) * 100 : 0
const formatPercent = (value: number) => `${value.toFixed(1).replace('.', ',')}%`

const segments = computed(() => [
  { label: 'Só por falta', value: risk.value?.onlyFrequency ?? 0, color: 'bg-error' },
  { label: 'Falta e nota', value: risk.value?.both ?? 0, color: 'bg-error/50' },
  { label: 'Só por nota', value: risk.value?.onlyGrade ?? 0, color: 'bg-warning' },
])
</script>

<template>
  <UCard :ui="{ body: 'flex flex-col gap-4' }">
    <template #header>
      <p class="text-xs text-muted uppercase">
        Alunos em risco de reprovação
      </p>
    </template>

    <template v-if="!totalStudents">
      <p class="text-3xl font-semibold text-dimmed">
        —
      </p>

      <div class="flex flex-col items-center justify-center gap-3 py-6 text-center">
        <UIcon name="i-lucide-users" class="size-8 text-muted" />
        <p class="text-sm text-muted">
          Nenhum aluno matriculado no período
        </p>
      </div>
    </template>

    <template v-else>
      <div>
        <p class="text-3xl font-semibold text-highlighted tabular-nums">{{ totalAtRisk }}</p>
        <p class="text-xs text-dimmed">{{ formatPercent(percentOf(totalAtRisk, totalStudents)) }} de {{ totalStudents }} alunos</p>
      </div>

      <div class="flex h-2.5 w-full overflow-hidden rounded-full bg-elevated gap-0.5">
        <div
          v-for="s in segments"
          :key="s.label"
          :class="s.color"
          :style="{ width: `${percentOf(s.value, totalAtRisk)}%` }"
        />
      </div>

      <ul class="space-y-1.5">
        <li v-for="s in segments" :key="s.label" class="flex items-center gap-2 text-sm">
          <span class="size-2 rounded-full shrink-0" :class="s.color" />
          <span class="flex-1 text-muted">{{ s.label }}</span>
          <span class="font-medium text-highlighted tabular-nums">{{ s.value }}</span>
        </li>
      </ul>
    </template>
  </UCard>
</template>
