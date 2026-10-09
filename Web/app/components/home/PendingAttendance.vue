<script setup lang="ts">
import { ptBR } from 'date-fns/locale'
import { formatDistanceToNowStrict, parseISO } from 'date-fns'

type PendingClass = { id: number, name: string, teacher: string, pendingLessons: number, oldestPendingAt: string }

// TODO: trocar pelos dados do backend
const summary = { pastLessons: 1380, pendingLessons: 23 }
const classes: PendingClass[] = [
  { id: 1, name: 'Cálculo I', teacher: 'Marina Albuquerque', pendingLessons: 7, oldestPendingAt: '2026-09-15' },
  { id: 2, name: 'Química Orgânica', teacher: 'Ricardo Tavares', pendingLessons: 5, oldestPendingAt: '2026-09-22' },
  { id: 3, name: 'Física Geral', teacher: 'Paulo Henrique Lima', pendingLessons: 4, oldestPendingAt: '2026-09-29' },
  { id: 4, name: 'Estatística Aplicada', teacher: 'Juliana Costa', pendingLessons: 2, oldestPendingAt: '2026-10-02' },
  { id: 5, name: 'Algoritmos e Programação', teacher: 'Fernando Rocha', pendingLessons: 1, oldestPendingAt: '2026-10-07' },
]

const upToDate = summary.pastLessons
  ? ((summary.pastLessons - summary.pendingLessons) / summary.pastLessons) * 100
  : 100

const formatPercent = (value: number) => `${value.toFixed(1).replace('.', ',')}%`
const formatSince = (date: string) => formatDistanceToNowStrict(parseISO(date), { locale: ptBR, addSuffix: true })
</script>

<template>
  <UCard :ui="{ body: 'p-0!' }">
    <template #header>
      <p class="text-xs text-muted uppercase mb-1.5">
        Chamadas em dia
      </p>
      <p class="text-3xl font-semibold text-highlighted tabular-nums">
        {{ formatPercent(upToDate) }}
      </p>
      <p class="text-xs text-dimmed">
        {{ summary.pendingLessons }} {{ summary.pendingLessons === 1 ? 'aula' : 'aulas' }} com chamada pendente
      </p>
    </template>

    <div v-if="!classes.length" class="flex flex-col items-center justify-center gap-3 py-12 text-center">
      <UIcon name="i-lucide-circle-check" class="size-8 text-success" />
      <p class="text-sm text-muted">
        Todas as chamadas estão em dia
      </p>
    </div>

    <ul v-else class="divide-y divide-default">
      <li v-for="c in classes" :key="c.id" class="flex items-center gap-3 px-4 py-3 sm:px-6">
        <div class="flex-1 min-w-0">
          <p class="truncate text-sm font-medium text-highlighted">
            {{ c.name }}
          </p>
          <p class="truncate text-xs text-muted">
            {{ c.teacher }} · desde {{ formatSince(c.oldestPendingAt) }}
          </p>
        </div>

        <UBadge
          :label="`${c.pendingLessons} ${c.pendingLessons === 1 ? 'aula' : 'aulas'}`"
          color="warning"
          variant="subtle"
          class="shrink-0 tabular-nums"
        />
      </li>
    </ul>
  </UCard>
</template>
