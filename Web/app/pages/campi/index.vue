<script setup lang="ts">
interface CampusItem {
  id: number
  name: string
  state: string
  city: string
  students: number
  teachers: number
}

interface GetCampiOut {
  total: number
  items: CampusItem[]
}

const config = useRuntimeConfig()
const createModalOpen = ref(false)

const { data, status, refresh } = await useFetch<GetCampiOut>(`${config.public.backendUrl}/campi`, {
  credentials: 'include',
  server: false
})
</script>

<template>
  <UDashboardPanel id="campi">
    <template #header>
      <UDashboardNavbar title="Campi">
        <template #leading>
          <PageIcon />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div v-if="status === 'pending'" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        <USkeleton v-for="i in 3" :key="i" class="h-44 rounded-xl" />
      </div>

      <TableEmptyState
        v-else-if="!data?.items?.length"
        :loading="false"
        icon="i-lucide-map-pin"
        message="Nenhum campus cadastrado"
        button-label="Campus"
        @create="createModalOpen = true"
      />

      <div v-else class="space-y-4">
        <div class="flex items-start justify-between gap-4">
          <div>
            <p class="text-sm text-muted mt-0.5">Visualize e gerencie os campus da sua instituição.</p>
          </div>
          <UButton icon="i-lucide-plus" label="Campus" class="shrink-0" @click="() => { createModalOpen = true }" />
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          <NuxtLink
            v-for="campus in data.items"
            :key="campus.id"
            :to="`/campi/${campus.id}`"
            class="rounded-xl border border-default bg-elevated flex flex-col overflow-hidden hover:shadow-md hover:border-primary/50 transition-all duration-200 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          >
            <!-- Header -->
            <div class="px-4 pt-3 pb-3">
              <p class="font-bold text-base text-highlighted truncate">{{ campus.name }}</p>
              <div class="flex items-center gap-1 mt-0.5">
                <UIcon name="i-lucide-map-pin" class="size-3.5 text-muted shrink-0" />
                <p class="text-sm text-muted">{{ campus.city }} · {{ campus.state }}</p>
              </div>
            </div>

            <!-- Divisor -->
            <div class="border-t border-default mx-4" />

            <!-- Stats -->
            <div class="grid grid-cols-2 py-3">
              <div class="flex flex-col px-4 border-r border-default">
                <span class="text-2xl font-bold text-highlighted leading-none">{{ campus.teachers }}</span>
                <span class="text-xs text-muted mt-1">Professores</span>
              </div>
              <div class="flex flex-col px-4">
                <span class="text-2xl font-bold text-highlighted leading-none">{{ campus.students }}</span>
                <span class="text-xs text-muted mt-1">Alunos</span>
              </div>
            </div>
          </NuxtLink>
        </div>
      </div>
    </template>
  </UDashboardPanel>

  <CampiCreateModal v-model:open="createModalOpen" @created="refresh()" />
</template>
