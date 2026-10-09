<script setup lang="ts">
import { isAfter, isBefore, parseISO, startOfDay } from 'date-fns'
import type { GetPeriodsOut, PeriodItem } from '~/types/calendar'
import type { InstitutionConfig } from '~/types/configs'

const { account } = useUserAccount()
const config = useRuntimeConfig()
const { can } = usePolicy()
const canGetConfig = can('GetInstitutionConfig')
const canGetPeriods = can('GetAcademicPeriods')

const { data: institutionConfig } = await useFetch<InstitutionConfig>(`${config.public.backendUrl}/institutions/config`, {
  credentials: 'include',
  server: false,
  immediate: canGetConfig.value,
})

// Sem permissão pra ler a config, vale o padrão do backend (InstitutionConfig.DefaultFrequencyLimit).
const frequencyLimit = computed(() => institutionConfig.value?.frequencyLimit ?? 70)
const noteLimit = computed(() => institutionConfig.value?.noteLimit ?? 7)

const { data: periodsData } = await useFetch<GetPeriodsOut>(`${config.public.backendUrl}/periods/academic`, {
  credentials: 'include',
  server: false,
  immediate: canGetPeriods.value,
})

const periods = computed(() => periodsData.value?.items ?? [])
const periodOptions = computed(() => periods.value.map(p => ({ label: p.name, value: p.id })))

function currentPeriod(items: PeriodItem[]) {
  const today = startOfDay(new Date())
  const started = items.filter(p => !isAfter(parseISO(p.startAt), today))
  return started.find(p => !isBefore(parseISO(p.endAt), today))
    ?? started.at(-1)
    ?? items[0]
}

const periodId = ref<number>()
watch(periods, (items) => {
  if (!items.some(p => p.id === periodId.value)) periodId.value = currentPeriod(items)?.id
}, { immediate: true })

const period = computed(() => periods.value.find(p => p.id === periodId.value))
</script>

<template>
  <div class="flex flex-col gap-6 w-full">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <h2 class="text-2xl font-semibold text-highlighted">{{ account?.institution }}</h2>

      <USelectMenu
        v-if="periodOptions.length"
        v-model="periodId"
        :items="periodOptions"
        value-key="value"
        :search-input="false"
        icon="i-lucide-calendar-range"
        class="w-40"
      />
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-4 gap-6">
      <HomeStudentsAtRisk />
      <HomeLowestAttendanceClasses :frequency-limit="frequencyLimit" />
      <HomePendingAttendance />
      <HomeLowestGradeClasses :note-limit="noteLimit" />
    </div>

    <HomeAttendanceChart class="w-full" :frequency-limit="frequencyLimit" :period="period" />
  </div>
</template>
