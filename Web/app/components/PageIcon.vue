<script setup lang="ts">
// Ícone da tela atual, no leading da navbar. Vem do item da sidebar
// correspondente à rota, pra ícone e menu nunca saírem de sincronia; telas que
// não estão na sidebar (conta, caixa de entrada, ...) passam o seu no `icon`.
const props = defineProps<{ icon?: string }>()

const { currentLink } = useSidebarNav()

const name = computed(() => currentLink.value?.icon ?? props.icon)
</script>

<template>
  <!-- Escondido no mobile, onde esse canto já é do botão que abre a sidebar
       (o UDashboardSidebarToggle some no mesmo breakpoint, lg). -->
  <UIcon v-if="name" :name="name" class="hidden size-5 shrink-0 text-highlighted lg:block" />
</template>
