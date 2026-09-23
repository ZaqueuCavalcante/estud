<script setup lang="ts">
import type { GetAgendaOut } from '~/types/agenda'

const config = useRuntimeConfig()
const { account } = useUserAccount()

const url = computed(() => {
  const base = config.public.backendUrl

  if (account.value?.userType === 'Teacher') return `${base}/teachers/agenda`

  return `${base}/students/agenda`
})

const { data, status } = await useAsyncData<GetAgendaOut>(
  'agenda',
  () => $fetch<GetAgendaOut>(url.value, { credentials: 'include' }),
  { server: false, watch: [url] }
)
</script>

<template>
  <UDashboardPanel id="agenda">
    <template #header>
      <UDashboardNavbar title="Agenda">
        <template #leading>
          <PageIcon />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div v-if="status === 'idle' || status === 'pending'" class="flex flex-1 items-center justify-center">
        <AppSpinner class="size-8" />
      </div>
      <AgendaWeek v-else :days="data?.days ?? []" />
    </template>
  </UDashboardPanel>
</template>
