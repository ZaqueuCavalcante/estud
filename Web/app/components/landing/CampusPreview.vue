<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'

// Moldura estática do dashboard em volta da demo de ocupação. Não dá pra usar
// o `layouts/default.vue` aqui: ele depende de conta, permissões e
// notificações, e a landing é pública.
const sidebarLinks = computed<NavigationMenuItem[]>(() =>
  sidebarGroups
    .filter(group => ['academico', 'secretaria', 'pessoas', 'sistema'].includes(group.id))
    .map(group => ({
      value: group.id,
      label: group.label,
      icon: group.icon,
      children: group.items.map(item => ({
        label: item.label,
        icon: item.icon,
        active: item.to === '/campi',
      })),
    })),
)

const openGroups = ref(['academico', 'secretaria', 'pessoas', 'sistema'])

const externalLinks: NavigationMenuItem[] = [
  { label: 'Code', icon: 'i-simple-icons-github' },
  { label: 'Documentação', icon: 'i-lucide-book-open' },
]

const breadcrumb = [
  { label: 'Campi', icon: 'i-lucide-map-pin' },
  { label: 'Detalhes' },
]

const tabs: NavigationMenuItem[][] = [[
  { label: 'Ocupação', icon: 'i-lucide-layout-grid', active: true },
  { label: 'Horários', icon: 'i-lucide-clock' },
  { label: 'Salas', icon: 'i-lucide-door-open' },
]]
</script>

<template>
  <div class="overflow-hidden rounded-xl bg-default ring ring-default shadow-2xl shadow-black/10 dark:shadow-black/40">
    <p class="sr-only">
      Tela de ocupação de campus do Estud, com mapa de uso das salas por turno e indicadores de tempo e espaço.
      Clique numa célula do mapa para ver as salas daquele dia e turno.
    </p>

    <div class="flex items-stretch">
      <aside
        inert
        aria-hidden="true"
        class="hidden w-52 shrink-0 flex-col border-r border-default bg-elevated/25 lg:flex"
      >
        <div class="flex h-16 shrink-0 items-center gap-2 px-4">
          <EstudIcon class="size-6 shrink-0" />
          <span class="text-xl font-semibold text-highlighted">Estud</span>
          <UIcon name="i-lucide-panel-left-close" class="ms-auto size-5 text-muted" />
        </div>

        <div class="flex flex-1 flex-col gap-1 px-2 pb-2">
          <UNavigationMenu
            v-model="openGroups"
            :items="sidebarLinks"
            orientation="vertical"
          />
          <UNavigationMenu
            :items="externalLinks"
            orientation="vertical"
            class="mt-auto"
          />
        </div>

        <div class="shrink-0 border-t border-default px-4 py-3">
          <span class="block truncate text-sm font-medium text-default">Zaqueu Cavalcante</span>
          <span class="block truncate text-xs text-muted">Diretor</span>
        </div>
      </aside>

      <div class="flex min-w-0 flex-1 flex-col">
        <div
          inert
          aria-hidden="true"
          class="flex h-16 shrink-0 items-center gap-3 border-b border-default px-6"
        >
          <UIcon name="i-lucide-menu" class="size-5 shrink-0 text-default lg:hidden" />
          <UBreadcrumb :items="breadcrumb" class="min-w-0" />

          <div class="ms-auto flex shrink-0 items-center gap-3">
            <UIcon name="i-lucide-sun" class="size-5 text-default dark:hidden" />
            <UIcon name="i-lucide-moon" class="hidden size-5 text-default dark:block" />
            <UIcon name="i-lucide-bell" class="size-5 text-default" />
          </div>
        </div>

        <!-- O painel rola dentro da moldura, como na tela real. `overscroll-auto`
             (o padrão) é de propósito: ao chegar no fim, o scroll volta a ser o
             da landing em vez de travar o visitante aqui dentro. -->
        <div class="max-h-[46rem] overflow-y-auto px-6 py-5">
          <div class="flex flex-col gap-6 pb-2">
            <div class="flex items-center justify-between gap-6">
              <div class="flex flex-col gap-1">
                <div class="flex items-center gap-2">
                  <h2 class="text-2xl font-semibold tracking-tight text-highlighted">
                    Campus Agreste
                  </h2>
                  <UIcon name="i-lucide-pencil" class="size-4 shrink-0 text-muted" />
                </div>
                <p class="flex items-center gap-1.5 text-sm text-muted">
                  <UIcon name="i-lucide-map-pin" class="size-4 shrink-0" />
                  Caruaru · PE
                </p>
              </div>

              <UNavigationMenu
                inert
                aria-hidden="true"
                :items="tabs"
                highlight
                class="shrink-0"
              />
            </div>

            <LandingCampusOccupancy />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
