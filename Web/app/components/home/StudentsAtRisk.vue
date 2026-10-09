<script setup lang="ts">
// TODO: trocar pelos dados do backend
const risk = {
  totalStudents: 1240,
  byFrequency: 87,
  byGrade: 132,
  both: 41,
}

const onlyFrequency = risk.byFrequency - risk.both
const onlyGrade = risk.byGrade - risk.both
const totalAtRisk = onlyFrequency + onlyGrade + risk.both

const percentOf = (value: number, total = risk.totalStudents) => total ? (value / total) * 100 : 0
const formatPercent = (value: number) => `${value.toFixed(1).replace('.', ',')}%`

const segments = [
  { label: 'Só por falta', value: onlyFrequency, color: 'bg-error' },
  { label: 'Falta e nota', value: risk.both, color: 'bg-error/50' },
  { label: 'Só por nota', value: onlyGrade, color: 'bg-warning' },
]
</script>

<template>
  <UCard :ui="{ body: 'flex flex-col gap-4' }">
    <template #header>
      <p class="text-xs text-muted uppercase">
        Alunos em risco de reprovação
      </p>
    </template>

    <div>
      <p class="text-3xl font-semibold text-highlighted tabular-nums">{{ totalAtRisk }}</p>
      <p class="text-xs text-dimmed">{{ formatPercent(percentOf(totalAtRisk)) }} de {{ risk.totalStudents }} alunos</p>
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
  </UCard>
</template>
