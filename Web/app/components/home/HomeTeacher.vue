<script setup lang="ts">
import type { GetTeacherHomeOut, TeacherHomeClassItem } from '~/types/teachers'

const { account } = useUserAccount()
const config = useRuntimeConfig()

const { data, status } = await useFetch<GetTeacherHomeOut>(`${config.public.backendUrl}/teachers/home`, {
  credentials: 'include',
  server: false,
})

const isLoading = computed(() => status.value === 'idle' || status.value === 'pending')

const classes = computed<TeacherHomeClassItem[]>(() => data.value?.classes ?? [])

const stats = computed(() => [
  { label: 'Turmas ativas', value: data.value?.activeClasses ?? 0, icon: 'i-lucide-presentation' },
  { label: 'Alunos', value: data.value?.students ?? 0, icon: 'i-lucide-users' },
])

const meta = computed(() => {
  const started = classes.value.filter(c => c.status === 'Started')
  const campi = [...new Set(started.map(c => c.campus).filter(Boolean))]
  const periods = [...new Set(started.map(c => c.period))]
  return [
    { icon: 'i-lucide-map-pin', text: campi.join(', ') },
    { icon: 'i-lucide-calendar', text: periods.join(', ') },
  ].filter(m => !!m.text)
})
</script>

<template>
  <div v-if="isLoading" class="flex flex-1 items-center justify-center">
    <AppSpinner class="size-8" />
  </div>

  <div v-else class="space-y-6">
    <div>
      <h2 class="text-2xl font-semibold text-highlighted">{{ account?.institution }}</h2>
      <div v-if="meta.length" class="flex flex-wrap items-center gap-x-4 gap-y-1 mt-2">
        <span v-for="m in meta" :key="m.icon" class="inline-flex items-center gap-1.5 text-xs text-muted">
          <UIcon :name="m.icon" class="size-3.5 shrink-0" />
          {{ m.text }}
        </span>
      </div>
    </div>

    <UCard v-if="!classes.length">
      <div class="flex flex-col items-center justify-center text-center py-10 gap-3">
        <div class="p-3 rounded-full bg-primary/10 ring ring-inset ring-primary/25">
          <UIcon name="i-lucide-presentation" class="size-6 text-primary" />
        </div>
        <div>
          <p class="font-medium text-highlighted">Você ainda não tem turmas atribuídas</p>
          <p class="text-sm text-muted mt-1">Assim que uma turma for atribuída a você, ela aparecerá aqui.</p>
        </div>
      </div>
    </UCard>

    <template v-else>
      <div class="grid grid-cols-2 gap-3">
        <div
          v-for="stat in stats"
          :key="stat.label"
          class="flex items-center gap-3 rounded-lg p-3 ring ring-default bg-elevated/40"
        >
          <div class="flex items-center justify-center p-2 rounded-lg bg-primary/10 ring ring-inset ring-primary/20 shrink-0">
            <UIcon :name="stat.icon" class="size-4 text-primary" />
          </div>
          <div class="min-w-0">
            <p class="text-xl font-semibold text-highlighted leading-none">{{ stat.value }}</p>
            <p class="text-xs text-muted mt-1 truncate">{{ stat.label }}</p>
          </div>
        </div>
      </div>

      <div>
        <div class="flex items-center justify-between gap-4 mb-4">
          <span class="font-semibold text-highlighted">Minhas turmas</span>
          <span class="text-sm text-muted">{{ classes.length }} {{ classes.length === 1 ? 'turma' : 'turmas' }}</span>
        </div>

        <div class="grid gap-3 sm:grid-cols-2">
          <NuxtLink
            v-for="c in classes"
            :key="c.id"
            :to="`/classes/${c.id}`"
            class="flex flex-col gap-3 rounded-lg p-3 ring ring-default bg-elevated/40 hover:bg-elevated transition-colors"
          >
            <div class="flex items-start gap-3">
              <div class="min-w-0 flex-1">
                <p class="font-medium text-highlighted wrap-break-word">{{ c.discipline }}</p>
                <p class="text-xs text-muted mt-0.5 truncate">{{ [c.period, c.campus].filter(Boolean).join(' · ') }}</p>
              </div>
              <UBadge
                :color="classStatusColors[c.status] ?? 'neutral'"
                variant="subtle"
                size="sm"
                class="shrink-0"
              >
                {{ classStatusLabels[c.status] ?? c.status }}
              </UBadge>
            </div>

            <div class="flex items-center gap-3 text-xs text-muted">
              <span class="inline-flex items-center gap-1.5 shrink-0">
                <UIcon name="i-lucide-users" class="size-3.5" />
                {{ c.students }} {{ c.students === 1 ? 'aluno' : 'alunos' }}
              </span>
              <template v-if="c.lessons">
                <UProgress :model-value="c.finishedLessons" :max="c.lessons" size="xs" class="flex-1" />
                <span class="shrink-0">{{ c.finishedLessons }}/{{ c.lessons }} aulas</span>
              </template>
            </div>
          </NuxtLink>
        </div>
      </div>
    </template>
  </div>
</template>
