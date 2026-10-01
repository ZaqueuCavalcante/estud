<script setup lang="ts">
interface RoleItem {
  id: number
  name: string
  description: string
  permissions: number
}

interface GetRolesOut {
  total: number
  items: RoleItem[]
}

const config = useRuntimeConfig()
const createModalOpen = ref(false)
const editModalOpen = ref(false)
const selectedRoleId = ref<number | null>(null)

function openEdit(role: RoleItem) {
  selectedRoleId.value = role.id
  editModalOpen.value = true
}

const { data, status, refresh } = await useFetch<GetRolesOut>(`${config.public.backendUrl}/identity/roles`, {
  credentials: 'include',
  server: false
})
</script>

<template>
  <div v-if="!data && status !== 'error'" class="flex flex-1 items-center justify-center py-12">
    <AppSpinner class="size-8" />
  </div>

  <TableEmptyState
    v-else-if="!data?.items?.length"
    :loading="false"
    icon="i-lucide-user-cog"
    message="Nenhum perfil cadastrado"
    button-label="Perfil"
    @create="() => { createModalOpen = true }"
  />

  <div v-else class="space-y-4">
    <div class="flex justify-start sm:justify-end">
      <UButton icon="i-lucide-plus" label="Perfil" @click="() => { createModalOpen = true }" />
    </div>

    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
      <button
        v-for="role in data.items"
        :key="role.id"
        type="button"
        class="rounded-xl bg-elevated flex flex-col gap-3 p-4 text-left hover:shadow-md hover:ring hover:ring-primary/50 transition-all duration-200 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
        @click="(e) => { (e.currentTarget as HTMLElement).blur(); openEdit(role) }"
      >
        <div class="flex items-start gap-3">
          <div class="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary/10">
            <UIcon name="i-lucide-user-cog" class="size-5 text-primary" />
          </div>
          <div class="min-w-0 flex-1">
            <p class="font-bold text-base text-highlighted truncate">{{ role.name }}</p>
            <p class="text-sm text-muted line-clamp-2">{{ role.description }}</p>
          </div>
        </div>

        <div class="mt-auto flex items-center gap-1.5 text-sm text-muted">
          <UIcon name="i-lucide-shield-check" class="size-4 shrink-0" />
          <span><span class="font-semibold tabular-nums text-highlighted">{{ role.permissions }}</span> {{ role.permissions === 1 ? 'permissão' : 'permissões' }}</span>
        </div>
      </button>
    </div>
  </div>

  <SecurityRolesCreateModal v-model:open="createModalOpen" @created="refresh()" />
  <SecurityRolesEditModal v-model:open="editModalOpen" :role-id="selectedRoleId" @updated="refresh()" />
</template>
