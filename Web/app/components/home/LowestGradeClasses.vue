<script setup lang="ts">
type ClassGrade = { id: number, name: string, course: string, average: number }

const { noteLimit } = defineProps<{ noteLimit: number }>()

// TODO: trocar pelos dados do backend
const classes: ClassGrade[] = [
  { id: 1, name: 'Cálculo I', course: 'Engenharia Civil', average: 4.8 },
  { id: 2, name: 'Física Geral', course: 'Engenharia Elétrica', average: 5.3 },
  { id: 3, name: 'Química Orgânica', course: 'Técnico em Química', average: 5.9 },
  { id: 4, name: 'Estatística Aplicada', course: 'Administração', average: 6.6 },
  { id: 5, name: 'Algoritmos e Programação', course: 'Sistemas de Informação', average: 7.2 },
]

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
              {{ c.name }}
            </p>
            <span
              class="shrink-0 text-sm font-semibold tabular-nums"
              :class="c.average < noteLimit ? 'text-error' : 'text-success'"
            >
              {{ formatGrade(c.average) }}
            </span>
          </div>

          <p class="truncate text-xs text-muted">
            {{ c.course }}
          </p>

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
