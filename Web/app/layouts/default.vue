<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'
import type { SidebarGroup, SidebarLink } from '~/composables/useSidebarNav'

const open = ref(false)
const { can } = usePolicy()
const { account } = useUserAccount()
const { unreadCount } = useNotifications()
const { isNotificationsSlideoverOpen } = useDashboard()
const { classes: teacherClasses, fetchClasses: fetchTeacherClasses } = useTeacherClasses()

const { sidebarGroups, isLinkActive } = useSidebarNav()

const route = useRoute()

// Grupos que sobraram depois do filtro de permissões. Grupos sem nenhum item
// visível são descartados, pra um perfil restrito não ver um título órfão.
const visibleGroups = computed<SidebarGroup[]>(() =>
  sidebarGroups
    .map(group => ({ ...group, items: group.items.filter(({ policy }) => can(policy).value) }))
    .filter(group => group.items.length > 0)
)

const canSeeTeacherClasses = can('GetTeacherCurrentClasses')

watch(account, () => { fetchTeacherClasses() }, { immediate: true })

const teacherClassesLinks = computed<NavigationMenuItem[]>(() =>
  teacherClasses.value.map(({ id, name }) => {
    const to = `/classes/${id}`
    return {
      label: name,
      to,
      // Prefixo, não igualdade: as telas de dentro da turma
      // (/classes/186/activities/26) também mantêm a turma destacada.
      active: isLinkActive(to),
      onSelect: () => { open.value = false },
    }
  })
)

function toLink({ label, icon, to }: SidebarLink): NavigationMenuItem {
  return {
    label,
    icon,
    to,
    active: isLinkActive(to),
    onSelect: () => { open.value = false },
  }
}

// Quais grupos estão expandidos. O accordion do UNavigationMenu é
// não-controlado por padrão, e o `defaultOpen` dos itens só é lido na
// montagem — como os grupos só aparecem depois que a conta carrega, nesse
// momento a lista ainda está vazia e tudo nasceria fechado. Controlando via
// v-model o estado fica correto e ainda sobrevive ao reload.
const collapsibleGroupIds = [
  ...sidebarGroups.filter(group => group.label).map(group => group.id),
  TEACHER_CLASSES_GROUP_ID,
]
const openGroups = useLocalStorage<string[]>('estud-sidebar-open-groups', collapsibleGroupIds)

// Uma lista só: os itens soltos no topo seguidos dos grupos colapsáveis. Se
// fossem listas separadas o UNavigationMenu desenharia um separador entre
// elas. Grupo sem título (o primeiro) não vira accordion.
const links = computed<NavigationMenuItem[]>(() => {
  const ungrouped = visibleGroups.value.filter(group => !group.label)
  const grouped = visibleGroups.value.filter(group => group.label)

  return [
    ...ungrouped.flatMap(group => group.items.map(toLink)),
    ...grouped.map(group => ({
      value: group.id,
      label: group.label,
      icon: group.icon,
      children: group.items.map(toLink),
    })),
    ...(canSeeTeacherClasses.value
      ? [{
          value: TEACHER_CLASSES_GROUP_ID,
          label: 'Turmas',
          icon: 'i-lucide-presentation',
          children: teacherClassesLinks.value,
        }]
      : []),
  ]
})

// Navegar pra dentro de um grupo fechado (pelo Ctrl+K ou por um link interno)
// abre o grupo, senão o item ativo ficaria escondido. Só adiciona — colapsar
// um grupo estando numa página dele continua valendo.
watch([() => route.path, visibleGroups], () => {
  const activeGroup = visibleGroups.value.find(
    group => group.label && group.items.some(({ to }) => isLinkActive(to))
  )
  if (activeGroup && !openGroups.value.includes(activeGroup.id)) {
    openGroups.value = [...openGroups.value, activeGroup.id]
  }
}, { immediate: true })

const groups = computed(() => [
  ...visibleGroups.value.map(group => ({
    id: group.id,
    label: group.label,
    items: group.items.map(({ label, icon, to }) => ({
      label,
      icon,
      to,
      onSelect: () => { open.value = false },
    })),
  })),
  ...(canSeeTeacherClasses.value
    ? [{
        id: 'turmas',
        label: 'Turmas',
        items: teacherClassesLinks.value.map(item => ({ ...item, icon: 'i-lucide-presentation' })),
      }]
    : []),
])
</script>

<template>
  <UDashboardGroup unit="rem">
    <UDashboardSidebar
      id="default"
      v-model:open="open"
      toggle-side="right"
      collapsible
      resizable
      class="bg-elevated/25"
      :ui="{ footer: 'lg:border-t lg:border-default' }"
    >
      <template #header="{ collapsed }">
        <TeamsMenu :collapsed="collapsed" />
      </template>

      <template #default="{ collapsed }">
        <UNavigationMenu
          v-model="openGroups"
          :collapsed="collapsed"
          :items="links"
          orientation="vertical"
          tooltip
          popover
        />

        <UNavigationMenu
          :collapsed="collapsed"
          :items="[{ label: 'Documentação', icon: 'i-lucide-book-open', to: '/docs', target: '_blank' }]"
          orientation="vertical"
          :external-icon="false"
          tooltip
          class="mt-auto"
        />
      </template>

      <template #footer="{ collapsed }">
        <UserMenu :collapsed="collapsed" />
      </template>
    </UDashboardSidebar>

    <UDashboardSearch :groups="groups" />

    <NotificationsSlideover />

    <div class="fixed top-4 right-6 z-50 flex items-center gap-3">
      <ChildrenSelector />
      <UChip
        :text="unreadCount > 9 ? '+9' : unreadCount"
        :show="unreadCount > 0"
        color="error"
        size="3xl"
        :ui="{ base: 'font-semibold text-xs leading-none h-3.5 min-w-3.5 px-1 rounded-full -translate-x-0.5 translate-y-0.5 pointer-events-none' }"
      >
        <UTooltip text="Notificações">
          <UButton
            icon="i-lucide-bell"
            color="neutral"
            variant="ghost"
            :square="true"
            @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); isNotificationsSlideoverOpen = true }"
          />
        </UTooltip>
      </UChip>
    </div>

    <slot />
  </UDashboardGroup>
</template>
