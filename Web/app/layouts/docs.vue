<script setup lang="ts">
import type { ContentNavigationItem } from '@nuxt/content'

const route = useRoute()
const isMobile = useIsMobile()
const tocLinks = useState<any[]>('docs-toc', () => [])
const mobileNavOpen = ref(false)

const { data: navigation } = await useAsyncData('docs-nav', () =>
  queryCollectionNavigation('docs', ['description'])
)

const descriptions = computed(() => {
  const map = new Map<string, string>()
  const visit = (items: ContentNavigationItem[]) => {
    for (const { path, description, children } of items) {
      if (description) map.set(path, description as string)
      if (children) visit(children)
    }
  }
  visit(navigation.value ?? [])
  return map
})

// O `index.md` de uma pasta entra na navegação como filho com o mesmo título
// dela, e o prefixo sairia "Introdução > Introdução".
function breadcrumb({ prefix, label }: { prefix?: string, label?: string }) {
  const parts = prefix?.replace(/ >$/, '').split(' > ') ?? []
  return parts.at(-1) === label ? parts.slice(0, -1) : parts
}

const { data: searchFiles } = await useAsyncData('search-sections', () =>
  queryCollectionSearchSections('docs')
)

const searchNavigation = computed(() =>
  navigation.value?.map(item => ({ ...item, title: '' })) ?? []
)

watch(() => route.path, () => {
  mobileNavOpen.value = false
})
</script>

<template>
  <div>
    <UHeader :toggle="false">
      <template #left>
        <NuxtLink to="/docs" class="flex items-center gap-2 text-xl font-bold text-default">
          Estud
          <UBadge label="Docs" variant="subtle" size="sm" />
        </NuxtLink>
      </template>

      <template #right>
        <UTooltip text="Buscar">
          <UContentSearchButton @click="($event.currentTarget as HTMLElement).blur()" />
        </UTooltip>
        <UTooltip text="Code">
          <UButton
            icon="i-simple-icons-github"
            color="neutral"
            variant="ghost"
            to="https://github.com/ZaqueuCavalcante/estud"
            target="_blank"
            aria-label="Code"
            @click="(e) => { (e.currentTarget as HTMLElement).blur() }"
          />
        </UTooltip>
        <UTooltip text="Alternar tema">
          <UColorModeButton />
        </UTooltip>
        <UButton
          icon="i-lucide-align-left"
          color="neutral"
          variant="ghost"
          size="sm"
          class="lg:hidden"
          @click="() => { mobileNavOpen = true }"
        />
      </template>
    </UHeader>

    <UMain>
      <UContainer>
        <UPage>
          <template #left>
            <UPageAside class="overflow-x-hidden scrollbar-on-hover">
              <UContentNavigation :navigation="navigation?.[0]?.children" highlight />
            </UPageAside>
          </template>

          <slot />

          <template #right>
            <UContentToc :links="tocLinks" title="Nesta página" class="hidden lg:block scrollbar-on-hover" />
          </template>
        </UPage>
      </UContainer>
    </UMain>

    <USlideover v-model:open="mobileNavOpen" side="left" class="lg:hidden">
      <template #content>
        <div class="h-(--ui-header-height) shrink-0 flex items-center justify-between gap-1.5 px-4 sm:px-6">
          <span class="font-semibold text-default">Documentação</span>
          <UButton
            icon="i-lucide-x"
            color="neutral"
            variant="ghost"
            aria-label="Fechar"
            @click="() => { mobileNavOpen = false }"
          />
        </div>
        <div class="flex-1 overflow-y-auto px-4 py-2 sm:px-6">
          <UContentNavigation :navigation="navigation?.[0]?.children" highlight />
        </div>
      </template>
    </USlideover>

    <UContentSearch
      v-if="searchFiles"
      :files="searchFiles"
      :navigation="searchNavigation"
      :fullscreen="isMobile"
      :color-mode="false"
      placeholder="Pesquisar na documentação"
      shortcut="meta_k"
    >
      <template #item-label="{ item, ui }">
        <span class="flex items-center gap-1 min-w-0 font-medium text-highlighted">
          <template v-for="part in breadcrumb(item)" :key="part">
            <span class="shrink-0">{{ part }}</span>
            <UIcon name="i-lucide-chevron-right" class="size-3.5 shrink-0 text-dimmed" />
          </template>
          <span v-if="item.labelHtml" :class="ui.itemLabelBase({ class: 'truncate' })" v-html="item.labelHtml" />
          <span v-else :class="ui.itemLabelBase({ class: 'truncate' })">{{ item.label }}</span>
        </span>
      </template>

      <template #item-description="{ item }">
        <span v-if="descriptions.get(item.to as string)">{{ descriptions.get(item.to as string) }}</span>
        <span v-else-if="item.suffixHtml" v-html="item.suffixHtml" />
        <span v-else>{{ item.suffix }}</span>
      </template>
    </UContentSearch>
  </div>
</template>
