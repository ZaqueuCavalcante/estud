<script setup lang="ts">
import type { PermissionItem, UserTypeName } from '~/composables/usePermissions'

const props = defineProps<{ permissions: PermissionItem[], baseType: UserTypeName }>()
const selected = defineModel<number[]>({ default: () => [] })

const uid = useId()

const access = computed(() => baseTypeAccess[props.baseType])

const categories = computed(() =>
  permissionCategories
    .map(({ label, icon, groups }) => ({
      label,
      icon,
      items: groups.flatMap(group => props.permissions.filter(p => p.group === group)),
    }))
    .filter(c => c.items.length)
)

function selectedCount(items: PermissionItem[]) {
  return items.filter(p => selected.value.includes(p.id)).length
}

function categoryState(items: PermissionItem[]) {
  const count = selectedCount(items)
  if (count === 0) return false
  if (count === items.length) return true
  return 'indeterminate'
}

function togglePermission(id: number) {
  selected.value = selected.value.includes(id)
    ? selected.value.filter(x => x !== id)
    : [...selected.value, id]
}

function toggleCategory(items: PermissionItem[]) {
  const ids = items.map(p => p.id)
  selected.value = selectedCount(items) === items.length
    ? selected.value.filter(id => !ids.includes(id))
    : [...new Set([...selected.value, ...ids])]
}
</script>

<template>
  <div class="flex flex-col gap-3 w-full">
    <div class="flex items-start gap-3 rounded-xl border border-info/25 bg-info/[0.06] p-4">
      <UIcon name="i-lucide-info" class="size-5 shrink-0 text-info" />
      <div class="flex flex-col gap-1">
        <p class="text-sm font-semibold text-highlighted">{{ access.title }}</p>
        <p class="text-sm text-muted">
          {{ access.description }}
          <template v-if="!permissions.length">
            Não existem permissões extras para marcar.
          </template>
        </p>
      </div>
    </div>

    <div
      v-for="(category, index) in categories"
      :key="category.label"
      class="rounded-lg border border-default divide-y divide-default"
    >
      <div class="flex items-center gap-2 px-3 py-2 bg-elevated/40">
        <UIcon :name="category.icon" class="size-4 shrink-0 text-muted" />
        <span class="flex-1 text-sm font-medium">{{ category.label }}</span>
        <span class="text-xs text-muted">{{ selectedCount(category.items) }} de {{ category.items.length }}</span>
        <UCheckbox
          :id="`${uid}-category-${index}`"
          :model-value="categoryState(category.items)"
          :aria-label="`Selecionar tudo em ${category.label}`"
          @update:model-value="() => { toggleCategory(category.items) }"
        />
      </div>

      <div class="flex flex-col gap-3 px-3 py-3">
        <UCheckbox
          v-for="perm in category.items"
          :id="`${uid}-permission-${perm.id}`"
          :key="perm.id"
          :label="perm.name"
          :description="perm.description"
          :model-value="selected.includes(perm.id)"
          @update:model-value="() => { togglePermission(perm.id) }"
        />
      </div>
    </div>
  </div>
</template>
