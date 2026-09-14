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
      <div v-if="status === 'pending'" class="flex justify-center py-16">
        <UIcon name="i-lucide-loader-circle" class="size-8 animate-spin text-muted" />
      </div>
      <AgendaWeek v-else :days="data?.days ?? []" />
    </template>
  </UDashboardPanel>
</template>
