<script setup lang="ts">
type ClassAttendance = { id: number, name: string, course: string, attendance: number }

const { frequencyLimit } = defineProps<{ frequencyLimit: number }>()

// TODO: trocar pelos dados do backend
const classes: ClassAttendance[] = [
  { id: 1, name: 'Cálculo I', course: 'Engenharia Civil', attendance: 58.4 },
  { id: 2, name: 'Física Geral', course: 'Engenharia Elétrica', attendance: 63.1 },
  { id: 3, name: 'Algoritmos e Programação', course: 'Sistemas de Informação', attendance: 68.9 },
  { id: 4, name: 'Química Orgânica', course: 'Técnico em Química', attendance: 72.5 },
  { id: 5, name: 'Estatística Aplicada', course: 'Administração', attendance: 76.2 },
]

const formatPercent = (value: number) => `${value.toFixed(1).replace('.', ',')}%`
</script>

<template>
  <UCard :ui="{ body: 'p-0!' }">
    <template #header>
      <p class="text-xs text-muted uppercase">
        Turmas com menor frequência
      </p>
    </template>

    <div v-if="!classes.length" class="flex flex-col items-center justify-center gap-3 py-12 text-center">
      <UIcon name="i-lucide-list-ordered" class="size-8 text-muted" />
      <p class="text-sm text-muted">
        Nenhuma frequência registrada no período
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
              {{ c.name }}
            </p>
            <span
              class="shrink-0 text-sm font-semibold tabular-nums"
              :class="c.attendance < frequencyLimit ? 'text-error' : 'text-success'"
            >
              {{ formatPercent(c.attendance) }}
            </span>
          </div>

          <p class="truncate text-xs text-muted">
            {{ c.course }}
          </p>

          <UProgress
            :model-value="c.attendance"
            :max="100"
            size="xs"
            :color="c.attendance < frequencyLimit ? 'error' : 'success'"
          />
        </div>
      </li>
    </ol>
  </UCard>
</template>
